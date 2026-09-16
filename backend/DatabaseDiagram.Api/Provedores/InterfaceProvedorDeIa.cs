namespace DatabaseDiagram.Api.Provedores;

/// <summary>
/// Contrato mínimo do provedor de IA. Cada provedor é isolado na sua pasta e
/// traduz o pedido comum para o formato da própria API; adicionar um provedor
/// não altera controllers, serviços comuns ou frontend (constituição IV).
/// </summary>
public interface InterfaceProvedorDeIa
{
    /// <summary>Identificador do provedor, ex.: <c>openai</c>.</summary>
    string Tipo { get; }

    /// <summary>Rótulo exibido ao usuário em pt-BR, ex.: <c>OpenAI</c>.</summary>
    string Rotulo { get; }

    /// <summary>
    /// Envia o pedido ao provedor e retorna o texto bruto da resposta.
    /// A chave de API é usada somente nesta requisição, em memória
    /// (constituição III).
    /// </summary>
    Task<string> ObterRespostaAsync(
        PedidoDeResposta pedido,
        string chaveDeApi,
        CancellationToken cancellationToken);
}