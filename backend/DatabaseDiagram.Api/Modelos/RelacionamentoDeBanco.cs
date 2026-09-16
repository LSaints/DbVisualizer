namespace DatabaseDiagram.Api.Modelos;

/// <summary>
/// Representa uma chave estrangeira (origem → destino). Referencia as tabelas
/// de origem e destino por esquema + nome (+ coluna), sem objeto aninhado.
/// </summary>
public sealed class RelacionamentoDeBanco
{
    public required string EsquemaOrigem { get; init; }

    public required string TabelaOrigem { get; init; }

    public required string ColunaOrigem { get; init; }

    public required string EsquemaDestino { get; init; }

    public required string TabelaDestino { get; init; }

    public required string ColunaDestino { get; init; }

    /// <summary>Nome da constraint, quando disponível.</summary>
    public string? NomeDaRestricao { get; init; }

    /// <summary>Regra <c>ON UPDATE</c>, quando disponível.</summary>
    public string? RegraDeAtualizacao { get; init; }

    /// <summary>Regra <c>ON DELETE</c>, quando disponível.</summary>
    public string? RegraDeExclusao { get; init; }
}
