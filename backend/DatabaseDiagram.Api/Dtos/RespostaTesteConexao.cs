namespace DatabaseDiagram.Api.Dtos;

/// <summary>
/// Resposta de <c>POST /api/conexoes/teste</c>. Nunca retorna senha ou
/// connection string.
/// </summary>
public sealed class RespostaTesteConexao
{
    public bool Sucesso { get; init; }

    public required string Provedor { get; init; }

    /// <summary>Presente somente em erro; texto em pt-BR.</summary>
    public string? Mensagem { get; init; }

    public static RespostaTesteConexao DeSucesso(string provedor) =>
        new() { Sucesso = true, Provedor = provedor };

    public static RespostaTesteConexao DeErro(string provedor, string mensagem) =>
        new() { Sucesso = false, Provedor = provedor, Mensagem = mensagem };
}
