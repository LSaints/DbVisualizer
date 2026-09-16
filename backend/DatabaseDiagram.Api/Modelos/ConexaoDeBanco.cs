namespace DatabaseDiagram.Api.Modelos;

/// <summary>
/// Parâmetros de conexão informados pelo usuário. Transitória: criada em
/// memória por requisição e descartada ao fim. Nunca persistida, nunca logada
/// e nunca retornada em resposta de API.
/// </summary>
public sealed class ConexaoDeBanco
{
    public required string Provedor { get; init; }

    public required string Host { get; init; }

    /// <summary>Porta padrão para MySQL: 3306.</summary>
    public int Porta { get; init; } = 3306;

    public required string BancoDeDados { get; init; }

    public required string Usuario { get; init; }

    public required string Senha { get; init; }
}
