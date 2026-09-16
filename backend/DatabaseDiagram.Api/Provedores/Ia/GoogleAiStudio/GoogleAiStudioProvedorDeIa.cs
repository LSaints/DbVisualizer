using System.Net.Http.Json;
using System.Text.Json;

namespace DatabaseDiagram.Api.Provedores.Ia.GoogleAiStudio;

/// <summary>
/// Provedor de IA: Google AI Studio (Gemini <c>generateContent</c>,
/// não-streaming). Monta o payload de <c>POST /v1beta/models/&lt;modelo&gt;:generateContent</c>
/// com a instrução de sistema e o conteúdo do usuário, autoriza com o cabeçalho
/// <c>x-goog-api-key</c> e extrai <c>data.candidates[0].content.parts[0].text</c>.
/// O modelo padrão é fixo na configuração (sem seleção de modelo no MVP). A
/// chave de API é usada somente nesta requisição, em memória, e nunca é
/// retornada nem logada (G3).
/// </summary>
public sealed class GoogleAiStudioProvedorDeIa : InterfaceProvedorDeIa
{
    private static readonly Uri EnderecoBase = new(
        "https://generativelanguage.googleapis.com/v1beta/");

    /// <summary>Modelo padrão usado quando nenhum é informado na configuração.</summary>
    internal const string ModeloPadrao = "gemini-3.5-flash";

    /// <summary>Nome do cliente HTTP registrado no DI (com timeout interno).</summary>
    internal const string NomeDoCliente = "GoogleAiStudio";

    private const string CabecalhoDaChave = "x-goog-api-key";

    private readonly HttpClient _httpClient;
    private readonly string _modelo;

    public GoogleAiStudioProvedorDeIa(HttpClient httpClient, string? modelo = null)
    {
        _httpClient = httpClient;
        _modelo = string.IsNullOrWhiteSpace(modelo) ? ModeloPadrao : modelo;
    }

    public string Tipo => "google-ai-studio";

    public string Rotulo => "Google AI Studio";

    public async Task<string> ObterRespostaAsync(
        PedidoDeResposta pedido,
        string chaveDeApi,
        CancellationToken cancellationToken)
    {
        using var requisicao = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(EnderecoBase, $"models/{_modelo}:generateContent"));
        requisicao.Headers.Add(CabecalhoDaChave, chaveDeApi);
        requisicao.Content = JsonContent.Create(new
        {
            system_instruction = new
            {
                parts = new[]
                {
                    new { text = pedido.MensagemDeSistema }
                }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = pedido.MensagemDoUsuario }
                    }
                }
            }
        });

        using var resposta = await _httpClient.SendAsync(requisicao, cancellationToken);
        resposta.EnsureSuccessStatusCode();

        var corpo = await resposta.Content.ReadAsStringAsync(cancellationToken);
        using var documento = JsonDocument.Parse(corpo);

        if (!documento.RootElement.TryGetProperty("candidates", out var candidatos) ||
            candidatos.GetArrayLength() == 0 ||
            !candidatos[0].TryGetProperty("content", out var conteudo) ||
            !conteudo.TryGetProperty("parts", out var partes) ||
            partes.GetArrayLength() == 0 ||
            !partes[0].TryGetProperty("text", out var texto) ||
            texto.ValueKind != JsonValueKind.String)
        {
            throw new FalhaNoProvedorDeIaException();
        }

        return texto.GetString() ?? throw new FalhaNoProvedorDeIaException();
    }
}