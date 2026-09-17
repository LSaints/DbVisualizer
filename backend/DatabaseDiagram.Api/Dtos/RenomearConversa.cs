namespace DatabaseDiagram.Api.Dtos;

/// <summary>
/// Corpo do <c>PATCH /api/conversas/{id}/titulo</c>. O <c>titulo</c> é
/// obrigatório, tamanho 1–100 caracteres (contracts/api.md).
/// </summary>
public sealed class RenomearConversa
{
    public string? Titulo { get; set; }
}
