using DatabaseDiagram.Api.Dtos;
using DatabaseDiagram.Api.Modelos;
using DatabaseDiagram.Api.Provedores;
using DatabaseDiagram.Api.Servicos;

namespace DatabaseDiagram.Testes.Servicos;

public class ServicoDoAssistenteTestes
{
    private const string JsonValido = """
        {
          "consulta": "SELECT c.* FROM contratos c;",
          "explicacao": "Lista todos os contratos.",
          "objetivo": "Encontrar os contratos.",
          "tabelas": [ { "nome": "contratos", "apelido": "c", "funcao": "Tabela principal." } ],
          "relacionamentos": [],
          "filtros": [],
          "campos_retorno": { "contratos": "*" },
          "tipo_consulta": "SELECT",
          "resultado_esperado": "Uma lista de contratos.",
          "parametros": []
        }
        """;

    private static ServicoDoAssistente CriarServico(string respostaDoProvedor, out ProvedorDeIaFalso provedor)
    {
        provedor = new ProvedorDeIaFalso(respostaDoProvedor);
        var fabrica = new FabricaDeProvedoresDeIa([provedor]);
        return new ServicoDoAssistente(fabrica, new ConstrutorDeContextoDeBanco());
    }

    private static RequisicaoDeConsultaDoAssistente CriarRequisicao() => new()
    {
        ProvedorDeIa = "openai",
        ChaveDeApi = "sk-teste",
        Mensagem = "quero listar os contratos",
        ContextoDeBanco = CriarContexto()
    };

    private static EsquemaDeBanco CriarContexto() => new()
    {
        Provedor = "mysql",
        NomeDoBanco = "erp",
        Versao = "8.0.35",
        Tabelas =
        [
            new TabelaDeBanco
            {
                Esquema = "erp",
                Nome = "contratos",
                Colunas = [new ColunaDeBanco { Nome = "id", TipoDoDado = "int", PosicaoOrdinal = 1 }]
            }
        ],
        Relacionamentos = []
    };

    [Fact]
    public async Task ObterRespostaAsync_ComJsonValido_RetornaRespostaEstruturada()
    {
        var servico = CriarServico(JsonValido, out var provedor);

        var resposta = await servico.ObterRespostaAsync(CriarRequisicao(), CancellationToken.None);

        Assert.Equal("SELECT c.* FROM contratos c;", resposta.Consulta);
        Assert.Equal("Lista todos os contratos.", resposta.Explicacao);
        Assert.Equal("contratos", Assert.Single(resposta.Tabelas).Nome);
        Assert.Equal("*", resposta.CamposRetorno["contratos"]);
        Assert.Equal(1, provedor.Chamadas);
    }

    [Fact]
    public async Task ObterRespostaAsync_ComJsonEmbrulhadoEmFencesMarkdown_RetornaRespostaEstruturada()
    {
        var jsonEmbrulhado = $"```json\n{JsonValido}\n```";
        var servico = CriarServico(jsonEmbrulhado, out _);

        var resposta = await servico.ObterRespostaAsync(CriarRequisicao(), CancellationToken.None);

        Assert.Equal("SELECT c.* FROM contratos c;", resposta.Consulta);
    }

    [Fact]
    public async Task ObterRespostaAsync_ComTextoAntesEDepoisDoJson_RetornaRespostaEstruturada()
    {
        var jsonComTexto = $"Aqui está:\n{JsonValido}\nEspero ter ajudado.";
        var servico = CriarServico(jsonComTexto, out _);

        var resposta = await servico.ObterRespostaAsync(CriarRequisicao(), CancellationToken.None);

        Assert.Equal("SELECT c.* FROM contratos c;", resposta.Consulta);
    }

    [Fact]
    public async Task ObterRespostaAsync_ComJsonInvalido_RejeitaComRespostaInvalida()
    {
        var servico = CriarServico("isto não é um json", out _);

        await Assert.ThrowsAsync<RespostaDoAssistenteInvalidaException>(
            () => servico.ObterRespostaAsync(CriarRequisicao(), CancellationToken.None));
    }

    [Fact]
    public async Task ObterRespostaAsync_SemConsulta_LancaRespostaInvalida()
    {
        var servico = CriarServico(
            """{ "explicacao": "sem a consulta." }""", out _);

        await Assert.ThrowsAsync<RespostaDoAssistenteInvalidaException>(
            () => servico.ObterRespostaAsync(CriarRequisicao(), CancellationToken.None));
    }

    [Fact]
    public async Task ObterRespostaAsync_SemExplicacao_LancaRespostaInvalida()
    {
        var servico = CriarServico(
            """{ "consulta": "SELECT * FROM contratos;" }""", out _);

        await Assert.ThrowsAsync<RespostaDoAssistenteInvalidaException>(
            () => servico.ObterRespostaAsync(CriarRequisicao(), CancellationToken.None));
    }

