using DatabaseDiagram.Api.Controladores;
using DatabaseDiagram.Api.Dtos;
using DatabaseDiagram.Api.Modelos;
using DatabaseDiagram.Api.Servicos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace DatabaseDiagram.Testes.Controladores;

public class ConversasControladorTestes : IDisposable
{
    private readonly string _diretorioTemporario;
    private readonly ConversasControlador _controlador;

    public ConversasControladorTestes()
    {
        _diretorioTemporario = Path.Combine(
            Path.GetTempPath(),
            $"conversas-testes-{Guid.NewGuid():N}");
        var servico = new ServicoDeConversas(_diretorioTemporario);
        _controlador = new ConversasControlador(
            servico,
            NullLogger<ConversasControlador>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_diretorioTemporario))
        {
            Directory.Delete(_diretorioTemporario, recursive: true);
        }
    }

    [Fact]
    public async Task Criar_ComCorpoVazio_Retorna201ComNovaConversa()
    {
        var resultado = await _controlador.Criar(null, CancellationToken.None);

        var criada = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(201, criada.StatusCode);

        var resumo = Assert.IsType<ConversaResumo>(criada.Value);
        Assert.Equal("Nova conversa", resumo.Titulo);
        Assert.False(string.IsNullOrEmpty(resumo.Id));
    }

    [Fact]
    public async Task Criar_ComTituloValido_Retorna201ComTituloPersistido()
    {
        var resultado = await _controlador.Criar(
            new CriarConversa { Titulo = "Contratos de março" },
            CancellationToken.None);

        var criada = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(201, criada.StatusCode);

        var resumo = Assert.IsType<ConversaResumo>(criada.Value);
        Assert.Equal("Contratos de março", resumo.Titulo);
    }

    [Fact]
    public async Task Criar_ComTituloMaiorQue100_Retorna400()
    {
        var titulo = new string('a', 101);
        var resultado = await _controlador.Criar(
            new CriarConversa { Titulo = titulo },
            CancellationToken.None);

        var erro = Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Contains("1 e 100", MensagemDe(erro));
    }

    [Fact]
    public async Task Listar_SemConversas_RetornaListaVazia()
    {
        var resultado = await _controlador.Listar(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var lista = Assert.IsAssignableFrom<List<ConversaResumo>>(ok.Value);
        Assert.Empty(lista);
    }

    [Fact]
    public async Task Listar_ComConversas_RetornaOrdenadoPorAtualizadaEmDecrescente()
    {
        await _controlador.Criar(new CriarConversa { Titulo = "Primeira" }, CancellationToken.None);
        await Task.Delay(10); // Garante timestamp diferente
        await _controlador.Criar(new CriarConversa { Titulo = "Segunda" }, CancellationToken.None);

        var resultado = await _controlador.Listar(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var lista = Assert.IsAssignableFrom<List<ConversaResumo>>(ok.Value);
        Assert.Equal(2, lista.Count);
        Assert.Equal("Segunda", lista[0].Titulo);
        Assert.Equal("Primeira", lista[1].Titulo);
    }

    [Fact]
    public async Task Obter_ConversaExistente_Retorna200ComConversaDetalhada()
    {
        var id = await CriarECapturarId();

        var resultado = await _controlador.Obter(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var detalhada = Assert.IsType<ConversaDetalhada>(ok.Value);
        Assert.Equal(id, detalhada.Id);
        Assert.NotNull(detalhada.Mensagens);
        Assert.Empty(detalhada.Mensagens);
    }

    [Fact]
    public async Task Obter_ConversaComProvedorDeIaPersistido_Retorna200ComProvedorDeIa()
    {
        var criada = await _controlador.Criar(null, CancellationToken.None);
        var resumo = Assert.IsType<ConversaResumo>(Assert.IsType<ObjectResult>(criada).Value);
        var servico = new ServicoDeConversas(_diretorioTemporario);
        await servico.PersistirTrocaAsync(
            resumo.Id,
            new IdentidadeDeBanco { Provedor = "mysql", NomeDoBanco = "erp" },
            "quero contratos",
            new DatabaseDiagram.Api.Modelos.RespostaDeConsultaDoAssistente { Consulta = "SELECT 1;", Explicacao = "teste" },
            "claude");

        var resultado = await _controlador.Obter(resumo.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var detalhada = Assert.IsType<ConversaDetalhada>(ok.Value);
        Assert.Equal("claude", detalhada.ProvedorDeIa);
    }

    [Fact]
    public async Task Obter_ConversaSemProvedorDeIa_Retorna200ComProvedorDeIaNulo()
    {
        var id = await CriarECapturarId();

        var resultado = await _controlador.Obter(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var detalhada = Assert.IsType<ConversaDetalhada>(ok.Value);
        Assert.Null(detalhada.ProvedorDeIa);
    }

    [Fact]
    public async Task Obter_ConversaInexistente_Retorna404()
    {
        var resultado = await _controlador.Obter(
            Guid.NewGuid().ToString(),
            CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(resultado);
        Assert.Contains("não encontrada", MensagemDe(notFound));
    }

    [Fact]
    public async Task Renomear_ConversaExistente_Retorna200ComNovoTitulo()
    {
        var id = await CriarECapturarId();

        var resultado = await _controlador.Renomear(
            id,
            new RenomearConversa { Titulo = "Novo título" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Contains("Novo título", ok.Value?.ToString() ?? string.Empty);

        // Verifica persistência: reabre a conversa.
        var obtida = await _controlador.Obter(id, CancellationToken.None);
        var okObtida = Assert.IsType<OkObjectResult>(obtida);
        var detalhada = Assert.IsType<ConversaDetalhada>(okObtida.Value);
        Assert.Equal("Novo título", detalhada.Titulo);
    }

    [Fact]
    public async Task Renomear_ConversaInexistente_Retorna404()
    {
        var resultado = await _controlador.Renomear(
            Guid.NewGuid().ToString(),
            new RenomearConversa { Titulo = "Teste" },
            CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(resultado);
        Assert.Contains("não encontrada", MensagemDe(notFound));
    }

    [Fact]
    public async Task Renomear_ComTituloVazio_Retorna400()
    {
        var id = await CriarECapturarId();

        var resultado = await _controlador.Renomear(
            id,
            new RenomearConversa { Titulo = "" },
            CancellationToken.None);

        var erro = Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Contains("1 e 100", MensagemDe(erro));
    }

    [Fact]
    public async Task Excluir_ConversaExistente_Retorna204EDepois404()
    {
        var id = await CriarECapturarId();

        var exclusao = await _controlador.Excluir(id, CancellationToken.None);
        Assert.IsType<NoContentResult>(exclusao);

        var obtida = await _controlador.Obter(id, CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(obtida);
    }

    [Fact]
    public async Task Excluir_ConversaInexistente_Retorna404()
    {
        var resultado = await _controlador.Excluir(
            Guid.NewGuid().ToString(),
            CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(resultado);
        Assert.Contains("não encontrada", MensagemDe(notFound));
    }

    private async Task<string> CriarECapturarId()
    {
        var resultado = await _controlador.Criar(null, CancellationToken.None);
        var criada = Assert.IsType<ObjectResult>(resultado);
        var resumo = Assert.IsType<ConversaResumo>(criada.Value);
        return resumo.Id;
    }

    private static string MensagemDe(ObjectResult resultado)
    {
        var mensagem = (resultado.Value as dynamic)?.mensagem as string;
        return mensagem ?? string.Empty;
    }
}