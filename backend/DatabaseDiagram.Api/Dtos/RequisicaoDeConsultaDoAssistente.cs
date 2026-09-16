using System.ComponentModel.DataAnnotations;
using DatabaseDiagram.Api.Modelos;

namespace DatabaseDiagram.Api.Dtos;

/// <summary>
/// Corpo de <c>POST /api/assistente/consultas</c>. Transitória: a
/// <c>chaveDeApi</c> é usada somente na requisição que a originou, em memória,
/// e nunca é retornada, logada ou persistida (constituição III/G3).
/// </summary>
public sealed class RequisicaoDeConsultaDoAssistente
{
    [Required(ErrorMessage = "Informe o provedor de IA.")]
    public string? ProvedorDeIa { get; set; }

    [Required(ErrorMessage = "Informe a chave de API do provedor de IA.")]
    public string? ChaveDeApi { get; set; }

    [Required(ErrorMessage = "Informe uma mensagem para gerar a consulta.")]
    [MinLength(3, ErrorMessage = "A mensagem deve ter ao menos 3 caracteres.")]
    [MaxLength(2000, ErrorMessage = "A mensagem deve ter no máximo 2000 caracteres.")]
    public string? Mensagem { get; set; }

    [Required(ErrorMessage = "O contexto do banco é obrigatório.")]
    public EsquemaDeBanco? ContextoDeBanco { get; set; }
}