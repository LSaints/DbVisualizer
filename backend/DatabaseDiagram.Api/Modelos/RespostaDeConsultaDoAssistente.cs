using System.Text.Json.Serialization;

namespace DatabaseDiagram.Api.Modelos;

/// <summary>
/// Resposta estruturada do assistente de IA. As chaves JSON seguem
/// exatamente o exemplo informado pelo usuário (decisão D1): chaves compostas
/// em snake_case (<c>campos_retorno</c>, <c>tipo_consulta</c>,
/// <c>resultado_esperado</c>, <c>tabela_origem</c> etc.) são mapeadas via
/// <see cref="JsonPropertyNameAttribute"/>. O SQL gerado é dado de exibição —
/// nunca é executado (constituição III).
/// </summary>
public sealed class RespostaDeConsultaDoAssistente
{
    public string Consulta { get; set; } = string.Empty;

    public string Explicacao { get; set; } = string.Empty;

    public string Objetivo { get; set; } = string.Empty;

    public List<TabelaDaResposta> Tabelas { get; set; } = [];

    public List<RelacionamentoDaResposta> Relacionamentos { get; set; } = [];

    public List<FiltroDaResposta> Filtros { get; set; } = [];

    /// <summary>Mapa dinâmico <c>tabela → campos</c> + chave reservada <c>explicacao</c>.</summary>
    [JsonPropertyName("campos_retorno")]
    public CamposDeRetorno CamposRetorno { get; set; } = [];

    [JsonPropertyName("tipo_consulta")]
    public string TipoConsulta { get; set; } = string.Empty;

    [JsonPropertyName("resultado_esperado")]
    public string ResultadoEsperado { get; set; } = string.Empty;

    public List<ParametroDaResposta> Parametros { get; set; } = [];
}

/// <summary>
/// Mapa <c>tabela → campos retornados</c> (ex.: <c>"contratos": "*"</c>) com a
/// chave reservada <c>explicacao</c>. Preserva as chaves como vieram no JSON.
/// </summary>
public sealed class CamposDeRetorno : Dictionary<string, string>
{
}

public sealed class TabelaDaResposta
{
    public string Nome { get; set; } = string.Empty;

    public string Apelido { get; set; } = string.Empty;

    public string Funcao { get; set; } = string.Empty;
}

public sealed class RelacionamentoDaResposta
{
    [JsonPropertyName("tabela_origem")]
    public string TabelaOrigem { get; set; } = string.Empty;

    [JsonPropertyName("campo_origem")]
    public string CampoOrigem { get; set; } = string.Empty;

    [JsonPropertyName("tabela_destino")]
    public string TabelaDestino { get; set; } = string.Empty;

    [JsonPropertyName("campo_destino")]
    public string CampoDestino { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Explicacao { get; set; } = string.Empty;
}

public sealed class FiltroDaResposta
{
    public string Campo { get; set; } = string.Empty;

    public string Operador { get; set; } = string.Empty;

    public string Valor { get; set; } = string.Empty;

    public string Explicacao { get; set; } = string.Empty;
}

public sealed class ParametroDaResposta
{
    public string Nome { get; set; } = string.Empty;

    public string Valor { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;
}