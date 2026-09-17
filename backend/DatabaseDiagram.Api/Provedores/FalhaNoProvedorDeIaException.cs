namespace DatabaseDiagram.Api.Provedores;

/// <summary>
/// Lançada quando a chamada ao provedor de IA falha (rede, chave inválida,
/// serviço indisponível, timeout). Resposta <c>502</c> com mensagem genérica,
/// sem expor a chave ou detalhes internos.
/// </summary>
public sealed class FalhaNoProvedorDeIaException : Exception
{
    public FalhaNoProvedorDeIaException(string? mensagem = null)
        : base(mensagem ?? "Não foi possível gerar a consulta pelo provedor selecionado.")
    {
    }
}