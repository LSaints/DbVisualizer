namespace DatabaseDiagram.Api.Dtos;

/// <summary>
/// Corpo do <c>POST /api/conversas</c>. O <c>titulo</c> é opcional;
/// se ausente, a conversa nasce com "Nova conversa" (D6/contracts/api.md).
/// </summary>
public sealed class CriarConversa
{
    public string? Titulo { get; set; }
}
