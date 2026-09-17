using DatabaseDiagram.Api.Modelos;

namespace DatabaseDiagram.Api.Provedores;

/// <summary>
/// Pedido enviado ao provedor de IA com as mensagens já compostas: a mensagem
/// de sistema (contexto do banco), o histórico da janela de contexto (D3/D4,
/// quando a consulta pertence a uma conversa) e a mensagem atual do usuário. O
/// provedor apenas traduz para o formato da própria API (constituição IV).
/// </summary>
public sealed record PedidoDeResposta(
    string MensagemDeSistema,
    string MensagemDoUsuario,
    IReadOnlyList<MensagemDaConversa>? MensagensDaConversa = null);