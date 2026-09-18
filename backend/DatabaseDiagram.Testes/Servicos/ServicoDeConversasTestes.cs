using DatabaseDiagram.Api.Modelos;
using DatabaseDiagram.Api.Servicos;

namespace DatabaseDiagram.Testes.Servicos;

public class ServicoDeConversasTestes : IDisposable
{
    private readonly string _diretorioTemporario;

    public ServicoDeConversasTestes()
    {
        _diretorioTemporario = Path.Combine(
            Path.GetTempPath(),
            $"servico-conversas-testes-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_diretorioTemporario))
        {
            Directory.Delete(_diretorioTemporario, recursive: true);
        }
    }

    [Fact]
    public async Task CriarConversaAsync_CriaArquivoComIdETituloPadrao()
    {
        var servico = CriarServico();

        var resultado = await servico.CriarConversaAsync();

        Assert.False(string.IsNullOrEmpty(resultado.Id));
        Assert.Equal("Nova conversa", resultado.Titulo);
        Assert.NotNull(resultado.CriadaEm);
        Assert.Equal(resultado.CriadaEm, resultado.AtualizadaEm);
    }

    [Fact]
    public async Task CriarConversaAsync_ComTituloInformado_UseOTitulo()
    {
        var servico = CriarServico();

        var resultado = await servico.CriarConversaAsync("Contratos");

        Assert.Equal("Contratos", resultado.Titulo);
    }

    [Fact]
    public async Task ListarConversasAsync_RetornaVazioQuandoSemConversas()
    {
        var servico = CriarServico();

        var lista = await servico.ListarConversasAsync();

        Assert.Empty(lista);
    }

    [Fact]
    public async Task ListarConversasAsync_RetornaOrdenadoPorAtualizadaEmDecrescente()
    {
        var servico = CriarServico();
        await servico.CriarConversaAsync("Primeira");
        await Task.Delay(10);
        await servico.CriarConversaAsync("Segunda");

        var lista = await servico.ListarConversasAsync();

        Assert.Equal(2, lista.Count);
        Assert.Equal("Segunda", lista[0].Titulo);
        Assert.Equal("Primeira", lista[1].Titulo);
    }

    [Fact]
    public async Task ObterConversaAsync_ConversaExistente_RetornaDetalhada()
    {
        var servico = CriarServico();
        var criada = await servico.CriarConversaAsync("Teste");

        var detalhada = await servico.ObterConversaAsync(criada.Id);

        Assert.NotNull(detalhada);
        Assert.Equal(criada.Id, detalhada!.Id);
        Assert.Equal("Teste", detalhada.Titulo);
        Assert.NotNull(detalhada.Mensagens);
        Assert.Empty(detalhada.Mensagens);
    }

    [Fact]
    public async Task ObterConversaAsync_ConversaInexistente_RetornaNull()
    {
        var servico = CriarServico();

        var resultado = await servico.ObterConversaAsync(Guid.NewGuid().ToString());

        Assert.Null(resultado);
    }

    [Fact]
    public async Task RenomearConversaAsync_ConversaExistente_AlteraOTitulo()
    {
        var servico = CriarServico();
        var criada = await servico.CriarConversaAsync();

        var titulo = await servico.RenomearConversaAsync(criada.Id, "Novo título");

        Assert.Equal("Novo título", titulo);

        // Verifica persistência: reabre.
        var detalhada = await servico.ObterConversaAsync(criada.Id);
        Assert.Equal("Novo título", detalhada!.Titulo);
    }

    [Fact]
    public async Task RenomearConversaAsync_ConversaInexistente_RetornaNull()
    {
        var servico = CriarServico();

        var resultado = await servico.RenomearConversaAsync(
            Guid.NewGuid().ToString(),
            "Título");

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ExcluirConversaAsync_ConversaExistente_ExcluiEDepoisNaoEncontra()
    {
        var servico = CriarServico();
        var criada = await servico.CriarConversaAsync();

        var excluida = await servico.ExcluirConversaAsync(criada.Id);
        Assert.True(excluida);

        var obtida = await servico.ObterConversaAsync(criada.Id);
        Assert.Null(obtida);
    }

    [Fact]
    public async Task ExcluirConversaAsync_ConversaInexistente_RetornaFalse()
    {
        var servico = CriarServico();

        var resultado = await servico.ExcluirConversaAsync(Guid.NewGuid().ToString());

        Assert.False(resultado);
    }

    [Fact]
    public async Task PersistirTrocaAsync_ConversaExistente_AdicionaMensagens()
    {
        var servico = CriarServico();
        var criada = await servico.CriarConversaAsync();
        var contexto = new IdentidadeDeBanco
        {
            Provedor = "mysql",
            NomeDoBanco = "erp",
            Versao = "8.0.35"
        };
        var resposta = new RespostaDeConsultaDoAssistente
        {
            Consulta = "SELECT * FROM contratos;",
            Explicacao = "Lista os contratos.",
            Objetivo = "Encontrar contratos.",
            TipoConsulta = "SELECT",
            ResultadoEsperado = "Lista de contratos."
        };

        await servico.PersistirTrocaAsync(criada.Id, contexto, "quero contratos", resposta);

        var detalhada = await servico.ObterConversaAsync(criada.Id);
        Assert.NotNull(detalhada);
        Assert.Equal(2, detalhada!.Mensagens.Count);
        Assert.Equal("usuario", detalhada.Mensagens[0].Papel);
        Assert.Equal("quero contratos", detalhada.Mensagens[0].Conteudo);
        Assert.Equal("assistente", detalhada.Mensagens[1].Papel);
        Assert.IsType<RespostaDeConsultaDoAssistente>(detalhada.Mensagens[1].Conteudo);
    }

    [Fact]
    public async Task PersistirTrocaAsync_PrimeraTroca_DefineTituloAutomatico()
    {
        var servico = CriarServico();
        var criada = await servico.CriarConversaAsync();
        var contexto = new IdentidadeDeBanco
        {
            Provedor = "mysql",
            NomeDoBanco = "erp"
        };
        var resposta = new RespostaDeConsultaDoAssistente
        {
            Consulta = "SELECT * FROM contratos;",
            Explicacao = "Lista.",
            Objetivo = "Encontrar.",
            TipoConsulta = "SELECT",
            ResultadoEsperado = "Lista."
        };

        await servico.PersistirTrocaAsync(
            criada.Id,
            contexto,
            "quero contratos",
            resposta);

        var detalhada = await servico.ObterConversaAsync(criada.Id);
        Assert.Equal("quero contratos", detalhada!.Titulo);
    }

    [Fact]
    public async Task PersistirTrocaAsync_MensagemLonga_TruncaTituloEm40Caracteres()
    {
        var servico = CriarServico();
        var criada = await servico.CriarConversaAsync();
        var contexto = new IdentidadeDeBanco
        {
            Provedor = "mysql",
            NomeDoBanco = "erp"
        };
        var resposta = new RespostaDeConsultaDoAssistente
        {
            Consulta = "SELECT * FROM contratos;",
            Explicacao = "Lista.",
            Objetivo = "Encontrar.",
            TipoConsulta = "SELECT",
            ResultadoEsperado = "Lista."
        };
        var mensagemLonga = new string('a', 50);

        await servico.PersistirTrocaAsync(criada.Id, contexto, mensagemLonga, resposta);

        var detalhada = await servico.ObterConversaAsync(criada.Id);
        Assert.Equal(41, detalhada!.Titulo.Length); // 40 + "…"
        Assert.EndsWith("…", detalhada.Titulo);
    }

    [Fact]
    public async Task PersistirTrocaAsync_SanitizaSegredosNaMensagem()
    {
        var servico = CriarServico();
        var criada = await servico.CriarConversaAsync();
        var contexto = new IdentidadeDeBanco
        {
            Provedor = "mysql",
            NomeDoBanco = "erp"
        };
        var resposta = new RespostaDeConsultaDoAssistente
        {
            Consulta = "SELECT * FROM contratos;",
            Explicacao = "Lista.",
            Objetivo = "Encontrar.",
            TipoConsulta = "SELECT",
            ResultadoEsperado = "Lista."
        };

        await servico.PersistirTrocaAsync(
            criada.Id,
            contexto,
            "minha chave é sk-abc123def456ghi789jkl",
            resposta);

        var detalhada = await servico.ObterConversaAsync(criada.Id);
        var conteudo = (string)detalhada!.Mensagens[0].Conteudo;
        Assert.DoesNotContain("sk-abc123def456ghi789jkl", conteudo);
        Assert.Contains("<segredo removido>", conteudo);
    }

    [Fact]
    public async Task PersistirTrocaAsync_NaoModificaTituloSeAutomaticoJaDesativado()
    {
        var servico = CriarServico();
        var criada = await servico.CriarConversaAsync("Título manual");
        var contexto = new IdentidadeDeBanco
        {
            Provedor = "mysql",
            NomeDoBanco = "erp"
        };
        var resposta = new RespostaDeConsultaDoAssistente
        {
            Consulta = "SELECT * FROM contratos;",
            Explicacao = "Lista.",
            Objetivo = "Encontrar.",
            TipoConsulta = "SELECT",
            ResultadoEsperado = "Lista."
        };

        await servico.PersistirTrocaAsync(criada.Id, contexto, "mensagem", resposta);

        var detalhada = await servico.ObterConversaAsync(criada.Id);
        Assert.Equal("Título manual", detalhada!.Titulo);
    }

    [Fact]
    public async Task PersistenciaEntreInstancias_DadosSobrevivem()
    {
        var servico1 = CriarServico();
        var criada = await servico1.CriarConversaAsync("Persistente");
        var contexto = new IdentidadeDeBanco
        {
            Provedor = "mysql",
            NomeDoBanco = "erp"
        };
        var resposta = new RespostaDeConsultaDoAssistente
        {
            Consulta = "SELECT * FROM contratos;",
            Explicacao = "Lista.",
            Objetivo = "Encontrar.",
            TipoConsulta = "SELECT",
            ResultadoEsperado = "Lista."
        };
        await servico1.PersistirTrocaAsync(criada.Id, contexto, "pedido", resposta);

        // Nova instância, mesmo diretório.
        var servico2 = CriarServico();
        var detalhada = await servico2.ObterConversaAsync(criada.Id);

        Assert.NotNull(detalhada);
        Assert.Equal("Persistente", detalhada!.Titulo);
        Assert.Equal(2, detalhada.Mensagens.Count);
    }

    [Theory]
    [InlineData("Authorization: Bearer abc123def456ghi789", "Bearer abc123def456ghi789")]
    [InlineData("use key=xyz987wvu654tsr321qpo em produção", "key=xyz987wvu654tsr321qpo")]
    public async Task PersistirTrocaAsync_SanitizaOutrosPadroesDeSegredo(string mensagem, string segredo)
    {
        var servico = CriarServico();
        var criada = await servico.CriarConversaAsync();
        var contexto = new IdentidadeDeBanco { Provedor = "mysql", NomeDoBanco = "erp" };
        var resposta = new RespostaDeConsultaDoAssistente
        {
            Consulta = "SELECT 1;",
            Explicacao = "Teste.",
            Objetivo = "Teste.",
            TipoConsulta = "SELECT",
            ResultadoEsperado = "Teste."
        };

        await servico.PersistirTrocaAsync(criada.Id, contexto, mensagem, resposta);

        var detalhada = await servico.ObterConversaAsync(criada.Id);
        var conteudo = (string)detalhada!.Mensagens[0].Conteudo;
        Assert.DoesNotContain(segredo, conteudo);
        Assert.Contains("<segredo removido>", conteudo);
    }

    [Fact]
    public void ObterJanelaDeContexto_SanitizaSegredoNaMensagemAtual()
    {
        var servico = CriarServico();
        var conversa = new Conversa { Mensagens = [] };

        var janela = servico.ObterJanelaDeContexto(
            conversa,
            "minha chave é sk-abc123def456ghi789jkl");

        var conteudoAtual = (string)janela.Mensagens[^1].Conteudo;
        Assert.DoesNotContain("sk-abc123def456ghi789jkl", conteudoAtual);
        Assert.Contains("<segredo removido>", conteudoAtual);
    }

    [Fact]
    public void ObterJanelaDeContexto_QuantidadeMaximaPadrao_E20()
    {
        var servico = CriarServico();
        var mensagens = Enumerable.Range(0, 25)
            .Select(i => new MensagemDaConversa
            {
                Papel = i % 2 == 0 ? "usuario" : "assistente",
                Conteudo = $"mensagem {i}"
            })
            .ToList();
        var conversa = new Conversa { Mensagens = mensagens };

        var janela = servico.ObterJanelaDeContexto(conversa, "atual");

        Assert.True(janela.Truncada);
        Assert.Equal(21, janela.Mensagens.Count); // 20 do histórico + atual
    }

    [Fact]
    public void ObterJanelaDeContexto_ConversaCurta_RetornaTodasSemTruncamento()
    {
        var servico = CriarServico();
        var conversa = new Conversa
        {
            Mensagens =
            [
                new MensagemDaConversa { Papel = "usuario", Conteudo = "1" },
                new MensagemDaConversa { Papel = "assistente", Conteudo = "2" }
            ]
        };

        var janela = servico.ObterJanelaDeContexto(conversa, "3ª mensagem");

        Assert.Equal(3, janela.Mensagens.Count);
        Assert.False(janela.Truncada);
    }

    [Fact]
    public void ObterJanelaDeContexto_ConversaLonga_TruncarMensagensAntigas()
    {
        var servico = CriarServico(quantidadeMaximaDeMensagens: 3);
        var mensagens = new List<MensagemDaConversa>();
        for (var i = 0; i < 10; i++)
        {
            mensagens.Add(new MensagemDaConversa
            {
                Papel = i % 2 == 0 ? "usuario" : "assistente",
                Conteudo = $"mensagem {i}"
            });
        }
        var conversa = new Conversa { Mensagens = mensagens };

        var janela = servico.ObterJanelaDeContexto(conversa, "mensagem atual");

        // 3 da janela (histórico truncado) + atual.
        Assert.Equal(4, janela.Mensagens.Count);
        Assert.True(janela.Truncada);
        Assert.Equal("mensagem atual", janela.Mensagens[^1].Conteudo);
    }

    [Fact]
    public void ObterJanelaDeContexto_MensagemAtualSempreIncluida()
    {
        var servico = CriarServico(quantidadeMaximaDeMensagens: 2);
        var mensagens = new List<MensagemDaConversa>
        {
            new() { Papel = "usuario", Conteudo = "1ª" },
            new() { Papel = "assistente", Conteudo = "2ª" },
            new() { Papel = "usuario", Conteudo = "3ª" }
        };
        var conversa = new Conversa { Mensagens = mensagens };

        var janela = servico.ObterJanelaDeContexto(conversa, "atual");

        // Janela = últimas 2 do histórico [2ª, 3ª] + atual.
        Assert.Equal(3, janela.Mensagens.Count);
        Assert.Equal("2ª", janela.Mensagens[0].Conteudo);
        Assert.Equal("3ª", janela.Mensagens[1].Conteudo);
        Assert.Equal("atual", janela.Mensagens[^1].Conteudo);
        Assert.True(janela.Truncada);
    }

    [Fact]
    public async Task PersistirTrocaAsync_ComProvedorDeIaSelecionado_GravaERestauraNaDetalhada()
    {
        var servico = CriarServico();
        var criada = await servico.CriarConversaAsync();
        var contexto = new IdentidadeDeBanco
        {
            Provedor = "mysql",
            NomeDoBanco = "erp"
        };
        var resposta = CriarRespostaPadrao();

        await servico.PersistirTrocaAsync(
            criada.Id,
            contexto,
            "quero contratos",
            resposta,
            "openai");

        var detalhada = await servico.ObterConversaAsync(criada.Id);
        Assert.NotNull(detalhada);
        Assert.Equal("openai", detalhada!.ProvedorDeIa);

        // Persistência: reabre em nova instância.
        var servico2 = CriarServico();
        var reaberta = await servico2.ObterConversaAsync(criada.Id);
        Assert.Equal("openai", reaberta!.ProvedorDeIa);
    }

    [Fact]
    public async Task PersistirTrocaAsync_TrocarDeProvedor_PreservaHistoricoERegrava()
    {
        var servico = CriarServico();
        var criada = await servico.CriarConversaAsync();
        var contexto = new IdentidadeDeBanco
        {
            Provedor = "mysql",
            NomeDoBanco = "erp"
        };

        await servico.PersistirTrocaAsync(criada.Id, contexto, "primeira", CriarRespostaPadrao(), "openai");
        await Task.Delay(10);
        await servico.PersistirTrocaAsync(criada.Id, contexto, "segunda", CriarRespostaPadrao(), "deepseek");

        var detalhada = await servico.ObterConversaAsync(criada.Id);
        Assert.Equal("deepseek", detalhada!.ProvedorDeIa);
        Assert.Equal(4, detalhada.Mensagens.Count); // histórico preservado
        Assert.Equal("usuario", detalhada.Mensagens[0].Papel);
        Assert.Equal("primeira", detalhada.Mensagens[0].Conteudo);
        Assert.Equal("assistente", detalhada.Mensagens[3].Papel);
    }

    [Fact]
    public async Task PersistirTrocaAsync_SemProvedorDeIa_ProvedorDeIaPermaneceNull()
    {
        var servico = CriarServico();
        var criada = await servico.CriarConversaAsync();
        var contexto = new IdentidadeDeBanco
        {
            Provedor = "mysql",
            NomeDoBanco = "erp"
        };

        await servico.PersistirTrocaAsync(criada.Id, contexto, "mensagem", CriarRespostaPadrao());

        var detalhada = await servico.ObterConversaAsync(criada.Id);
        Assert.Null(detalhada!.ProvedorDeIa);
    }

    private RespostaDeConsultaDoAssistente CriarRespostaPadrao() => new()
    {
        Consulta = "SELECT * FROM contratos;",
        Explicacao = "Lista.",
        Objetivo = "Encontrar.",
        TipoConsulta = "SELECT",
        ResultadoEsperado = "Lista."
    };

    private ServicoDeConversas CriarServico(int? quantidadeMaximaDeMensagens = null)
    {
        return new ServicoDeConversas(
            _diretorioTemporario,
            quantidadeMaximaDeMensagens ?? 20);
    }
}
