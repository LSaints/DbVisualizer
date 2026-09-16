using DatabaseDiagram.Api.Modelos;

namespace DatabaseDiagram.Api.Provedores;

/// <summary>
/// Contrato mínimo do provider de schema. Cada banco implementa a sua própria
/// introspecção de forma isolada (constituição IV).
/// </summary>
public interface InterfaceProvedorDeSchema
{
    /// <summary>Identificador do provider, ex.: <c>mysql</c>.</summary>
    string Tipo { get; }

    Task<EsquemaDeBanco> ObterSchemaAsync(ConexaoDeBanco conexao, CancellationToken cancellationToken);
}
