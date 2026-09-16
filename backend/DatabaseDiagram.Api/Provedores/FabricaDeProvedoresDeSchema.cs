using DatabaseDiagram.Api.Provedores.MySql;

namespace DatabaseDiagram.Api.Provedores;

/// <summary>
/// Resolve o provider de schema pelo campo <c>provedor</c> da conexão.
/// Novos bancos entram apenas registrando o provider aqui e no DI.
/// </summary>
public sealed class FabricaDeProvedoresDeSchema(IEnumerable<InterfaceProvedorDeSchema> provedores)
{
    private readonly IReadOnlyList<InterfaceProvedorDeSchema> _provedores =
        provedores.ToList();

    public InterfaceProvedorDeSchema? ObterProvider(string provedor) =>
        _provedores.FirstOrDefault(p =>
            string.Equals(p.Tipo, provedor, StringComparison.OrdinalIgnoreCase));

    public bool Suporta(string? provedor) =>
        !string.IsNullOrWhiteSpace(provedor) && ObterProvider(provedor) is not null;
}
