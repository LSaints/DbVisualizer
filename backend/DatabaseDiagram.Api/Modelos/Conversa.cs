using DatabaseDiagram.Api.Modelos;

namespace DatabaseDiagram.Api.Modelos;

/// <summary>
/// Modelo persistido: uma conversa por arquivo <c>{id}.json</c>. Campos
/// camelCase no arquivo (formato interno), divergindo do contrato da API
/// (data-model.md). Serializado com <see cref="System.Text.Json"/>.
/// </summary>
public sealed class Conversa
{
    /// <summary>Versão do formato; hoje <c>1</c> (FR-016).</summary>
    public int Versao { get; set; } = 1;

    /// <summary>Guid (file name). Obrigatório.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Título da conversa (listagem); padrão inicial "Nova conversa";
    /// renomeável via PATCH (US4).
    /// </summary>
    public string Titulo { get; set; } = "Nova conversa";

    /// <summary>
    /// <c>true</c> até a primeira mensagem; título derivado da 1ª mensagem;
    /// PATCH renomear marca <c>false</c> (D6).
    /// </summary>
    public bool TituloAutomatico { get; set; } = true;

    public string CriadaEm { get; set; } = string.Empty;

    public string AtualizadaEm { get; set; } = string.Empty;

    /// <summary>Identidade do banco usada na criação da conversa (FR-013, D7).</summary>
    public IdentidadeDeBanco? ContextoDeBanco { get; set; }

    /// <summary>
    /// Identificador do provedor de IA da conversa (<c>openai</c>,
    /// <c>google-ai-studio</c>, <c>claude</c>…); setado na 1ª troca persistida
    /// que usar o provedor; nulo em conversas antigas → frontend usa o padrão
    /// da sessão.
    /// </summary>
    public string? ProvedorDeIaSelecionado { get; set; }

    /// <summary>
    /// Ordem cronológica das trocas; vazio logo após a criação.
    /// </summary>
    public List<MensagemDaConversa> Mensagens { get; set; } = [];
}
