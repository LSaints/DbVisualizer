using System.Text;
using System.Text.Json;
using DatabaseDiagram.Api.Dtos;
using DatabaseDiagram.Api.Modelos;

namespace DatabaseDiagram.Api.Servicos;

/// <summary>
/// Único serviço de persistência de conversas (G1). Persiste cada conversa em
/// um arquivo <c>{id}.json</c> em diretório configurável (fora de
/// wwwroot/pasta servida), criado lazy na primeira escrita. Oferece CRUD
/// completo, persistência de trocas, janela de contexto, título automático,
/// sanitização de segredos e escrita atômica (D4/D6/D8/D9).
/// </summary>
public sealed class ServicoDeConversas
{
    private static readonly JsonSerializerOptions OpcoesDeSerializacao = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions OpcoesDeLeitura = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Padrões típicos de segredo substituídos por <c>"&lt;segredo removido&gt;"</c>
    /// na persistência e no contexto (D8/FR-012/SC-005).
    /// </summary>
    private static readonly (string Padrao, string Regex)[] PadroesDeSegredo =
    [
        ("sk-", @"sk-[A-Za-z0-9\-_]{10,}"),
        ("Bearer ", @"Bearer\s+[A-Za-z0-9\-_\.]{10,}"),
        ("key=", @"key\s*=\s*[A-Za-z0-9\-_]{10,}")
    ];

    private readonly string _diretorio;
    private readonly int _quantidadeMaximaDeMensagens;
    private readonly object _lockDeCriacao = new();
    private bool _diretorioVerificado;

    public ServicoDeConversas(string diretorio, int quantidadeMaximaDeMensagens = 20)
    {
        _diretorio = diretorio;
        _quantidadeMaximaDeMensagens = quantidadeMaximaDeMensagens;
    }

    /// <summary>
    /// Cria uma nova conversa vazia com título padrão "Nova conversa" e
    /// <c>tituloAutomatico = true</c>. Retorna o resumo para o <c>201</c>.
    /// </summary>
    public async Task<ConversaResumo> CriarConversaAsync(string? titulo = null)
    {
        var agora = DateTime.UtcNow.ToString("o");
        var conversa = new Conversa
        {
            Id = Guid.NewGuid().ToString(),
            Titulo = string.IsNullOrWhiteSpace(titulo) ? "Nova conversa" : titulo.Trim(),
            TituloAutomatico = string.IsNullOrWhiteSpace(titulo),
            CriadaEm = agora,
            AtualizadaEm = agora
        };

        await SalvarAsync(conversa);

        return ParaResumo(conversa);
    }

    /// <summary>
    /// Lista as conversas ordenadas por <c>atualizadaEm</c> decrescente.
    /// </summary>
    public async Task<List<ConversaResumo>> ListarConversasAsync()
    {
        GarantirDiretorio();

        if (!Directory.Exists(_diretorio))
        {
            return [];
        }

        var arquivos = Directory.GetFiles(_diretorio, "*.json");
        var conversas = new List<ConversaResumo>(arquivos.Length);

        foreach (var arquivo in arquivos)
        {
            try
            {
                var texto = await File.ReadAllTextAsync(arquivo);
                var conversa = JsonSerializer.Deserialize<Conversa>(texto, OpcoesDeLeitura);
                if (conversa is not null)
                {
                    conversas.Add(ParaResumo(conversa));
                }
            }
            catch (JsonException)
            {
                // Arquivo corrompido ou formato incompatível: ignorar silenciosamente.
            }
        }

        return conversas
            .OrderByDescending(c => c.AtualizadaEm)
            .ToList();
    }

