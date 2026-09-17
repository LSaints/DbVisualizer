using System.Net;
using System.Text.Json;
using DatabaseDiagram.Api.Modelos;
using DatabaseDiagram.Api.Provedores;
using DatabaseDiagram.Api.Provedores.Ia.Claude;

namespace DatabaseDiagram.Testes.Provedores;

public class ClaudeProvedorDeIaTestes
{
    private const string RespostaJson = """
        {
          "id": "msg_01",
          "type": "message",
          "role": "assistant",
          "content": [
            { "type": "thinking", "thinking": "preciso listar os contratos", "signature": "ass" },
            { "type": "text", "text": "{\"consulta\":\"SELECT * FROM contratos;\",\"explicacao\":\"Lista os contratos.\"}" }
          ],
          "model": "claude-sonnet-5",
          "stop_reason": "end_turn",
          "usage": { "input_tokens": 100, "output_tokens": 50 }
        }
        """;

    [Fact]
    public async Task ObterRespostaAsync_EnviaPayloadParaMessages()
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
        var provedor = new ClaudeProvedorDeIa(httpClient);

        var pedido = new PedidoDeResposta(
            MensagemDeSistema: "Você é um assistente de SQL.",
            MensagemDoUsuario: "Liste os contratos.");

        var resposta = await provedor.ObterRespostaAsync(pedido, "chave-de-teste-123", CancellationToken.None);

        Assert.NotNull(requisicaoCapturada);
        Assert.Equal(HttpMethod.Post, requisicaoCapturada!.Method);
        Assert.Equal("https://api.anthropic.com/v1/messages", requisicaoCapturada.RequestUri?.ToString());
        Assert.Equal("chave-de-teste-123", requisicaoCapturada.Headers.GetValues("x-api-key").Single());
        Assert.Equal("2023-06-01", requisicaoCapturada.Headers.GetValues("anthropic-version").Single());

        Assert.Equal("claude-sonnet-5", corpoCapturado.GetProperty("model").GetString());
        Assert.Equal(8192, corpoCapturado.GetProperty("max_tokens").GetInt32());
        Assert.Equal(
            "Você é um assistente de SQL.",
            corpoCapturado.GetProperty("system").GetString());

        var mensagens = corpoCapturado.GetProperty("messages");
        Assert.Equal(1, mensagens.GetArrayLength());
        Assert.Equal("user", mensagens[0].GetProperty("role").GetString());
        Assert.Equal("Liste os contratos.", mensagens[0].GetProperty("content").GetString());

