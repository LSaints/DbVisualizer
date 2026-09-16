using DatabaseDiagram.Api.Dtos;
using DatabaseDiagram.Api.Provedores;
using DatabaseDiagram.Api.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseDiagram.Api.Controladores;

/// <summary>
/// Testa a conexão sem armazená-la. Nunca retorna nem loga a senha.
/// </summary>
[ApiController]
[Route("api/conexoes")]
public sealed class ConexoesControlador(
    ServicoDeConexao servicoDeConexao,
    FabricaDeProvedoresDeSchema fabricaDeProvedores) : ControllerBase
{
    /// <summary>
    /// <c>POST /api/conexoes/teste</c>. Sucesso: <c>200</c> com
    /// <c>{"sucesso":true,...}</c>. Falha de conexão ou validação: <c>400</c>
    /// com <c>{"sucesso":false,...,"mensagem":"..."}</c> em pt-BR.
    /// </summary>
    [HttpPost("teste")]
    public async Task<IActionResult> Testar(
        RequisicaoConexao requisicao,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(RespostaTesteConexao.DeErro(
                requisicao.Provedor ?? string.Empty,
                MensagensDeValidacao.PrimeiraMensagem(ModelState)));
        }

        if (!fabricaDeProvedores.Suporta(requisicao.Provedor))
        {
            return BadRequest(RespostaTesteConexao.DeErro(
                requisicao.Provedor ?? string.Empty,
                "Provedor de banco não suportado."));
        }

        var resposta = await servicoDeConexao.TestarAsync(
            requisicao.ParaConexaoDeBanco(),
            cancellationToken);

        return resposta.Sucesso ? Ok(resposta) : StatusCode(StatusCodes.Status400BadRequest, resposta);
    }
}
