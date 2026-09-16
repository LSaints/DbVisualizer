namespace DatabaseDiagram.Api.Modelos;

/// <summary>Representa uma coluna de uma tabela.</summary>
public sealed class ColunaDeBanco
{
    public required string Nome { get; init; }

    /// <summary>Tipo base, ex.: <c>int</c>, <c>varchar</c>.</summary>
    public required string TipoDoDado { get; init; }

    /// <summary>Tipo completo, ex.: <c>int unsigned</c>, <c>varchar(255)</c>.</summary>
    public string? TipoCompletoDoDado { get; init; }

    /// <summary>Default da coluna, quando disponível.</summary>
    public string? ValorPadrao { get; init; }

    /// <summary>Comentário da coluna, quando disponível.</summary>
    public string? Comentario { get; init; }

    /// <summary>Posição da coluna na tabela (1-based).</summary>
    public int PosicaoOrdinal { get; init; }

    /// <summary>Se a coluna aceita NULL.</summary>
    public bool PodeSerNula { get; init; }

    /// <summary>Se participa da chave primária.</summary>
    public bool ChavePrimaria { get; set; }

    /// <summary>Se é origem de uma FK.</summary>
    public bool ChaveEstrangeira { get; set; }

    /// <summary>Se possui auto incremento, quando disponível.</summary>
    public bool AutoIncremento { get; init; }
}
