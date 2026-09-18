using DatabaseDiagram.Api.Dtos;
using DatabaseDiagram.Api.Modelos;
using DatabaseDiagram.Api.Provedores;
using DatabaseDiagram.Api.Servicos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DatabaseDiagram.Api.Controladores;

/// <summary>
/// Assistente de IA para consultas SQL: <c>POST /api/assistente/consultas</c>.
/// Limitada por janela de tempo por cliente e por tamanho do corpo. A chave de
/// API é efêmera (memória, por requisição) e nunca é retornada nem logada — o
/// <c>logger</c> registra somente o provedor usado (D9/G3/G6). Com
/// <c>conversaId</c> (US1), o histórico da janela de contexto é enviado ao
/// provedor e a troca é persistida somente em sucesso (D5); sem
/// <c>conversaId</c>, o fluxo legado de troca única permanece sem persistência.
/// </summary>
[ApiController]
[Route("api/assistente")]
[EnableRateLimiting("AssistentePorCliente")]
[RequestSizeLimit(TamanhoMaximoDoCorpoEmBytes)]
public sealed class AssistenteControlador(
    ServicoDoAssistente servicoDoAssistente,
    FabricaDeProvedoresDeIa fabricaDeProvedores,
    ServicoDeConversas servicoDeConversas,
    ILogger<AssistenteControlador> logger) : ControllerBase
{
    private const string CabecalhoDeContextoTruncado = "X-Contexto-Truncado";
    /// <summary>Corpo máximo do pedido (inclui o schema de contexto): 1 MB.</summary>
    internal const int TamanhoMaximoDoCorpoEmBytes = 1_048_576;

    /// <summary>
    /// <c>GET /api/assistente/provedores</c>. Lista somente os provedores
    /// implementados na fábrica, ex.: <c>[ { "provedor": "openai", "rotulo": "OpenAI" } ]</c>
    /// (FR-002/FR-004).
    /// </summary>
    [HttpGet("provedores")]
    public IActionResult ListarProvedores() =>
        Ok(fabricaDeProvedores.Listar()
            .Select(provedor => new ProvedorDeIaDisponivel(provedor.Tipo, provedor.Rotulo)));

    /// <summary>
    /// <c>POST /api/assistente/consultas</c>. Sucesso: <c>200</c> com o contrato
    /// estruturado. Entrada inválida: <c>400</c> em pt-BR. Resposta não
    /// interpretável: <c>422</c>. Limite excedido: <c>429</c>. Falha do provedor:
    /// <c>502</c> com mensagem genérica, sem expor a chave.
    /// </summary>
    [HttpPost("consultas")]
    public async Task<IActionResult> ObterResposta(
        RequisicaoDeConsultaDoAssistente? requisicao,
        CancellationToken cancellationToken)
    {
        var erroDeEntrada = ValidarEntrada(requisicao);
        if (erroDeEntrada is not null)
        {
            return BadRequest(new { mensagem = erroDeEntrada });
        }

        Conversa? conversa = null;
        var contextoTruncado = false;
        IReadOnlyList<MensagemDaConversa>? historico = null;

        if (requisicao!.ConversaId is { } conversaId)
        {
            conversa = await servicoDeConversas.ObterConversaInternaAsync(conversaId.ToString());
            if (conversa is null)
            {
                return NotFound(new { mensagem = MensagensDeValidacao.ConversaNaoEncontrada });
            }

            if (conversa.ContextoDeBanco is not null &&
                !IdentidadeCorresponde(conversa.ContextoDeBanco, requisicao.ContextoDeBanco!))
            {
                return Conflict(new { mensagem = MensagensDeValidacao.BancoDivergenteDaConversa });
            }

            var janela = servicoDeConversas.ObterJanelaDeContexto(conversa, requisicao.Mensagem!.Trim());
            contextoTruncado = janela.Truncada;
            historico = janela.Mensagens.Take(janela.Mensagens.Count - 1).ToList();
        }

        try
        {
            var resposta = await servicoDoAssistente.ObterRespostaAsync(
                requisicao,
                historico,
                cancellationToken);

            if (conversa is not null)
            {
                var identidade = new IdentidadeDeBanco
                {
                    Provedor = requisicao.ContextoDeBanco!.Provedor,
                    NomeDoBanco = requisicao.ContextoDeBanco.NomeDoBanco,
                    Versao = requisicao.ContextoDeBanco.Versao
                };

                await servicoDeConversas.PersistirTrocaAsync(
                    conversa.Id,
                    identidade,
                    requisicao.Mensagem!.Trim(),
                    resposta,
                    requisicao.ProvedorDeIa);
            }

            Response.Headers[CabecalhoDeContextoTruncado] = contextoTruncado ? "true" : "false";

            return Ok(Dtos.RespostaDeConsultaDoAssistente.Criar(resposta));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ProvedorDeIaNaoSuportadoException)
        {
            return BadRequest(new { mensagem = MensagensDeValidacao.ProvedorDeIaNaoSuportado });
        }
        catch (RespostaDoAssistenteInvalidaException)
        {
            return StatusCode(
                StatusCodes.Status422UnprocessableEntity,
                new { mensagem = MensagensDeValidacao.RespostaDoAssistenteInvalida });
        }
        catch (FalhaNoProvedorDeIaException excecao)
        {
            // Nunca loga a chave, o corpo do pedido ou o contexto em claro; a
            // mensagem traz apenas o motivo genérico/status do provedor.
            logger.LogInformation(
                excecao,
                "Falha ao gerar consulta pelo provedor de IA '{ProvedorDeIa}'.",
                requisicao!.ProvedorDeIa);
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { mensagem = MensagensDeValidacao.FalhaAoGerarConsulta });
        }
        catch (Exception excecao)
        {
            logger.LogError(
                excecao,
                "Falha inesperada ao gerar consulta pelo provedor de IA '{ProvedorDeIa}'.",
                requisicao!.ProvedorDeIa);
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { mensagem = MensagensDeValidacao.FalhaAoGerarConsulta });
        }
    }

    /// <summary>
    /// Valida a entrada manualmente (o filtro automático de ModelState está
    /// desabilitado no Program.cs) e devolve a primeira mensagem de erro em
    /// pt-BR, seguindo o contrato em <c>contracts/api.md</c>.
    /// </summary>
    private string? ValidarEntrada(RequisicaoDeConsultaDoAssistente? requisicao)
    {
        if (requisicao is null)
        {
            return "Corpo da requisição inválido.";
        }

        if (!fabricaDeProvedores.Suporta(requisicao.ProvedorDeIa))
        {
            return MensagensDeValidacao.ProvedorDeIaNaoSuportado;
        }

        if (string.IsNullOrWhiteSpace(requisicao.ChaveDeApi))
        {
            return "Informe a chave de API do provedor de IA.";
        }

        var mensagem = requisicao.Mensagem?.Trim() ?? string.Empty;
        if (mensagem.Length < 3)
        {
            return "Informe uma mensagem para gerar a consulta (mínimo de 3 caracteres).";
        }

        if (mensagem.Length > 2000)
        {
            return "A mensagem deve ter no máximo 2000 caracteres.";
        }

        if (requisicao.ContextoDeBanco is null)
        {
            return "O contexto do banco é obrigatório.";
        }

        if (requisicao.ContextoDeBanco.Tabelas.Count == 0)
        {
            return "O contexto do banco deve conter tabelas para gerar a consulta.";
        }

        if (requisicao.ContextoDeBanco.Tabelas.Count > 200)
        {
            return "O contexto do banco deve conter no máximo 200 tabelas.";
        }

        return null;
    }

    /// <summary>
    /// Compara a identidade registrada na conversa com o contexto de banco da
    /// requisição atual (provedor + nome do banco, ignorando maiúsculas —
    /// FR-013/D7). A versão não participa da comparação: pequenas variações de
    /// versão não devem bloquear a continuação.
    /// </summary>
    private static bool IdentidadeCorresponde(
        IdentidadeDeBanco identidadeRegistrada,
        EsquemaDeBanco contextoAtual) =>
        string.Equals(identidadeRegistrada.Provedor, contextoAtual.Provedor, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(identidadeRegistrada.NomeDoBanco, contextoAtual.NomeDoBanco, StringComparison.OrdinalIgnoreCase);
}