        Assert.Contains("SELECT * FROM contratos;", resposta);
    }

    [Fact]
    public async Task ObterRespostaAsync_ComHistorico_EnviaMessagesComPapeisNativosNaOrdemCronologica()
    {
        JsonElement corpoCapturado = default;
        using var httpClient = new HttpClient(new HandlerFalso(async requisicao =>
        {
            corpoCapturado = JsonSerializer.Deserialize<JsonElement>(
                await requisicao.Content!.ReadAsStringAsync());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(RespostaJson)
            };
        }));
        var provedor = new ClaudeProvedorDeIa(httpClient);

        var historico = new List<MensagemDaConversa>
        {
            new() { Papel = "usuario", Conteudo = "primeira pergunta", CriadaEm = "2026-01-01T00:00:00Z" },
            new() { Papel = "assistente", Conteudo = "resposta anterior", CriadaEm = "2026-01-01T00:00:01Z" }
        };
        var pedido = new PedidoDeResposta(
            MensagemDeSistema: "Você é um assistente de SQL.",
            MensagemDoUsuario: "na consulta anterior, inclua o nome do cliente",
            MensagensDaConversa: historico);

        await provedor.ObterRespostaAsync(pedido, "chave-de-teste-123", CancellationToken.None);

        var mensagens = corpoCapturado.GetProperty("messages");
        Assert.Equal(3, mensagens.GetArrayLength());
        Assert.Equal("user", mensagens[0].GetProperty("role").GetString());
        Assert.Contains("primeira pergunta", mensagens[0].GetProperty("content").GetString());
        Assert.Equal("assistant", mensagens[1].GetProperty("role").GetString());
        Assert.Contains("resposta anterior", mensagens[1].GetProperty("content").GetString());
        Assert.Equal("user", mensagens[2].GetProperty("role").GetString());
        Assert.Equal("na consulta anterior, inclua o nome do cliente", mensagens[2].GetProperty("content").GetString());
    }

    [Fact]
    public async Task ObterRespostaAsync_ComDoisTurnosConsecutivosDoMesmoPapel_MesclaEmUmaUnicaMensagem()
    {
        JsonElement corpoCapturado = default;
        using var httpClient = new HttpClient(new HandlerFalso(async requisicao =>
        {
            corpoCapturado = JsonSerializer.Deserialize<JsonElement>(
                await requisicao.Content!.ReadAsStringAsync());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(RespostaJson)
            };
        }));
        var provedor = new ClaudeProvedorDeIa(httpClient);

        var historico = new List<MensagemDaConversa>
        {
            new() { Papel = "usuario", Conteudo = "parte 1", CriadaEm = "2026-01-01T00:00:00Z" }
        };
        var pedido = new PedidoDeResposta(
            MensagemDeSistema: "sistema",
            MensagemDoUsuario: "parte 2",
            MensagensDaConversa: historico);

        await provedor.ObterRespostaAsync(pedido, "chave-de-teste-123", CancellationToken.None);

        // Dois turnos "user" consecutivos mesclados em um só, evitando o erro
        // 400 da API Messages, que exige papéis alternados.
        var mensagens = corpoCapturado.GetProperty("messages");
        Assert.Equal(1, mensagens.GetArrayLength());
        Assert.Equal("user", mensagens[0].GetProperty("role").GetString());
        Assert.Equal("parte 1\nparte 2", mensagens[0].GetProperty("content").GetString());
    }

    [Fact]
    public async Task ObterRespostaAsync_ComHistoricoDoContrato_SerializaAssistenteComChavesDoContrato()
    {
        JsonElement corpoCapturado = default;
        using var httpClient = new HttpClient(new HandlerFalso(async requisicao =>
        {
            corpoCapturado = JsonSerializer.Deserialize<JsonElement>(
                await requisicao.Content!.ReadAsStringAsync());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(RespostaJson)
            };
        }));
        var provedor = new ClaudeProvedorDeIa(httpClient);

        var historico = new List<MensagemDaConversa>
        {
            new()
            {
                Papel = "usuario",
                Conteudo = "liste os contratos",
                CriadaEm = "2026-01-01T00:00:00Z"
            },
            new()
            {
                Papel = "assistente",
                Conteudo = new RespostaDeConsultaDoAssistente
                {
                    Consulta = "SELECT c.* FROM contratos c;",
                    Explicacao = "Lista os contratos.",
                    Objetivo = "Encontrar os contratos.",
                    TipoConsulta = "SELECT",
                    ResultadoEsperado = "Uma lista.",
                    CamposRetorno = new CamposDeRetorno { ["contratos"] = "*" }
                },
                CriadaEm = "2026-01-01T00:00:01Z"
            }
        };
        var pedido = new PedidoDeResposta(
            MensagemDeSistema: "sistema",
            MensagemDoUsuario: "inclua o nome do cliente",
            MensagensDaConversa: historico);

        await provedor.ObterRespostaAsync(pedido, "chave-de-teste-123", CancellationToken.None);

        var mensagens = corpoCapturado.GetProperty("messages");
        Assert.Equal(3, mensagens.GetArrayLength());
        var assistenteAnterior = mensagens[1];
        Assert.Equal("assistant", assistenteAnterior.GetProperty("role").GetString());
        var conteudo = JsonDocument.Parse(assistenteAnterior.GetProperty("content").GetString()!);
        var raiz = conteudo.RootElement;
        // Chaves do contrato em minúsculas (consulta, explicacao, ...) e as
        // compostas mapeadas por JsonPropertyName (campos_retorno, tipo_consulta):
        // nunca PascalCase, que faria o modelo ecoar um formato inválido.
        Assert.True(raiz.TryGetProperty("consulta", out var consulta));
        Assert.Equal("SELECT c.* FROM contratos c;", consulta.GetString());
        Assert.True(raiz.TryGetProperty("explicacao", out _));
        Assert.True(raiz.TryGetProperty("objetivo", out var objetivo));
        Assert.Equal("Encontrar os contratos.", objetivo.GetString());
        Assert.True(raiz.TryGetProperty("campos_retorno", out _));
        Assert.True(raiz.TryGetProperty("tipo_consulta", out _));
        Assert.True(raiz.TryGetProperty("resultado_esperado", out _));
    }

    [Fact]
    public async Task ObterRespostaAsync_ComRespostaInvalidaDoProvedor_LancaFalha()
    {
        using var httpClient = new HttpClient(new HandlerFalso(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized))));
        var provedor = new ClaudeProvedorDeIa(httpClient);

        var pedido = new PedidoDeResposta("sistema", "usuário");

        await Assert.ThrowsAnyAsync<Exception>(
            () => provedor.ObterRespostaAsync(pedido, "chave-de-teste-123", CancellationToken.None));
    }

    [Fact]
    public async Task ObterRespostaAsync_ComRespostaSemConteudo_LancaFalhaNoProvedor()
    {
        using var httpClient = new HttpClient(new HandlerFalso(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{ "content": [] }""")
            })));
        var provedor = new ClaudeProvedorDeIa(httpClient);

        var pedido = new PedidoDeResposta("sistema", "usuário");

        await Assert.ThrowsAsync<FalhaNoProvedorDeIaException>(
            () => provedor.ObterRespostaAsync(pedido, "chave-de-teste-123", CancellationToken.None));
    }

    [Fact]
    public async Task ObterRespostaAsync_ComApenasBlocosDeThinking_LancaFalhaNoProvedor()
    {
        using var httpClient = new HttpClient(new HandlerFalso(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{ "content": [ { "type": "thinking", "thinking": "", "signature": "sig" } ] }""")
            })));
        var provedor = new ClaudeProvedorDeIa(httpClient);

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