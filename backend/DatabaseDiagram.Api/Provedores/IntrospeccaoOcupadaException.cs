namespace DatabaseDiagram.Api.Provedores;

/// <summary>
/// Lançada quando o limite de introspecções simultâneas do
/// <see cref="Servicos.ServicoDeSchema"/> é excedido, protegendo o banco alvo
/// contra sobrecarga de requisições concorrentes.
/// </summary>
public sealed class IntrospeccaoOcupadaException(string mensagem) : Exception(mensagem);