    [Theory]
    [InlineData("UPDATE")]
    [InlineData("INSERT")]
    [InlineData("DELETE")]
    [InlineData("CREATE TABLE")]
    public async Task ObterRespostaAsync_ComTipoDeEscrita_RejeitaESeMantemSomenteLeitura(string tipoDeEscrita)
    {
        var jsonDeEscrita = JsonValido.Replace(
            "\"tipo_consulta\": \"SELECT\"",
            $"\"tipo_consulta\": \"{tipoDeEscrita}\"");
        var servico = CriarServico(jsonDeEscrita, out var provedor);

        await Assert.ThrowsAsync<RespostaDoAssistenteInvalidaException>(
            () => servico.ObterRespostaAsync(CriarRequisicao(), CancellationToken.None));

        Assert.Equal(1, provedor.Chamadas);
    }

    [Fact]
    public async Task ObterRespostaAsync_QuandoOProvedorFalha_LancaErroGenericoSemExporAChave()
    {
        var fabrica = new FabricaDeProvedoresDeIa([new ProvedorDeIaFalsoFalhando()]);
        var servico = new ServicoDoAssistente(fabrica, new ConstrutorDeContextoDeBanco());

        var excecao = await Assert.ThrowsAsync<FalhaNoProvedorDeIaException>(
            () => servico.ObterRespostaAsync(CriarRequisicao(), CancellationToken.None));

        Assert.Equal("Não foi possível gerar a consulta pelo provedor selecionado.", excecao.Message);
        Assert.DoesNotContain("sk-teste", excecao.Message);
    }

    [Fact]
    public async Task ObterRespostaAsync_SemTabelasNoContexto_NaoChamaOProvedor()
    {
        var servico = CriarServico(JsonValido, out var provedor);
        var requisicao = CriarRequisicao();
        requisicao.ContextoDeBanco = new EsquemaDeBanco
        {
            Provedor = "mysql",
            NomeDoBanco = "erp",
            Tabelas = [],
            Relacionamentos = []
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => servico.ObterRespostaAsync(requisicao, CancellationToken.None));

        Assert.Equal(0, provedor.Chamadas);
    }

    [Fact]
    public async Task ObterRespostaAsync_ComHistorico_RepassaMensagensDaConversaAoProvedorNaOrdem()
    {
        var servico = CriarServico(JsonValido, out var provedor);
        var historico = new List<MensagemDaConversa>
        {
            new() { Papel = "usuario", Conteudo = "primeira pergunta", CriadaEm = "2026-01-01T00:00:00Z" },
            new() { Papel = "assistente", Conteudo = "resposta anterior", CriadaEm = "2026-01-01T00:00:01Z" }
        };

        await servico.ObterRespostaAsync(CriarRequisicao(), historico, CancellationToken.None);

        Assert.NotNull(provedor.UltimoPedido);
        Assert.NotNull(provedor.UltimoPedido!.MensagensDaConversa);
        Assert.Equal(2, provedor.UltimoPedido.MensagensDaConversa!.Count);
        Assert.Equal("usuario", provedor.UltimoPedido.MensagensDaConversa[0].Papel);
        Assert.Equal("assistente", provedor.UltimoPedido.MensagensDaConversa[1].Papel);
    }

    [Fact]
    public async Task ObterRespostaAsync_ComConversaCurta_EnviaTodasAsMensagensSemTruncamento()
    {
        var servico = CriarServico(JsonValido, out var provedor);
        var historico = Enumerable.Range(0, 4)
            .Select(indice => new MensagemDaConversa
            {
                Papel = indice % 2 == 0 ? "usuario" : "assistente",
                Conteudo = $"mensagem {indice}",
                CriadaEm = "2026-01-01T00:00:00Z"
            })
            .ToList();

        await servico.ObterRespostaAsync(CriarRequisicao(), historico, CancellationToken.None);

        Assert.Equal(4, provedor.UltimoPedido!.MensagensDaConversa!.Count);
    }

    [Fact]
    public async Task ObterRespostaAsync_ComCancelamento_RepassaOCancelamentoSemConverterEm502()
    {
        var servico = CriarServico(JsonValido, out _);
        using var cancelamento = new CancellationTokenSource();
        cancelamento.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => servico.ObterRespostaAsync(CriarRequisicao(), cancelamento.Token));
    }

    private sealed class ProvedorDeIaFalso(string conteudo) : InterfaceProvedorDeIa
    {
        public string Tipo => "openai";
        public string Rotulo => "OpenAI";
        public int Chamadas { get; private set; }
        public PedidoDeResposta? UltimoPedido { get; private set; }

        public Task<string> ObterRespostaAsync(
            PedidoDeResposta pedido,
            string chaveDeApi,
            CancellationToken cancellationToken)
        {
            Chamadas++;
            UltimoPedido = pedido;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(conteudo);
        }
    }

    private sealed class ProvedorDeIaFalsoFalhando : InterfaceProvedorDeIa
    {
        public string Tipo => "openai";
        public string Rotulo => "OpenAI";

        public Task<string> ObterRespostaAsync(
            PedidoDeResposta pedido,
            string chaveDeApi,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Falha de rede simulada pelo provedor.");
    }
}