namespace DatabaseDiagram.Api.Dtos;

/// <summary>
/// Wrapper do corpo de <c>200</c> de <c>POST /api/assistente/consultas</c>.
/// O contrato de resposta é o próprio
/// <see cref="DatabaseDiagram.Api.Modelos.RespostaDeConsultaDoAssistente"/>
/// (ver <c>contracts/api.md</c>); esta classe é o marcador do contrato e evita
/// duplicar o modelo de domínio.
/// </summary>
public static class RespostaDeConsultaDoAssistente
{
    public static Modelos.RespostaDeConsultaDoAssistente Criar(
        Modelos.RespostaDeConsultaDoAssistente resposta) => resposta;
}