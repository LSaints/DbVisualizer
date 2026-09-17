using System.Net.Http.Json;
using System.Text.Json;

namespace DatabaseDiagram.Api.Provedores.Ia.Claude;

/// <summary>
/// Provedor de IA: Claude (Anthropic Messages, não-streaming). Monta o payload
/// de <c>POST /v1/messages</c> com a instrução de sistema e as mensagens da
/// conversa, autoriza com os cabeçalhos <c>x-api-key</c> e
/// <c>anthropic-version</c> e extrai o primeiro bloco <c>text</c> de
/// <c>data.content</c>. Como o thinking adaptativo de modelos recentes
/// (ex.: <c>claude-sonnet-5</c>) é ligado por padrão e antepõe blocos
/// <c>thinking</c> ao texto, a leitura seleciona o bloco pelo campo
/// <c>type</c> em vez de assumir <c>content[0]</c> (guia de migração da
/// Anthropic). O modelo padrão é fixo na configuração (sem seleção de modelo
/// no MVP). A chave de API é usada somente nesta requisição, em memória, e
/// nunca é retornada nem logada (G3).
/// </summary>
public sealed class ClaudeProvedorDeIa : InterfaceProvedorDeIa
{
    private static readonly Uri EnderecoDaApi = new("https://api.anthropic.com/v1/messages");

    /// <summary>Modelo padrão usado quando nenhum é informado na configuração.</summary>
    internal const string ModeloPadrao = "claude-sonnet-5";

    /// <summary>Nome do cliente HTTP registrado no DI (com timeout interno).</summary>
    internal const string NomeDoCliente = "Claude";

    /// <summary>Limite de saída exigido pela API Messages (decisão D5).</summary>
    private const int LimiteDeSaida = 8192;

    private const string CabecalhoDaChave = "x-api-key";
    private const string CabecalhoDaVersao = "anthropic-version";
    private const string VersaoDaApi = "2023-06-01";

    private readonly HttpClient _httpClient;
    private readonly string _modelo;

    /// <summary>
    /// Serirializa o histórico no formato do contrato (camelCase/snake_case das
    /// chaves mapeadas com <c>JsonPropertyName</c>). Serializar com as opções
    /// padrão produziria PascalCase e induziria o modelo a ecoar esse formato,
    /// quebrando a validação do contrato na conversa seguinte.
    /// </summary>
    private static readonly JsonSerializerOptions OpcoesDeContrato = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ClaudeProvedorDeIa(HttpClient httpClient, string? modelo = null)
    {
        _httpClient = httpClient;
        _modelo = string.IsNullOrWhiteSpace(modelo) ? ModeloPadrao : modelo;
    }

    public string Tipo => "claude";

    public string Rotulo => "Claude (Anthropic)";

    public async Task<string> ObterRespostaAsync(
        PedidoDeResposta pedido,
        string chaveDeApi,
        CancellationToken cancellationToken)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, EnderecoDaApi);
        requisicao.Headers.Add(CabecalhoDaChave, chaveDeApi);
        requisicao.Headers.Add(CabecalhoDaVersao, VersaoDaApi);

        var mensagens = new List<Dictionary<string, object>>();

        void AdicionarTurno(string papel, string texto)
        {
            if (mensagens.Count > 0 && (string)mensagens[^1]["role"] == papel)
            {
                mensagens[^1]["content"] += "\n" + texto;
                return;
            }

            mensagens.Add(new Dictionary<string, object>
            {
                ["role"] = papel,
                ["content"] = texto
            });
        }

        foreach (var mensagemDaConversa in pedido.MensagensDaConversa ?? [])
        {
            // A API Messages exige papéis alternados (user/assistant); turnos
            // consecutivos do mesmo papel são mesclados para evitar o erro 400
            // (mesmo critério do provedor Google/Gemini).
            AdicionarTurno(
                mensagemDaConversa.Papel == "assistente" ? "assistant" : "user",
                ConteudoComoTexto(mensagemDaConversa.Conteudo));
        }

        AdicionarTurno("user", pedido.MensagemDoUsuario);

        requisicao.Content = JsonContent.Create(new
        {
            model = _modelo,
            max_tokens = LimiteDeSaida,
            system = pedido.MensagemDeSistema,
            messages = mensagens
        });

        using var resposta = await _httpClient.SendAsync(requisicao, cancellationToken);

        // Falha explícita com o status real para o log interno (o usuário
        // sempre recebe a mensagem genérica em 502; a chave nunca é exposta).
        if (!resposta.IsSuccessStatusCode)
        {
            throw new FalhaNoProvedorDeIaException(
                $"Falha do provedor Claude: status HTTP {(int)resposta.StatusCode}.");
        }

        var corpo = await resposta.Content.ReadAsStringAsync(cancellationToken);
        using var documento = JsonDocument.Parse(corpo);

        if (!documento.RootElement.TryGetProperty("content", out var conteudos))
        {
            throw new FalhaNoProvedorDeIaException();
        }

        // Com o thinking adaptativo ligado por padrão, a resposta começa com
        // um ou mais blocos "thinking" antes do texto; o bloco de texto é
        // selecionado pelo campo "type", nunca por posição (content[0]).
        foreach (var bloco in conteudos.EnumerateArray())
        {
            if (bloco.ValueKind == JsonValueKind.Object &&
                bloco.TryGetProperty("type", out var tipo) &&
                tipo.ValueKind == JsonValueKind.String &&
                tipo.GetString() == "text" &&
                bloco.TryGetProperty("text", out var texto) &&
                texto.ValueKind == JsonValueKind.String &&
                texto.GetString() is { Length: > 0 } textoDaResposta)
            {
                return textoDaResposta;
            }
        }

        throw new FalhaNoProvedorDeIaException();
    }

    /// <summary>
    /// Traduz o conteúdo de uma mensagem do histórico (string para
    /// <c>"usuario"</c>, contrato estruturado para <c>"assistente"</c>) para
    /// texto simples enviado ao provedor.
    /// </summary>
    private static string ConteudoComoTexto(object conteudo) =>
        conteudo switch
        {
            string texto => texto,
            _ => JsonSerializer.Serialize(conteudo, OpcoesDeContrato)
        };
}