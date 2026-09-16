using Microsoft.AspNetCore.Http;
using System.Data;
using System.Data.Common;
using DatabaseDiagram.Api.Dtos;
using DatabaseDiagram.Api.Infraestrutura;
using DatabaseDiagram.Api.Modelos;
using MySqlConnector;

namespace DatabaseDiagram.Api.Servicos;

/// <summary>
/// Testa uma conexão sem armazená-la. A senha e a connection string nunca são
/// retornadas nem logadas (constituição III); mensagens de erro são genéricas.
/// </summary>
public sealed class ServicoDeConexao
{
    private readonly ConfiguradorDeConexao _configurador;
    private readonly Func<string, DbConnection> _criarConexao;

    public ServicoDeConexao(ConfiguradorDeConexao configurador)
        : this(configurador, connectionString => new MySqlConnection(connectionString))
    {
    }

    internal ServicoDeConexao(ConfiguradorDeConexao configurador, Func<string, DbConnection> criarConexao)
    {
        _configurador = configurador;
        _criarConexao = criarConexao;
    }

    public async Task<RespostaTesteConexao> TestarAsync(
        ConexaoDeBanco conexao,
        CancellationToken cancellationToken)
    {
        var connectionString = _configurador.CriarConnectionString(conexao);

        await using var conexaoAbra = _criarConexao(connectionString);

        try
        {
            await conexaoAbra.OpenAsync(cancellationToken);
            return RespostaTesteConexao.DeSucesso(conexao.Provedor);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return RespostaTesteConexao.DeErro(
                conexao.Provedor,
                "Não foi possível conectar ao banco.");
        }
    }
}
