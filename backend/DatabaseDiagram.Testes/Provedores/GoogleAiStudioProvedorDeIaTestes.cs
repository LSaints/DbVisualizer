using System.Net;
using System.Text.Json;
using DatabaseDiagram.Api.Provedores;
using DatabaseDiagram.Api.Provedores.Ia.GoogleAiStudio;

namespace DatabaseDiagram.Testes.Provedores;

public class GoogleAiStudioProvedorDeIaTestes
{
    private const string RespostaJson = """
        {
          "candidates": [
            {
              "content": {
                "parts": [
                  { "text": "{\"consulta\":\"SELECT * FROM contratos;\",\"explicacao\":\"Lista os contratos.\"}" }
                ],
                "role": "model"
              },
              "finishReason": "STOP"
            }
          ]
        }
        """;

    [Fact]
    public async Task ObterRespostaAsync_EnviaPayloadParaGenerateContent()
    {
        HttpRequestMessage? requisicaoCapturada = null;
        JsonElement corpoCapturado = default;
        using var httpClient = new HttpClient(new HandlerFalso(async requisicao =>
        {
            requisicaoCapturada = requisicao;
            corpoCapturado = JsonSerializer.Deserialize<JsonElement>(
                await requisicao.Content!.ReadAsStringAsync());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(RespostaJson)
            };
        }));
        var provedor = new GoogleAiStudioProvedorDeIa(httpClient);

        var pedido = new PedidoDeResposta(
            MensagemDeSistema: "Você é um assistente de SQL.",
            MensagemDoUsuario: "Liste os contratos.");

        var resposta = await provedor.ObterRespostaAsync(pedido, "chave-de-teste-123", CancellationToken.None);

        Assert.NotNull(requisicaoCapturada);
        Assert.Equal(HttpMethod.Post, requisicaoCapturada!.Method);
        Assert.Equal(
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent",
            requisicaoCapturada.RequestUri?.ToString());
        Assert.Equal("chave-de-teste-123", requisicaoCapturada.Headers.GetValues("x-goog-api-key").Single());

        var instrucaoDeSistema = corpoCapturado.GetProperty("system_instruction");
        Assert.Equal(
            "Você é um assistente de SQL.",
            instrucaoDeSistema.GetProperty("parts")[0].GetProperty("text").GetString());

        var conteudos = corpoCapturado.GetProperty("contents");
        Assert.Equal(1, conteudos.GetArrayLength());
        Assert.Equal("user", conteudos[0].GetProperty("role").GetString());
        Assert.Equal(
            "Liste os contratos.",
            conteudos[0].GetProperty("parts")[0].GetProperty("text").GetString());

        Assert.Contains("SELECT * FROM contratos;", resposta);
    }

    [Fact]
    public async Task ObterRespostaAsync_ComRespostaInvalidaDoProvedor_LancaFalha()
    {
        using var httpClient = new HttpClient(new HandlerFalso(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError))));
        var provedor = new GoogleAiStudioProvedorDeIa(httpClient);

        var pedido = new PedidoDeResposta("sistema", "usuário");

        await Assert.ThrowsAnyAsync<Exception>(
            () => provedor.ObterRespostaAsync(pedido, "chave-de-teste-123", CancellationToken.None));
    }

    [Fact]
    public async Task ObterRespostaAsync_ComRespostaSemCandidatos_LancaFalhaNoProvedor()
    {
        using var httpClient = new HttpClient(new HandlerFalso(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{ "candidates": [] }""")
            })));
        var provedor = new GoogleAiStudioProvedorDeIa(httpClient);

        var pedido = new PedidoDeResposta("sistema", "usuário");

        await Assert.ThrowsAsync<FalhaNoProvedorDeIaException>(
            () => provedor.ObterRespostaAsync(pedido, "chave-de-teste-123", CancellationToken.None));
    }

    private sealed class HandlerFalso(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            responder(request);
    }
}