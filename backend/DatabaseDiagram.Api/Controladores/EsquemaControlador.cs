using DatabaseDiagram.Api.Dtos;
using DatabaseDiagram.Api.Provedores;
using DatabaseDiagram.Api.Servicos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DatabaseDiagram.Api.Controladores;

/// <summary>
/// Realiza a introspecção (somente leitura) do banco e retorna o schema comum.
/// Limitada por janela de tempo por cliente e por concorrência no
/// <see cref="ServicoDeSchema"/> para proteger o banco alvo.
/// </summary>
[ApiController]
[Route("api/esquema")]
[EnableRateLimiting("IntrospeccaoPorCliente")]
public sealed class EsquemaControlador(
    ServicoDeSchema servicoDeSchema,
    FabricaDeProvedoresDeSchema fabricaDeProvedores,
    ILogger<EsquemaControlador> logger) : ControllerBase
{
    /// <summary>
    /// <c>POST /api/esquema</c>. Sucesso: <c>200</c> com o
    /// <see cref="DatabaseDiagram.Api.Modelos.EsquemaDeBanco"/>. Erro de
    /// conexão/introspecção: <c>502</c> com <c>{"mensagem":"..."}</c>.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Obter(
        RequisicaoConexao requisicao,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { mensagem = MensagensDeValidacao.PrimeiraMensagem(ModelState) });
        }

        if (!fabricaDeProvedores.Suporta(requisicao.Provedor))
        {
            return BadRequest(new { mensagem = "Provedor de banco não suportado." });
        }

        try
        {
            var esquema = await servicoDeSchema.ObterAsync(
                requisicao.ParaConexaoDeBanco(),
                cancellationToken);

            return Ok(RespostaEsquema.Criar(esquema));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IntrospeccaoOcupadaException excecao)
        {
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                new { mensagem = excecao.Message });
        }
        catch (Exception excecao)
        {
            // Loga detalhes internos para diagnóstico local, sem dados sensíveis
            // (a senha e a connection string nunca são logadas).
            logger.LogError(
                excecao,
                "Falha na introspecção do schema do banco de dados '{BancoDeDados}'.",
                requisicao.BancoDeDados);

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { mensagem = "Não foi possível carregar o schema." });
        }
    }
}