    /// <summary>
    /// Obtém uma conversa pelo id. Lança <c>null</c> se não encontrada (404).
    /// </summary>
    public async Task<ConversaDetalhada?> ObterConversaAsync(string id)
    {
        var caminho = CaminhoDoArquivo(id);

        if (!File.Exists(caminho))
        {
            return null;
        }

        try
        {
            var texto = await File.ReadAllTextAsync(caminho);
            var conversa = JsonSerializer.Deserialize<Conversa>(texto, OpcoesDeLeitura);

            if (conversa is null)
            {
                return null;
            }

            return ParaDetalhada(conversa);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Obtém a conversa interna (domínio) pelo id. Usado pelo controlador
    /// para validação de identidade de banco e persistência.
    /// </summary>
    internal async Task<Conversa?> ObterConversaInternaAsync(string id)
    {
        var caminho = CaminhoDoArquivo(id);

        if (!File.Exists(caminho))
        {
            return null;
        }

        try
        {
            var texto = await File.ReadAllTextAsync(caminho);
            return JsonSerializer.Deserialize<Conversa>(texto, OpcoesDeLeitura);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Renomeia uma conversa e marca <c>tituloAutomatico = false</c> (D6).
    /// Retorna o novo título ou null se não encontrada.
    /// </summary>
    public async Task<string?> RenomearConversaAsync(string id, string titulo)
    {
        var conversa = await ObterConversaInternaAsync(id);
        if (conversa is null)
        {
            return null;
        }

        conversa.Titulo = titulo.Trim();
        conversa.TituloAutomatico = false;
        await SalvarAsync(conversa);

        return conversa.Titulo;
    }

    /// <summary>
    /// Exclui a conversa permanentemente. Retorna true se excluída, false
    /// se não encontrada (SC-006).
    /// </summary>
    public async Task<bool> ExcluirConversaAsync(string id)
    {
        var caminho = CaminhoDoArquivo(id);

        if (!File.Exists(caminho))
        {
            return false;
        }

        await Task.Run(() => File.Delete(caminho));
        return true;
    }

    /// <summary>
    /// Persiste o par usuário+assistente em uma conversa existente.
    /// Aplica sanitização (D8), título automático (D6) e escrita atômica (D9).
    /// Somente chamado em sucesso da geração (D5). O provedor de IA é gravado
    /// apenas quando não-vazio, na 1ª troca que o usar (FR-012).
    /// </summary>
    public async Task PersistirTrocaAsync(
        string conversaId,
        IdentidadeDeBanco contextoDeBanco,
        string mensagemDoUsuario,
        Modelos.RespostaDeConsultaDoAssistente respostaDoAssistente,
        string? provedorDeIaSelecionado = null)
    {
        var conversa = await ObterConversaInternaAsync(conversaId);
        if (conversa is null)
        {
            throw new InvalidOperationException($"Conversa '{conversaId}' não encontrada.");
        }

        if (!string.IsNullOrWhiteSpace(provedorDeIaSelecionado))
        {
            conversa.ProvedorDeIaSelecionado = provedorDeIaSelecionado;
        }

        var agora = DateTime.UtcNow.ToString("o");

        var mensagemUsuario = new MensagemDaConversa
        {
            Papel = "usuario",
            Conteudo = Sanitizar(mensagemDoUsuario),
            CriadaEm = agora
        };

        var mensagemAssistente = new MensagemDaConversa
        {
            Papel = "assistente",
            Conteudo = respostaDoAssistente,
            CriadaEm = agora
        };

        conversa.Mensagens.Add(mensagemUsuario);
        conversa.Mensagens.Add(mensagemAssistente);

        // Título automático: derivado da 1ª mensagem (D6).
        if (conversa.TituloAutomatico)
        {
            var trecho = mensagemDoUsuario.Trim();
            conversa.Titulo = trecho.Length > 40
                ? string.Concat(trecho.AsSpan(0, 40), "…")
                : trecho;
            conversa.TituloAutomatico = false;
        }

        // Atualiza identidade de banco (pode ter sido a primeira troca).
        conversa.ContextoDeBanco = contextoDeBanco;
        conversa.AtualizadaEm = agora;

        await SalvarAsync(conversa);
    }

    /// <summary>
    /// Obtém a janela de contexto: as últimas N mensagens da conversa com a
    /// mensagem atual do usuário sempre incluída ao final (D4/FR-010). A flag
    /// de truncamento é verdadeira quando o histórico persistido excede a
    /// janela (FR-011).
    /// </summary>
    public JanelaDeContexto ObterJanelaDeContexto(
        Conversa conversa,
        string mensagemAtualDoUsuario)
    {
        var historico = conversa.Mensagens;
        var truncada = historico.Count > _quantidadeMaximaDeMensagens;

        var inicio = truncada ? historico.Count - _quantidadeMaximaDeMensagens : 0;
        var janela = historico.GetRange(inicio, historico.Count - inicio).ToList();

        janela.Add(new MensagemDaConversa
        {
            Papel = "usuario",
            Conteudo = Sanitizar(mensagemAtualDoUsuario),
            CriadaEm = DateTime.UtcNow.ToString("o")
        });

        return new JanelaDeContexto(janela, truncada);
    }

    private async Task SalvarAsync(Conversa conversa)
    {
        GarantirDiretorio();

        var caminho = CaminhoDoArquivo(conversa.Id);
        var caminhoTemp = caminho + ".tmp";

        var texto = JsonSerializer.Serialize(conversa, OpcoesDeSerializacao);

        // Escrita atômica: temp + rename (D9).
        await File.WriteAllTextAsync(caminhoTemp, texto);
        File.Move(caminhoTemp, caminho, overwrite: true);
    }

    private void GarantirDiretorio()
    {
        if (_diretorioVerificado)
        {
            return;
        }

        lock (_lockDeCriacao)
        {
            if (_diretorioVerificado)
            {
                return;
            }

            Directory.CreateDirectory(_diretorio);
            _diretorioVerificado = true;
        }
    }

    private string CaminhoDoArquivo(string id)
    {
        return Path.Combine(_diretorio, $"{id}.json");
    }

    /// <summary>
    /// Sanitização leve (D8): substitui padrões típicos de segredo por
    /// <c>"&lt;segredo removido&gt;"</c>. Usado tanto ao persistir quanto ao
    /// montar o contexto enviado ao provedor (FR-012/SC-005).
    /// </summary>
    public static string SanitizarTexto(string texto) => Sanitizar(texto);

    private static string Sanitizar(string texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return texto;
        }

        var resultado = texto;

        foreach (var (_, regex) in PadroesDeSegredo)
        {
            resultado = System.Text.RegularExpressions.Regex.Replace(
                resultado,
                regex,
                "<segredo removido>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return resultado;
    }

    private static ConversaResumo ParaResumo(Conversa conversa)
    {
        var resumo = "Conversa vazia";

        if (conversa.Mensagens.Count > 0)
        {
            var ultima = conversa.Mensagens[^1];
            var texto = ultima.Conteudo switch
            {
                string s => s,
                _ => "Conversa vazia"
            };

            resumo = texto.Length > 80
                ? string.Concat(texto.AsSpan(0, 80), "…")
                : texto;
        }

        return new ConversaResumo
        {
            Id = conversa.Id,
            Titulo = conversa.Titulo,
            CriadaEm = conversa.CriadaEm,
            AtualizadaEm = conversa.AtualizadaEm,
            Resumo = resumo
        };
    }

    private static ConversaDetalhada ParaDetalhada(Conversa conversa)
    {
        return new ConversaDetalhada
        {
            Id = conversa.Id,
            Titulo = conversa.Titulo,
            CriadaEm = conversa.CriadaEm,
            AtualizadaEm = conversa.AtualizadaEm,
            ContextoDeBanco = conversa.ContextoDeBanco,
            ProvedorDeIa = conversa.ProvedorDeIaSelecionado,
            Mensagens = conversa.Mensagens
        };
    }
}

/// <summary>
/// Resultado da janela de contexto: lista de mensagens selecionadas e flag
/// de truncamento para o header <c>X-Contexto-Truncado</c> (D4/FR-011).
/// </summary>
public sealed class JanelaDeContexto(
    IReadOnlyList<MensagemDaConversa> mensagens,
    bool truncada)
{
    public IReadOnlyList<MensagemDaConversa> Mensagens { get; } = mensagens;

    public bool Truncada { get; } = truncada;
}
