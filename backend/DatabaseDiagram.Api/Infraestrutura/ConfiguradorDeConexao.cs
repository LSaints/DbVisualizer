using DatabaseDiagram.Api.Modelos;
using MySqlConnector;

namespace DatabaseDiagram.Api.Infraestrutura;

/// <summary>
/// Constrói a connection string em memória a partir de uma
/// <see cref="ConexaoDeBanco"/>. Nunca é persistida, retornada ou logada.
/// </summary>
public sealed class ConfiguradorDeConexao
{
    /// <summary>
    /// Cria a connection string usando <see cref="MySqlConnectionStringBuilder"/>
    /// (somente em memória). A string resultante contém a senha e não deve ser
    /// utilizada em respostas ou logs.
    /// </summary>
    public string CriarConnectionString(ConexaoDeBanco conexao)
    {
        return new MySqlConnectionStringBuilder
        {
            Server = conexao.Host,
            Port = (uint)conexao.Porta,
            Database = conexao.BancoDeDados,
            UserID = conexao.Usuario,
            Password = conexao.Senha,
            ConnectionTimeout = 5,
            // Pool pequeno: limita as conexões simultâneas contra o banco alvo,
            // mesmo com introspecções concorrentes.
            MaximumPoolSize = 8
        }.ConnectionString;
    }
}
