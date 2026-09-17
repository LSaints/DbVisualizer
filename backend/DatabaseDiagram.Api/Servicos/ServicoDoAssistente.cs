using System.Text.Json;
using DatabaseDiagram.Api.Dtos;
using DatabaseDiagram.Api.Provedores;
using RespostaDeConsultaDoAssistente = DatabaseDiagram.Api.Modelos.RespostaDeConsultaDoAssistente;

namespace DatabaseDiagram.Api.Servicos;

/// <summary>
/// Orquestra o fluxo do assistente: resolve o provedor na fábrica, monta o
/// contexto com o <see cref="ConstrutorDeContextoDeBanco"/>, chama o provedor
/// com a chave efêmera e valida a resposta contra o contrato estruturado com
/// <see cref="System.Text.Json"/> (decisão D6). O SQL gerado é dado de
/// exibição — nunca é executado (constituição III).
/// </summary>
public sealed class ServicoDoAssistente
{
    private static readonly JsonSerializerOptions OpcoesDeLeitura = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Comandos que implicam escrita (DML/DDL) — a geração é somente leitura
    /// (SELECT), conforme FR-017: uma resposta assim é rejeitada (422).
    /// </summary>
    private static readonly HashSet<string> TiposQueImplicamEscrita = new(StringComparer.OrdinalIgnoreCase)
    {
        "INSERT", "UPDATE", "DELETE", "REPLACE", "MERGE", "UPSERT",
        "CREATE", "ALTER", "DROP", "TRUNCATE", "RENAME", "GRANT", "REVOKE", "CALL"
    };

    private readonly FabricaDeProvedoresDeIa _fabrica;
    private readonly ConstrutorDeContextoDeBanco _construtor;

    public ServicoDoAssistente(
        FabricaDeProvedoresDeIa fabrica,
        ConstrutorDeContextoDeBanco construtor)
    {
        _fabrica = fabrica;
        _construtor = construtor;
    }

    /// <summary>
    /// Retorna a resposta estruturada validada. Provedor desconhecido lança
    /// <see cref="ProvedorDeIaNaoSuportadoException"/>; resposta fora do
    /// contrato lança <see cref="RespostaDoAssistenteInvalidaException"/>;
    /// falha/indisponibilidade do provedor lança
    /// <see cref="FalhaNoProvedorDeIaException"/>.
    /// </summary>
    public async Task<RespostaDeConsultaDoAssistente> ObterRespostaAsync(
        RequisicaoDeConsultaDoAssistente requisicao,
        CancellationToken cancellationToken) =>
        await ObterRespostaAsync(requisicao, historicoDeMensagens: null, cancellationToken);

