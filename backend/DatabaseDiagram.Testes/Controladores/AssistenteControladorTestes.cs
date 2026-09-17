using DatabaseDiagram.Api;
using DatabaseDiagram.Api.Controladores;
using DatabaseDiagram.Api.Dtos;
using DatabaseDiagram.Api.Modelos;
using DatabaseDiagram.Api.Provedores;
using DatabaseDiagram.Api.Servicos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http.Json;

namespace DatabaseDiagram.Testes.Controladores;

public class AssistenteControladorTestes
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

    private static AssistenteControlador CriarControlador(out ProvedorDeIaFalso provedor) =>
        CriarControlador(out provedor, out _);

    private static AssistenteControlador CriarControlador(
        out ProvedorDeIaFalso provedor,
        out ServicoDeConversas servicoDeConversas,
        string? diretorioDeConversas = null)
    {
        provedor = new ProvedorDeIaFalso(JsonValido);
        var fabrica = new FabricaDeProvedoresDeIa([provedor]);
        var servico = new ServicoDoAssistente(fabrica, new ConstrutorDeContextoDeBanco());
        servicoDeConversas = new ServicoDeConversas(
            diretorioDeConversas ?? Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        var controlador = new AssistenteControlador(
            servico,
            fabrica,
            servicoDeConversas,
            NullLogger<AssistenteControlador>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controlador;
    }

    private static RequisicaoDeConsultaDoAssistente CriarRequisicaoValida() => new()
    {
        ProvedorDeIa = "openai",
        ChaveDeApi = "sk-teste",
        Mensagem = "quero listar os contratos",
        ContextoDeBanco = new EsquemaDeBanco
        {
            Provedor = "mysql",
            NomeDoBanco = "erp",
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
        }
    };

    [Fact]
    public async Task ObterResposta_ComRequisicaoValida_Retorna200ComContratoEstruturado()
    {
        var controlador = CriarControlador(out _);

        var resultado = await controlador.ObterResposta(CriarRequisicaoValida(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var resposta = Assert.IsType<DatabaseDiagram.Api.Modelos.RespostaDeConsultaDoAssistente>(ok.Value);
        Assert.Equal("SELECT c.* FROM contratos c;", resposta.Consulta);
        Assert.Equal("Lista todos os contratos.", resposta.Explicacao);
    }

    [Fact]
    public async Task ObterResposta_ComProvedorDeIaNaoSuportado_Retorna400EmPtBr()
    {
        var controlador = CriarControlador(out _);
        var requisicao = CriarRequisicaoValida();
        requisicao.ProvedorDeIa = "deepseek";

        var resultado = await controlador.ObterResposta(requisicao, CancellationToken.None);

        var erro = Assert.IsType<BadRequestObjectResult>(resultado);
        var corpo = erro.Value as dynamic;
        Assert.Contains("não suportado", (string)corpo!.mensagem);
    }

    [Fact]
    public async Task ObterResposta_ComChaveDeApiVazia_Retorna400EmPtBr()
    {
        var controlador = CriarControlador(out _);
        var requisicao = CriarRequisicaoValida();
        requisicao.ChaveDeApi = string.Empty;

        var resultado = await controlador.ObterResposta(requisicao, CancellationToken.None);

        var erro = Assert.IsType<BadRequestObjectResult>(resultado);
        var corpo = erro.Value as dynamic;
        Assert.Contains("chave de API", (string)corpo!.mensagem, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObterResposta_ComMensagemVazia_Retorna400EmPtBr()
    {
        var controlador = CriarControlador(out _);
        var requisicao = CriarRequisicaoValida();
        requisicao.Mensagem = string.Empty;

        var resultado = await controlador.ObterResposta(requisicao, CancellationToken.None);

        var erro = Assert.IsType<BadRequestObjectResult>(resultado);
        var corpo = erro.Value as dynamic;
        Assert.Contains("mensagem", (string)corpo!.mensagem, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObterResposta_ComMensagemMenorQueTresCaracteres_Retorna400EmPtBr()
    {
        var controlador = CriarControlador(out _);
        var requisicao = CriarRequisicaoValida();
        requisicao.Mensagem = "ab";

        var resultado = await controlador.ObterResposta(requisicao, CancellationToken.None);

        var erro = Assert.IsType<BadRequestObjectResult>(resultado);
        var corpo = erro.Value as dynamic;
        Assert.Contains("mensagem", (string)corpo!.mensagem, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObterResposta_ComContextoSemTabelas_Retorna400EmPtBr()
    {
        var controlador = CriarControlador(out _);
        var requisicao = CriarRequisicaoValida();
        requisicao.ContextoDeBanco = new EsquemaDeBanco
        {
            Provedor = "mysql",
            NomeDoBanco = "erp",
            Tabelas = [],
            Relacionamentos = []
        };

        var resultado = await controlador.ObterResposta(requisicao, CancellationToken.None);

        var erro = Assert.IsType<BadRequestObjectResult>(resultado);
        var corpo = erro.Value as dynamic;
        Assert.Contains("tabelas", (string)corpo!.mensagem, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObterResposta_SemConversaId_Retorna200ComHeaderDeContextoFalseSemPersistir()
    {
        var controlador = CriarControlador(out _, out var servicoDeConversas);

        var resultado = await controlador.ObterResposta(CriarRequisicaoValida(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(resultado);
        Assert.Equal("false", controlador.HttpContext.Response.Headers["X-Contexto-Truncado"]);
        Assert.Empty(await servicoDeConversas.ListarConversasAsync());
    }

    [Fact]
    public async Task ObterResposta_ComConversaIdInexistente_Retorna404EmPtBr()
    {
        var controlador = CriarControlador(out _, out _);
        var requisicao = CriarRequisicaoValida();
        requisicao.ConversaId = Guid.NewGuid();

        var resultado = await controlador.ObterResposta(requisicao, CancellationToken.None);

        var erro = Assert.IsType<NotFoundObjectResult>(resultado);
        var corpo = erro.Value as dynamic;
        Assert.Equal("Conversa não encontrada.", (string)corpo!.mensagem);
    }

    [Fact]
    public async Task ObterResposta_ComBancoDivergenteDoRegistradoNaConversa_Retorna409EmPtBr()
    {
        var controlador = CriarControlador(out _, out var servicoDeConversas);
        var conversa = await servicoDeConversas.CriarConversaAsync();
        await servicoDeConversas.PersistirTrocaAsync(
            conversa.Id,
            new IdentidadeDeBanco { Provedor = "mysql", NomeDoBanco = "outro-banco" },
            "primeira mensagem",
            new DatabaseDiagram.Api.Modelos.RespostaDeConsultaDoAssistente
            {
                Consulta = "SELECT 1;",
                Explicacao = "teste"
            });

        var requisicao = CriarRequisicaoValida();
        requisicao.ConversaId = Guid.Parse(conversa.Id);

        var resultado = await controlador.ObterResposta(requisicao, CancellationToken.None);

        var erro = Assert.IsType<ConflictObjectResult>(resultado);
        var corpo = erro.Value as dynamic;
        Assert.Equal("O banco conectado difere do registrado nesta conversa.", (string)corpo!.mensagem);
    }

    [Fact]
    public async Task ObterResposta_ComConversaId_PersisteATrocaEAdicionaHeaderDeContexto()
    {
        var controlador = CriarControlador(out _, out var servicoDeConversas);
        var conversa = await servicoDeConversas.CriarConversaAsync();

        var requisicao = CriarRequisicaoValida();
        requisicao.ConversaId = Guid.Parse(conversa.Id);

        var resultado = await controlador.ObterResposta(requisicao, CancellationToken.None);

        Assert.IsType<OkObjectResult>(resultado);
        Assert.Equal("false", controlador.HttpContext.Response.Headers["X-Contexto-Truncado"]);

        var detalhada = await servicoDeConversas.ObterConversaAsync(conversa.Id);
        Assert.NotNull(detalhada);
        Assert.Equal(2, detalhada!.Mensagens.Count);
        Assert.Equal("usuario", detalhada.Mensagens[0].Papel);
        Assert.Equal("assistente", detalhada.Mensagens[1].Papel);
    }

    [Fact]
    public async Task ObterResposta_ComConversaId_UsaHistoricoPersistidoComoContextoSemDuplicarTrocas()
    {
        var controlador = CriarControlador(out var provedor, out var servicoDeConversas);
        var conversa = await servicoDeConversas.CriarConversaAsync();
        var requisicao = CriarRequisicaoValida();
        requisicao.ConversaId = Guid.Parse(conversa.Id);

        await controlador.ObterResposta(requisicao, CancellationToken.None);
        await controlador.ObterResposta(requisicao, CancellationToken.None);

        var detalhada = await servicoDeConversas.ObterConversaAsync(conversa.Id);
        Assert.Equal(4, detalhada!.Mensagens.Count);
        Assert.Equal(2, provedor.Chamadas);
    }

    [Fact]
    public async Task ObterResposta_QuandoOProvedorFalha_NaoPersisteNada()
    {
        var fabrica = new FabricaDeProvedoresDeIa([new ProvedorDeIaFalsoFalhando()]);
        var servico = new ServicoDoAssistente(fabrica, new ConstrutorDeContextoDeBanco());
        var servicoDeConversas = new ServicoDeConversas(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        var controlador = new AssistenteControlador(
            servico, fabrica, servicoDeConversas, NullLogger<AssistenteControlador>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        var conversa = await servicoDeConversas.CriarConversaAsync();
        var requisicao = CriarRequisicaoValida();
        requisicao.ConversaId = Guid.Parse(conversa.Id);

        var resultado = await controlador.ObterResposta(requisicao, CancellationToken.None);

        Assert.IsType<ObjectResult>(resultado);
        var detalhada = await servicoDeConversas.ObterConversaAsync(conversa.Id);
        Assert.Empty(detalhada!.Mensagens);
    }

    private sealed class ProvedorDeIaFalsoFalhando : InterfaceProvedorDeIa
    {
        public string Tipo => "openai";
        public string Rotulo => "OpenAI";

        public Task<string> ObterRespostaAsync(
            PedidoDeResposta pedido,
            string chaveDeApi,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Falha de rede simulada.");
    }

    [Fact]
    public void ListarProvedores_Retorna200ComProvedoresDisponiveisNaFabrica()
    {
        var controlador = CriarControlador(out _);

        var resultado = controlador.ListarProvedores();

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var provedores = Assert.IsAssignableFrom<IEnumerable<ProvedorDeIaDisponivel>>(ok.Value);
        Assert.Contains(
            provedores,
            provedor => provedor.Provedor == "openai" && provedor.Rotulo == "OpenAI");
    }

    [Fact]
    public async Task ObterResposta_QuandoOClienteExcedeOLimite_Retorna429ComMensagemEmPtBr()
    {
        using var fabrica = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(construtor => construtor.ConfigureTestServices(servicos =>
                servicos.AddSingleton<FabricaDeProvedoresDeIa>(
                    new FabricaDeProvedoresDeIa([new ProvedorDeIaFalso(JsonValido)]))));
        using var cliente = fabrica.CreateClient();

        var requisicao = CriarRequisicaoValida();
        var respostas = new List<HttpResponseMessage>();
        for (var indice = 0; indice < 11; indice++)
        {
            respostas.Add(await cliente.PostAsJsonAsync("/api/assistente/consultas", requisicao));
        }

        try
        {
            Assert.Equal(HttpStatusCode.OK, respostas[0].StatusCode);

            var ultima = respostas[^1];
            Assert.Equal(HttpStatusCode.TooManyRequests, ultima.StatusCode);

            var corpo = await ultima.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            Assert.Equal(
                "Muitas requisições. Aguarde um instante e tente novamente.",
                corpo?["mensagem"]);
        }
        finally
        {
            foreach (var resposta in respostas)
            {
                resposta.Dispose();
            }
        }
    }

    private sealed class ProvedorDeIaFalso(string conteudo) : InterfaceProvedorDeIa
    {
        public string Tipo => "openai";
        public string Rotulo => "OpenAI";
        public int Chamadas { get; private set; }

        public Task<string> ObterRespostaAsync(
            PedidoDeResposta pedido,
            string chaveDeApi,
            CancellationToken cancellationToken)
        {
            Chamadas++;
            return Task.FromResult(conteudo);
        }
    }
}