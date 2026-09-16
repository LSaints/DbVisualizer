using System.ComponentModel.DataAnnotations;
using DatabaseDiagram.Api.Modelos;

namespace DatabaseDiagram.Api.Dtos;

/// <summary>
/// Corpo das requisições <c>POST /api/conexoes/teste</c> e
/// <c>POST /api/esquema</c>. Chaves JSON em pt-BR (camelCase).
/// </summary>
public sealed class RequisicaoConexao
{
    [Required(ErrorMessage = "O provedor é obrigatório.")]
    public string? Provedor { get; set; }

    [Required(ErrorMessage = "O host é obrigatório.")]
    public string? Host { get; set; }

    /// <summary>
    /// Porta. Quando ausente, o padrão do provider é usado (3306 para MySQL).
    /// </summary>
    [Range(1, 65535, ErrorMessage = "A porta deve ser maior que zero.")]
    public int? Porta { get; set; }

    [Required(ErrorMessage = "O banco de dados é obrigatório.")]
    public string? BancoDeDados { get; set; }

    [Required(ErrorMessage = "O usuário é obrigatório.")]
    public string? Usuario { get; set; }

    [Required(ErrorMessage = "A senha é obrigatória.")]
    public string? Senha { get; set; }

    /// <summary>
    /// Converte para o modelo de domínio <see cref="ConexaoDeBanco"/>.
    /// Usa a porta padrão do provider (3306) quando ela não foi informada.
    /// </summary>
    public ConexaoDeBanco ParaConexaoDeBanco() => new()
    {
        Provedor = Provedor!,
        Host = Host!,
        Porta = Porta ?? 3306,
        BancoDeDados = BancoDeDados!,
        Usuario = Usuario!,
        Senha = Senha!
    };
}
