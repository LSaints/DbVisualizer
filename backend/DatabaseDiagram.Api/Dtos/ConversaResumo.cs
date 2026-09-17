namespace DatabaseDiagram.Api.Dtos;

/// <summary>
/// Item de <c>GET /api/conversas</c>. Resumo leve para listagem, sem
/// incluir mensagens (data-model.md/contracts/api.md).
/// </summary>
public sealed class ConversaResumo
{
    public string Id { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public string CriadaEm { get; set; } = string.Empty;

    public string AtualizadaEm { get; set; } = string.Empty;

    public string Resumo { get; set; } = string.Empty;
}
