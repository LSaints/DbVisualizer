using DatabaseDiagram.Api.Dtos;
using DatabaseDiagram.Api.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseDiagram.Api.Controladores;

/// <summary>
/// CRUD de conversas: <c>api/conversas</c> (G2/contracts/api.md). Listagem
/// ordenada por <c>atualizadaEm</c> decrescente, criação, abertura, renomeação
/// e exclusão. Logs registram somente o id da conversa, nunca corpo de
/// mensagem (G3/G6).
/// </summary>
[ApiController]
[Route("api/conversas")]
public sealed class ConversasControlador(
    ServicoDeConversas servicoDeConversas,
    ILogger<ConversasControlador> logger) : ControllerBase
{
    /// <summary>
    /// <c>GET /api/conversas</c>. Lista as conversas, ordenadas por
    /// <c>atualizadaEm</c> decrescente.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var conversas = await servicoDeConversas.ListarConversasAsync();
        return Ok(conversas);
    }

    /// <summary>
    /// <c>POST /api/conversas</c>. Cria uma nova conversa vazia.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarConversa? corpo,
        CancellationToken cancellationToken)
    {
        var titulo = corpo?.Titulo;

        if (!string.IsNullOrWhiteSpace(titulo) && (titulo.Trim().Length < 1 || titulo.Trim().Length > 100))
        {
            return BadRequest(new { mensagem = "O título deve ter entre 1 e 100 caracteres." });
        }

        var conversa = await servicoDeConversas.CriarConversaAsync(titulo);

        logger.LogInformation("Conversa criada: {ConversaId}.", conversa.Id);

        return StatusCode(StatusCodes.Status201Created, conversa);
    }

    /// <summary>
    /// <c>GET /api/conversas/{id}</c>. Restaura a conversa completa.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Obter(string id, CancellationToken cancellationToken)
    {
        var conversa = await servicoDeConversas.ObterConversaAsync(id);

        if (conversa is null)
        {
            return NotFound(new { mensagem = "Conversa não encontrada." });
        }

        return Ok(conversa);
    }

    /// <summary>
    /// <c>PATCH /api/conversas/{id}/titulo</c>. Renomeia a conversa.
    /// </summary>
    [HttpPatch("{id}/titulo")]
    public async Task<IActionResult> Renomear(
        string id,
        [FromBody] RenomearConversa? corpo,
        CancellationToken cancellationToken)
    {
        if (corpo?.Titulo is null || corpo.Titulo.Trim().Length < 1 || corpo.Titulo.Trim().Length > 100)
        {
            return BadRequest(new { mensagem = "Informe um título entre 1 e 100 caracteres." });
        }

        var titulo = await servicoDeConversas.RenomearConversaAsync(id, corpo.Titulo);

        if (titulo is null)
        {
            return NotFound(new { mensagem = "Conversa não encontrada." });
        }

        logger.LogInformation("Conversa renomeada: {ConversaId}.", id);

        return Ok(new { id, titulo });
    }

    /// <summary>
    /// <c>DELETE /api/conversas/{id}</c>. Exclui a conversa permanentemente.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(string id, CancellationToken cancellationToken)
    {
        var excluida = await servicoDeConversas.ExcluirConversaAsync(id);

        if (!excluida)
        {
            return NotFound(new { mensagem = "Conversa não encontrada." });
        }

        logger.LogInformation("Conversa excluída: {ConversaId}.", id);

        return NoContent();
    }
}
