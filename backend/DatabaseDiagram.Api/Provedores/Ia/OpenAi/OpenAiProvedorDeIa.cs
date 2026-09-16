using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DatabaseDiagram.Api.Provedores.Ia.OpenAi;

/// <summary>
/// Provedor MVP: OpenAI Chat Completions (não-streaming). Monta o payload de
/// <c>POST /v1/chat/completions</c> com a mensagem de sistema e a do usuário,
/// autoriza com <c>Authorization: Bearer &lt;chave&gt;</c> e extrai
/// <c>data.choices[0].message.content</c>. O modelo padrão é fixo na
/// configuração (sem seleção de modelo no MVP). A chave de API é usada somente
/// nesta requisição, em memória, e nunca é retornada nem logada (G3).
/// </summary>
public sealed class OpenAiProvedorDeIa : InterfaceProvedorDeIa
{
    private static readonly Uri EnderecoDaApi = new("https://api.openai.com/v1/chat/completions");

    /// <summary>Modelo padrão usado quando nenhum é informado na configuração.</summary>
    internal const string ModeloPadrao = "gpt-4o";

    /// <summary>Nome do cliente HTTP registrado no DI (com timeout interno).</summary>
    internal const string NomeDoCliente = "OpenAi";

    private readonly HttpClient _httpClient;
    private readonly string _modelo;

    public OpenAiProvedorDeIa(HttpClient httpClient, string? modelo = null)
    {
        _httpClient = httpClient;
        _modelo = string.IsNullOrWhiteSpace(modelo) ? ModeloPadrao : modelo;
    }

    public string Tipo => "openai";

    public string Rotulo => "OpenAI";

    public async Task<string> ObterRespostaAsync(
        PedidoDeResposta pedido,
        string chaveDeApi,
        CancellationToken cancellationToken)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, EnderecoDaApi);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", chaveDeApi);
        requisicao.Content = JsonContent.Create(new
        {
            model = _modelo,
            messages = new[]
            {
                new { role = "system", content = pedido.MensagemDeSistema },
                new { role = "user", content = pedido.MensagemDoUsuario }
            }
        });

        using var resposta = await _httpClient.SendAsync(requisicao, cancellationToken);
        resposta.EnsureSuccessStatusCode();

        var corpo = await resposta.Content.ReadAsStringAsync(cancellationToken);
        using var documento = JsonDocument.Parse(corpo);

        if (!documento.RootElement.TryGetProperty("choices", out var escolhas) ||
            escolhas.GetArrayLength() == 0 ||
            !escolhas[0].TryGetProperty("message", out var mensagem) ||
            !mensagem.TryGetProperty("content", out var conteudo) ||
            conteudo.ValueKind != JsonValueKind.String)
        {
            throw new FalhaNoProvedorDeIaException();
        }

        return conteudo.GetString() ?? throw new FalhaNoProvedorDeIaException();
    }
}