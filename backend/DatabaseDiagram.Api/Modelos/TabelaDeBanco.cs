namespace DatabaseDiagram.Api.Modelos;

/// <summary>Representa uma tabela (MVP prioriza <c>BASE TABLE</c>).</summary>
public sealed class TabelaDeBanco
{
    /// <summary>Nome do schema/database.</summary>
    public required string Esquema { get; init; }

    public required string Nome { get; init; }

    /// <summary>Ex.: <c>BASE TABLE</c>.</summary>
    public string? TipoDaTabela { get; init; }

    /// <summary>Engine, ex.: <c>InnoDB</c>.</summary>
    public string? Motor { get; init; }

    /// <summary>Comentário da tabela, quando disponível.</summary>
    public string? Comentario { get; init; }

    /// <summary>Colunas em ordem de posição.</summary>
    public List<ColunaDeBanco> Colunas { get; init; } = [];
}
