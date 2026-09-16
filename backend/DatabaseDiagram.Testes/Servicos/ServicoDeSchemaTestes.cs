using System.Threading;
using System.Threading.Tasks;
using DatabaseDiagram.Api.Modelos;
using DatabaseDiagram.Api.Provedores;
using DatabaseDiagram.Api.Servicos;

namespace DatabaseDiagram.Testes.Servicos;

public class ServicoDeSchemaTestes
{
    [Fact]
    public async Task ObterAsync_ComProvedorRegistrado_RetornaEsquemaDoProvedor()
    {
        var esperado = new EsquemaDeBanco
        {
            Provedor = "mysql",
            NomeDoBanco = "erp"
        };

        var fabrica = new FabricaDeProvedoresDeSchema([new ProvedorFalso(esperado)]);
        var servico = new ServicoDeSchema(fabrica);

        var resultado = await servico.ObterAsync(
            CriarConexao(),
            CancellationToken.None);

        Assert.Same(esperado, resultado);
    }

    [Fact]
    public async Task ObterAsync_ComProvedorDesconhecido_LancaProvedorNaoSuportado()
    {
        var fabrica = new FabricaDeProvedoresDeSchema([new ProvedorFalso()]);
        var servico = new ServicoDeSchema(fabrica);
        var conexao = CriarConexao("postgres");

        await Assert.ThrowsAsync<ProvedorNaoSuportadoException>(
            () => servico.ObterAsync(conexao, CancellationToken.None));
    }

    [Theory]
    [InlineData("mysql", true)]
    [InlineData("MYSQL", true)]
    [InlineData("postgres", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void Suporta_ValidaProvedor(string? provedor, bool esperado)
    {
        var fabrica = new FabricaDeProvedoresDeSchema([new ProvedorFalso()]);

        Assert.Equal(esperado, fabrica.Suporta(provedor));
    }

    [Fact]
    public async Task ObterAsync_ComConcorrenciaLimitada_EnfileiraAteLiberar()
    {
        var provedor = new ProvedorControlavel();
        var servico = new ServicoDeSchema(
            new FabricaDeProvedoresDeSchema([provedor]),
            concorrenciaMaxima: 1,
            tempoMaximoDeEspera: TimeSpan.FromSeconds(5));
        var conexao = CriarConexao();

        var primeira = servico.ObterAsync(conexao, CancellationToken.None);
        var segunda = servico.ObterAsync(conexao, CancellationToken.None);

        await Task.Delay(100);
        Assert.Equal(1, provedor.Contagem);

        provedor.Liberar.TrySetResult();

        Assert.Equal("erp", (await primeira).NomeDoBanco);
        Assert.Equal("erp", (await segunda).NomeDoBanco);
    }

    [Fact]
    public async Task ObterAsync_ComLimiteOcupado_LancaIntrospeccaoOcupada()
    {
        var provedor = new ProvedorControlavel();
        var servico = new ServicoDeSchema(
            new FabricaDeProvedoresDeSchema([provedor]),
            concorrenciaMaxima: 1,
            tempoMaximoDeEspera: TimeSpan.FromMilliseconds(150));
        var conexao = CriarConexao();

        var ocupada = servico.ObterAsync(conexao, CancellationToken.None);

        await Assert.ThrowsAsync<IntrospeccaoOcupadaException>(
            () => servico.ObterAsync(conexao, CancellationToken.None));

        provedor.Liberar.TrySetResult();
        await ocupada;
    }

    private static ConexaoDeBanco CriarConexao(string provedor = "mysql") => new()
    {
        Provedor = provedor,
        Host = "localhost",
        Porta = 3306,
        BancoDeDados = "erp",
        Usuario = "readonly",
        Senha = "segredo"
    };

    private sealed class ProvedorFalso(
        EsquemaDeBanco? esquema = null) : InterfaceProvedorDeSchema
    {
        private readonly EsquemaDeBanco _esquema = esquema ?? new EsquemaDeBanco
        {
            Provedor = "mysql",
            NomeDoBanco = "vazio"
        };

        public string Tipo => "mysql";

        public Task<EsquemaDeBanco> ObterEsquemaAsync(
            ConexaoDeBanco conexao,
            CancellationToken cancellationToken) =>
            Task.FromResult(_esquema);
    }

    private sealed class ProvedorControlavel : InterfaceProvedorDeSchema
    {
        public int Contagem { get; private set; }

        public TaskCompletionSource Liberar { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string Tipo => "mysql";

        public async Task<EsquemaDeBanco> ObterEsquemaAsync(
            ConexaoDeBanco conexao,
            CancellationToken cancellationToken)
        {
            Contagem++;
            await Liberar.Task.WaitAsync(cancellationToken);
            return new EsquemaDeBanco { Provedor = "mysql", NomeDoBanco = "erp" };
        }
    }
}
