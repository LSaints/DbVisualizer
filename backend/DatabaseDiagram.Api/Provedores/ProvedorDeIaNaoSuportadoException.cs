namespace DatabaseDiagram.Api.Provedores;

/// <summary>
/// Lançada quando o provedor de IA informado não possui uma implementação
/// registrada na fábrica. Mensagem em pt-BR, sem expor detalhes.
/// </summary>
public sealed class ProvedorDeIaNaoSuportadoException(string provedor)
    : Exception($"Provedor de IA não suportado: {provedor}.")
{
    public string Provedor { get; } = provedor;
}