using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DatabaseDiagram.Api.Controladores;

/// <summary>
/// Mensagens de erro e validação do assistente em pt-BR, orientadas à ação e
/// sem expor a chave de API, senha ou detalhes internos (FR-014/G6). As chaves
/// de status seguem o contrato em <c>contracts/api.md</c>.
/// </summary>
internal static class MensagensDeValidacao
{
    /// <summary>422 — resposta do provedor fora do formato estruturado ou com escrita.</summary>
    public const string RespostaDoAssistenteInvalida =
        "A resposta do assistente não pôde ser interpretada. Tente novamente.";

    /// <summary>429 — limite de requisições por cliente excedido.</summary>
    public const string MuitasRequisicoes =
        "Muitas requisições. Aguarde um instante e tente novamente.";

    /// <summary>502 — falha/indisponibilidade do provedor (sem expor a chave).</summary>
    public const string FalhaAoGerarConsulta =
        "Não foi possível gerar a consulta pelo provedor selecionado.";

    /// <summary>400 — provedor não implementado na fábrica.</summary>
    public const string ProvedorDeIaNaoSuportado =
        "Provedor de IA não suportado.";

    /// <summary>404 — conversaId informado não corresponde a nenhuma conversa persistida.</summary>
    public const string ConversaNaoEncontrada =
        "Conversa não encontrada.";

    /// <summary>409 — o banco conectado na requisição difere do registrado na conversa (FR-013).</summary>
    public const string BancoDivergenteDaConversa =
        "O banco conectado difere do registrado nesta conversa.";

    public static string PrimeiraMensagem(ModelStateDictionary modelState) =>
        modelState.Values
            .SelectMany(estado => estado.Errors)
            .Select(erro => erro.ErrorMessage)
            .FirstOrDefault(mensagem => !string.IsNullOrWhiteSpace(mensagem))
        ?? "Dados de conexão inválidos.";
}
