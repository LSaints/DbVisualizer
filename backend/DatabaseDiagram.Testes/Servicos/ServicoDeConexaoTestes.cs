using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using DatabaseDiagram.Api.Infraestrutura;
using DatabaseDiagram.Api.Modelos;
using DatabaseDiagram.Api.Servicos;

namespace DatabaseDiagram.Testes.Servicos;

public class ServicoDeConexaoTestes
{
    [Fact]
    public async Task TestarAsync_ComConexaoComSucesso_RetornaSucesso()
    {
        var conexao = CriarConexao();
        var servico = new ServicoDeConexao(
            new ConfiguradorDeConexao(),
            _ => new ConexaoDeBancoFalsa());

        var resposta = await servico.TestarAsync(conexao, CancellationToken.None);

        Assert.True(resposta.Sucesso);
        Assert.Equal("mysql", resposta.Provedor);
        Assert.Null(resposta.Mensagem);
    }

    [Fact]
    public async Task TestarAsync_ComFalha_RetornaErroSemExporSegredos()
    {
        const string senha = "segredo-super-secreto";
        var conexao = CriarConexao(senha: senha);
        var servico = new ServicoDeConexao(
            new ConfiguradorDeConexao(),
            _ => new ConexaoDeBancoFalsa(deveFalhar: true));

        var resposta = await servico.TestarAsync(conexao, CancellationToken.None);

        Assert.False(resposta.Sucesso);
        Assert.Equal("mysql", resposta.Provedor);
        Assert.Equal("Não foi possível conectar ao banco.", resposta.Mensagem);
        Assert.DoesNotContain(senha, resposta.Mensagem);
        Assert.DoesNotContain("Server=", resposta.Mensagem);
        Assert.DoesNotContain("Password=", resposta.Mensagem);
    }

    [Fact]
    public async Task TestarAsync_RepassaAStringDeConexaoEmMemoria()
    {
        var stringsDeConexaoRecebidas = new List<string>();
        var servico = new ServicoDeConexao(
            new ConfiguradorDeConexao(),
            stringDeConexao =>
            {
                stringsDeConexaoRecebidas.Add(stringDeConexao);
                return new ConexaoDeBancoFalsa();
            });

        await servico.TestarAsync(CriarConexao(), CancellationToken.None);

        var stringDeConexao = Assert.Single(stringsDeConexaoRecebidas);
        Assert.Contains("Server=localhost", stringDeConexao);
        Assert.Contains("Database=erp", stringDeConexao);
    }

    private static ConexaoDeBanco CriarConexao(string senha = "segredo") => new()
    {
        Provedor = "mysql",
        Host = "localhost",
        Porta = 3306,
        BancoDeDados = "erp",
        Usuario = "readonly",
        Senha = senha
    };

    /// <summary>
    /// Conexão <see cref="DbConnection"/> simulada para exercitar o serviço
    /// sem tocar em um MySQL real.
    /// </summary>
    private sealed class ConexaoDeBancoFalsa(bool deveFalhar = false) : DbConnection
    {
        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => string.Empty;
        public override string DataSource => string.Empty;
        public override string ServerVersion => string.Empty;
        public override ConnectionState State => ConnectionState.Closed;

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            if (deveFalhar)
            {
                throw new InvalidOperationException("Access denied for user 'readonly'@'localhost'");
            }

            return Task.CompletedTask;
        }

        public override void Open() => OpenAsync(CancellationToken.None).GetAwaiter().GetResult();
        public override void Close() { }
        public override void ChangeDatabase(string databaseName) { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }
}
