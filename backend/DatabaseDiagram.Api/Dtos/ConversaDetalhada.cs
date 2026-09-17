using DatabaseDiagram.Api.Dtos;

namespace DatabaseDiagram.Api.Dtos;

/// <summary>
/// Resposta de <c>GET /api/conversas/{id}</c>. Inclui tudo do
/// <see cref="ConversaResumo"/> acrescido do contexto de banco e das
/// mensagens completas (contracts/api.md).
/// </summary>
public sealed class ConversaDetalhada
{
    public string Id { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public string CriadaEm { get; set; } = string.Empty;

    public string AtualizadaEm { get; set; } = string.Empty;

    public Modelos.IdentidadeDeBanco? ContextoDeBanco { get; set; }

    public List<Modelos.MensagemDaConversa> Mensagens { get; set; } = [];
}
