namespace DatabaseDiagram.Api.Provedores;

/// <summary>
/// Pedido enviado ao provedor de IA com as mensagens já compostas: a mensagem
/// de sistema (contexto do banco) e a mensagem do usuário. O provedor apenas
/// traduz para o formato da própria API (constituição IV).
/// </summary>
public sealed record PedidoDeResposta(string MensagemDeSistema, string MensagemDoUsuario);