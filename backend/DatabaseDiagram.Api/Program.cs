using System.Text.Json;
using DatabaseDiagram.Api.Controladores;
using DatabaseDiagram.Api.Dtos;
using DatabaseDiagram.Api.Infraestrutura;
using DatabaseDiagram.Api.Provedores;
using DatabaseDiagram.Api.Provedores.Ia.GoogleAiStudio;
using DatabaseDiagram.Api.Provedores.Ia.OpenAi;
using DatabaseDiagram.Api.Provedores.MySql;
using DatabaseDiagram.Api.Servicos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opcoes =>
        opcoes.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
    .ConfigureApiBehaviorOptions(opcoes =>
    {
        // O filtro automático de ModelState é desabilitado para que cada
        // controlador valide explicitamente e siga o contrato em
        // contracts/api.md (mensagem em pt-BR, sem expor segredos).
        opcoes.SuppressModelStateInvalidFilter = true;
    });

// CORS de desenvolvimento: o Vite (5173) consome a API (5000).
builder.Services.AddCors(opcoes =>
    opcoes.AddPolicy("Desenvolvimento", politica =>
        politica
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()));

// Conexão efêmera (memória): testes de conexão e introspecção por requisição.
builder.Services.AddSingleton<ConfiguradorDeConexao>();
builder.Services.AddSingleton<ServicoDeConexao>();

// Proteção contra sobrecarga: limita introspecções por cliente (janela de
// tempo) e por concorrência (no ServicoDeSchema), evitando derrubar o banco
// alvo com muitas requisições simultâneas.
builder.Services.AddRateLimiter(opcoes =>
{
    opcoes.AddFixedWindowLimiter("IntrospeccaoPorCliente", config =>
    {
        config.PermitLimit = 20;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueLimit = 0;
    });

    // Protege o provedor de IA e o servidor de abuso/erros de loop na
    // geração de consultas (decisão D10).
    opcoes.AddFixedWindowLimiter("AssistentePorCliente", config =>
    {
        config.PermitLimit = 10;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueLimit = 0;
    });

    opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opcoes.OnRejected = async (contexto, cancellationToken) =>
    {
        contexto.HttpContext.Response.ContentType = "application/json";
        await contexto.HttpContext.Response.WriteAsJsonAsync(
            new { mensagem = MensagensDeValidacao.MuitasRequisicoes },
            cancellationToken);
    };
});

// Providers isolados com contrato comum: adicionar um banco não exige
// alterar controllers, serviços, modelo comum ou frontend.
builder.Services.AddSingleton<InterfaceProvedorDeSchema, MySqlProvedorDeSchema>();
builder.Services.AddSingleton<FabricaDeProvedoresDeSchema>();
builder.Services.AddSingleton<ServicoDeSchema>();

// Assistente de IA: provedores isolados com contrato comum (constituição IV).
// O OpenAI usa um HttpClient dedicado com timeout de 90 s (decisão D10); a
// chave de API é efêmera, por requisição, e nunca é persistida ou logada (D9).
builder.Services.AddHttpClient(OpenAiProvedorDeIa.NomeDoCliente, cliente =>
    cliente.Timeout = TimeSpan.FromSeconds(90));
builder.Services.AddSingleton<InterfaceProvedorDeIa>(provedor =>
    new OpenAiProvedorDeIa(
        provedor.GetRequiredService<IHttpClientFactory>()
            .CreateClient(OpenAiProvedorDeIa.NomeDoCliente)));

// Google AI Studio (Gemini generateContent): o mesmo contrato e o mesmo fluxo;
// a chave via cabeçalho `x-goog-api-key`, nunca na URL query nem em logs.
builder.Services.AddHttpClient(GoogleAiStudioProvedorDeIa.NomeDoCliente, cliente =>
    cliente.Timeout = TimeSpan.FromSeconds(90));
builder.Services.AddSingleton<InterfaceProvedorDeIa>(provedor =>
    new GoogleAiStudioProvedorDeIa(
        provedor.GetRequiredService<IHttpClientFactory>()
            .CreateClient(GoogleAiStudioProvedorDeIa.NomeDoCliente)));

builder.Services.AddSingleton<FabricaDeProvedoresDeIa>();
builder.Services.AddSingleton<ConstrutorDeContextoDeBanco>();
builder.Services.AddSingleton<ServicoDoAssistente>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("Desenvolvimento");
app.UseRateLimiter();
app.MapControllers();

app.Run();

/// <summary>Ponto de entrada, exposto para testes de integração.</summary>
public partial class Program
{
}
