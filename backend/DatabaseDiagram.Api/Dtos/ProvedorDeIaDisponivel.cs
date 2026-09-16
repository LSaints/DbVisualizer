namespace DatabaseDiagram.Api.Dtos;

/// <summary>
/// Item de <c>GET /api/assistente/provedores</c>: somente os provedores
/// implementados na fábrica, ex. <c>{ "provedor": "openai", "rotulo": "OpenAI" }</c>.
/// </summary>
public sealed record ProvedorDeIaDisponivel(string Provedor, string Rotulo);