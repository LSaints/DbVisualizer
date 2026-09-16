using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DatabaseDiagram.Api.Controladores;

/// <summary>
/// Extrai a primeira mensagem de validação do <see cref="ModelStateDictionary"/>
/// (mensagens em pt-BR definidas nas anotações dos DTOs).
/// </summary>
internal static class MensagensDeValidacao
{
    public static string PrimeiraMensagem(ModelStateDictionary modelState) =>
        modelState.Values
            .SelectMany(estado => estado.Errors)
            .Select(erro => erro.ErrorMessage)
            .FirstOrDefault(mensagem => !string.IsNullOrWhiteSpace(mensagem))
        ?? "Dados de conexão inválidos.";
}
