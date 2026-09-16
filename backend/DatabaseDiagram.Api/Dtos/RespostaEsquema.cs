using DatabaseDiagram.Api.Modelos;

namespace DatabaseDiagram.Api.Dtos;

/// <summary>
/// Resposta de <c>POST /api/esquema</c>. O contrato de resposta é o próprio
/// <see cref="EsquemaDeBanco"/> (ver <c>data-model.md</c> e <c>contracts/api.md</c>);
/// esta classe é o marcador do contrato e evita duplicar o modelo de domínio.
/// </summary>
public static class RespostaEsquema
{
    public static EsquemaDeBanco Criar(EsquemaDeBanco esquema) => esquema;
}
