namespace DatabaseDiagram.Api.Provedores;

/// <summary>
/// Resolve o provedor de IA pelo identificador informado (<c>openai</c> etc.).
/// Novos provedores entram apenas registrando a implementação aqui e no DI,
/// sem alterar controllers, serviços ou frontend (constituição IV).
/// </summary>
public sealed class FabricaDeProvedoresDeIa(IEnumerable<InterfaceProvedorDeIa> provedores)
{
    private readonly IReadOnlyList<InterfaceProvedorDeIa> _provedores =
        provedores.ToList();

    public InterfaceProvedorDeIa Obter(string provedor) =>
        _provedores.FirstOrDefault(p =>
            string.Equals(p.Tipo, provedor, StringComparison.OrdinalIgnoreCase))
        ?? throw new ProvedorDeIaNaoSuportadoException(provedor);

    /// <summary>Lista somente os provedores implementados na fábrica.</summary>
    public IReadOnlyList<InterfaceProvedorDeIa> Listar() => _provedores;

    public bool Suporta(string? provedor) =>
        !string.IsNullOrWhiteSpace(provedor) &&
        _provedores.Any(p =>
            string.Equals(p.Tipo, provedor, StringComparison.OrdinalIgnoreCase));
}