namespace DatabaseDiagram.Api.Modelos;

/// <summary>
/// Lote adicional de tabelas ("carregar mais"), com os relacionamentos que
/// partem dessas tabelas. Não repete tabelas/colunas já conhecidas pelo
/// cliente.
/// </summary>
public sealed class PaginaDeTabelas
{
    public List<TabelaDeBanco> Tabelas { get; init; } = [];

    public List<RelacionamentoDeBanco> Relacionamentos { get; init; } = [];

    /// <summary>Se ainda há tabelas além das retornadas neste lote.</summary>
    public bool TemMaisTabelas { get; init; }
}
