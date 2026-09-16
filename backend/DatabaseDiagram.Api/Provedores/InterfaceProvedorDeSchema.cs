using DatabaseDiagram.Api.Modelos;

namespace DatabaseDiagram.Api.Provedores;

/// <summary>
/// Contrato mínimo do provedor de esquema. Cada banco implementa a sua própria
/// introspecção de forma isolada (constituição IV).
/// </summary>
public interface InterfaceProvedorDeSchema
{
    /// <summary>Identificador do provedor, ex.: <c>mysql</c>.</summary>
    string Tipo { get; }

    Task<EsquemaDeBanco> ObterEsquemaAsync(ConexaoDeBanco conexao, CancellationToken cancellationToken);
}
