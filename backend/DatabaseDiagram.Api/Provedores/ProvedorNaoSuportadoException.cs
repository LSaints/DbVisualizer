namespace DatabaseDiagram.Api.Provedores;

/// <summary>
/// Lançada quando o provedor informado não possui um provider registrado na
/// fábrica. Mensagem em pt-BR, sem expor detalhes.
/// </summary>
public sealed class ProvedorNaoSuportadoException(string provedor)
    : Exception($"Provedor de banco não suportado: {provedor}.")
{
    public string Provedor { get; } = provedor;
}
