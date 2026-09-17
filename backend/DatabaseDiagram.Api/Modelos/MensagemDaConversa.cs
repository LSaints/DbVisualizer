using System.Text.Json;
using System.Text.Json.Serialization;

namespace DatabaseDiagram.Api.Modelos;

/// <summary>
/// Uma troca dentro da conversa (pedido ou resposta estruturada). O campo
/// <c>conteudo</c> é polymórfico: para <c>papel = "usuario"</c> é uma string
/// (texto do pedido); para <c>papel = "assistente"</c> é o contrato
/// estruturado <see cref="RespostaDeConsultaDoAssistente"/> (D3/data-model.md).
/// </summary>
public sealed class MensagemDaConversa
{
    /// <summary>
    /// Papel do remetente: <c>"usuario"</c> ou <c>"assistente"</c>.
    /// </summary>
    public string Papel { get; set; } = string.Empty;

    /// <summary>
    /// Conteúdo da mensagem: string para <c>"usuario"</c>, objeto
    /// <see cref="RespostaDeConsultaDoAssistente"/> para <c>"assistente"</c>.
    /// Serializado como <see cref="JsonElement"/> para preservar o formato
    /// original do contrato estruturado.
    /// </summary>
    [JsonConverter(typeof(ConteudoDaMensagemConverter))]
    public object Conteudo { get; set; } = string.Empty;

    public string CriadaEm { get; set; } = string.Empty;
}

/// <summary>
/// Serializa/deserializa o campo <c>conteudo</c> como string ou como o
/// contrato estruturado do assistente, preservando a fidelidade do JSON
/// gravado no arquivo da conversa.
/// </summary>
internal sealed class ConteudoDaMensagemConverter : JsonConverter<object>
{
    public override object? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() ?? string.Empty;
        }

        using var documento = JsonDocument.ParseValue(ref reader);

        try
        {
            var resposta = documento.RootElement.Deserialize<RespostaDeConsultaDoAssistente>(options);
            if (resposta is not null)
            {
                return resposta;
            }
        }
        catch (JsonException)
        {
            // Fallback: retorna o JsonElement puro.
        }

        return documento.RootElement.Clone();
    }

    public override void Write(
        Utf8JsonWriter writer,
        object value,
        JsonSerializerOptions options)
    {
        switch (value)
        {
            case string texto:
                writer.WriteStringValue(texto);
                break;
            case RespostaDeConsultaDoAssistente resposta:
                JsonSerializer.Serialize(writer, resposta, options);
                break;
            case JsonElement elemento:
                elemento.WriteTo(writer);
                break;
            default:
                JsonSerializer.Serialize(writer, value, value.GetType(), options);
                break;
        }
    }
}
