namespace DatabaseDiagram.Api.Modelos;

/// <summary>Raiz do schema descoberto em um database.</summary>
public sealed class EsquemaDeBanco
{
    public required string Provedor { get; init; }

    public required string NomeDoBanco { get; init; }

    /// <summary>Charset default, quando disponível.</summary>
    public string? Charset { get; init; }

    /// <summary>Collation default, quando disponível.</summary>
    public string? Collation { get; init; }

    public List<TabelaDeBanco> Tabelas { get; init; } = [];

    public List<RelacionamentoDeBanco> Relacionamentos { get; init; } = [];

    /// <summary>
    /// Aviso em pt-BR sobre limitações da introspecção (ex.: banco com muitas
    /// tabelas, resultando em schema truncado).
    /// </summary>
    public string? Aviso { get; set; }
}
