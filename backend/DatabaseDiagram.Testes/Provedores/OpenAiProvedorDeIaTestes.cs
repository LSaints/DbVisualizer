using System.Net;
using System.Text.Json;
using DatabaseDiagram.Api.Provedores;
using DatabaseDiagram.Api.Provedores.Ia.OpenAi;

namespace DatabaseDiagram.Testes.Provedores;

public class OpenAiProvedorDeIaTestes
{
    private const string RespostaJson = """
        {
          "choices": [
            {
              "message": { "content": "{\"consulta\":\"SELECT * FROM contratos;\",\"explicacao\":\"Lista os contratos.\"}" }
            }
          ]
        }
        """;

    [Fact]
    public async Task ObterRespostaAsync_EnviaPayloadParaChatCompletions()
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
        var provedor = new OpenAiProvedorDeIa(httpClient);

        var pedido = new PedidoDeResposta(
            MensagemDeSistema: "Você é um assistente de SQL.",
            MensagemDoUsuario: "Liste os contratos.");

        var resposta = await provedor.ObterRespostaAsync(pedido, "sk-teste-123", CancellationToken.None);

        Assert.NotNull(requisicaoCapturada);
        Assert.Equal(HttpMethod.Post, requisicaoCapturada!.Method);
        Assert.Equal("https://api.openai.com/v1/chat/completions", requisicaoCapturada.RequestUri?.ToString());
        Assert.Equal("Bearer", requisicaoCapturada.Headers.Authorization?.Scheme);
        Assert.Equal("sk-teste-123", requisicaoCapturada.Headers.Authorization?.Parameter);

        Assert.Equal("gpt-4o", corpoCapturado.GetProperty("model").GetString());
        var mensagens = corpoCapturado.GetProperty("messages");
        Assert.Equal(2, mensagens.GetArrayLength());
        Assert.Equal("system", mensagens[0].GetProperty("role").GetString());
        Assert.Equal("Você é um assistente de SQL.", mensagens[0].GetProperty("content").GetString());
        Assert.Equal("user", mensagens[1].GetProperty("role").GetString());
        Assert.Equal("Liste os contratos.", mensagens[1].GetProperty("content").GetString());

        Assert.Contains("SELECT * FROM contratos;", resposta);
    }

    [Fact]
    public async Task ObterRespostaAsync_ComRespostaInvalidaDoProvedor_LancaFalha()
    {
        using var httpClient = new HttpClient(new HandlerFalso(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError))));
        var provedor = new OpenAiProvedorDeIa(httpClient);

        var pedido = new PedidoDeResposta("sistema", "usuário");

        await Assert.ThrowsAnyAsync<Exception>(
            () => provedor.ObterRespostaAsync(pedido, "sk-teste-123", CancellationToken.None));
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