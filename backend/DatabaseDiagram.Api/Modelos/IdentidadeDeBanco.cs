namespace DatabaseDiagram.Api.Modelos;

/// <summary>
/// Identidade do banco registrada na conversa: provedor, nome e versão
/// (quando disponível). Usada para detectar divergência de contexto ao
/// continuar uma conversa (FR-013/D7).
/// </summary>
public sealed class IdentidadeDeBanco
{
    public string Provedor { get; set; } = string.Empty;

    public string NomeDoBanco { get; set; } = string.Empty;

    public string? Versao { get; set; }
}