    /// <summary>
    /// Sobrecarga usada quando a consulta pertence a uma conversa (US1):
    /// <paramref name="historicoDeMensagens"/> é a janela de contexto (sem a
    /// mensagem atual), repassada ao provedor com os papéis nativos (D3/FR-002).
    /// </summary>
    public async Task<RespostaDeConsultaDoAssistente> ObterRespostaAsync(
        RequisicaoDeConsultaDoAssistente requisicao,
        IReadOnlyList<Modelos.MensagemDaConversa>? historicoDeMensagens,
        CancellationToken cancellationToken)
    {
        // Defesa em profundidade: o controlador valida a entrada (400), mas o
        // serviço não chama o provedor sem contexto de tabelas (G3/FR-006).
        if (requisicao.ContextoDeBanco is not { Tabelas.Count: > 0 })
        {
            throw new ArgumentException(
                "O contexto do banco deve conter tabelas para gerar a consulta.",
                nameof(requisicao));
        }

        var provedor = _fabrica.Obter(requisicao.ProvedorDeIa!);
        var contexto = _construtor.Construir(requisicao.ContextoDeBanco!);
        var pedido = new PedidoDeResposta(
            contexto,
            requisicao.Mensagem!.Trim(),
            historicoDeMensagens);

        string texto;
        try
        {
            texto = await provedor.ObterRespostaAsync(
                pedido,
                requisicao.ChaveDeApi!,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        // Erros já tipados do provedor (ex.: com o status HTTP) são repassados
        // como estão; erros inesperados viram a falha genérica (FR-015/G6).
        catch (FalhaNoProvedorDeIaException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new FalhaNoProvedorDeIaException();
        }

        return Interpretar(texto);
    }

    /// <summary>
    /// Valida o contrato estruturado (FR-008/FR-015): exige JSON válido,
    /// <c>consulta</c> (string não vazia) e <c>explicacao</c> presentes; os
    /// demais grupos presentes com o tipo correto (listas podem ser vazias;
    /// <c>objetivo</c>/<c>tipo_consulta</c>/<c>resultado_esperado</c> strings
    /// mesmo vazias; <c>campos_retorno</c> objeto).
    /// </summary>
    private static RespostaDeConsultaDoAssistente Interpretar(string texto)
    {
        var textoLimpo = LimparFencesMarkdown(texto);

        using var documento = LerDocumento(textoLimpo);
        var raiz = documento.RootElement;

        if (raiz.ValueKind != JsonValueKind.Object ||
            !EhStringNaoVazia(raiz, "consulta") ||
            !EhPropriedadeString(raiz, "explicacao") ||
            !EhPropriedadeString(raiz, "objetivo") ||
            !EhPropriedadeString(raiz, "tipo_consulta") ||
            !EhPropriedadeString(raiz, "resultado_esperado") ||
            !EhArray(raiz, "tabelas") ||
            !EhArray(raiz, "relacionamentos") ||
            !EhArray(raiz, "filtros") ||
            !EhArray(raiz, "parametros") ||
            !EhObject(raiz, "campos_retorno"))
        {
            throw new RespostaDoAssistenteInvalidaException();
        }

        try
        {
            var resposta = JsonSerializer.Deserialize<RespostaDeConsultaDoAssistente>(
                    textoLimpo, OpcoesDeLeitura)
                ?? throw new RespostaDoAssistenteInvalidaException();

            if (ImplicaEscrita(resposta.TipoConsulta))
            {
                // A geração permanece somente leitura (SELECT); uma resposta de
                // escrita é rejeitada para nunca ser exibida como válida (FR-017).
                throw new RespostaDoAssistenteInvalidaException();
            }

            return resposta;
        }
        catch (JsonException)
        {
            throw new RespostaDoAssistenteInvalidaException();
        }
    }

    /// <summary>
    /// Verdadeiro quando o primeiro comando do <c>tipo_consulta</c> implica
    /// escrita (DML/DDL). Comandos vazios ou de leitura (SELECT/SHOW/DESCRIBE)
    /// são permitidos.
    /// </summary>
    private static bool ImplicaEscrita(string tipoConsulta)
    {
        var primeiroComando = tipoConsulta.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return primeiroComando is not null &&
               TiposQueImplicamEscrita.Contains(primeiroComando);
    }

    private static JsonDocument LerDocumento(string texto)
    {
        try
        {
            return JsonDocument.Parse(texto);
        }
        catch (JsonException)
        {
            throw new RespostaDoAssistenteInvalidaException();
        }
    }

    /// <summary>
    /// Remove cercas de código markdown (```json ... ```) e texto antes/depois
    /// do primeiro e último '{'/'}' para recuperar respostas cujo JSON seja
    /// embrulhado pelo provedor (gemini/chat LLMs frequentemente embrulham).
    /// </summary>
    private static string LimparFencesMarkdown(string texto)
    {
        var semFences = texto.Trim();

        var inicioFence = semFences.IndexOf("```");
        if (inicioFence >= 0)
        {
            var fimFence = semFences.LastIndexOf("```");
            if (fimFence > inicioFence)
            {
                semFences = semFences[(inicioFence + 3)..fimFence];
            }
            else
            {
                semFences = semFences[(inicioFence + 3)..];
            }
        }

        var primeiroDeObjeto = semFences.IndexOf('{');
        var ultimoDeObjeto = semFences.LastIndexOf('}');
        if (primeiroDeObjeto >= 0 && ultimoDeObjeto > primeiroDeObjeto)
        {
            semFences = semFences[primeiroDeObjeto..(ultimoDeObjeto + 1)];
        }

        return semFences.Trim();
    }

    private static bool EhStringNaoVazia(JsonElement objeto, string chave) =>
        objeto.TryGetProperty(chave, out var valor) &&
        valor.ValueKind == JsonValueKind.String &&
        !string.IsNullOrWhiteSpace(valor.GetString());

    private static bool EhPropriedadeString(JsonElement objeto, string chave) =>
        objeto.TryGetProperty(chave, out var valor) &&
        valor.ValueKind == JsonValueKind.String;

    private static bool EhArray(JsonElement objeto, string chave) =>
        objeto.TryGetProperty(chave, out var valor) &&
        valor.ValueKind == JsonValueKind.Array;

    private static bool EhObject(JsonElement objeto, string chave) =>
        objeto.TryGetProperty(chave, out var valor) &&
        valor.ValueKind == JsonValueKind.Object;
}