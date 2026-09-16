namespace DatabaseDiagram.Api.Provedores;

/// <summary>
/// Lançada quando a resposta do provedor não pôde ser interpretada como o
/// contrato estruturado esperado (resposta <c>422</c>).
/// </summary>
public sealed class RespostaDoAssistenteInvalidaException()
    : Exception("A resposta do assistente não pôde ser interpretada.")
{
}