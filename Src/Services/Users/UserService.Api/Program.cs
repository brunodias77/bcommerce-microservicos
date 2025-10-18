// ============================================================================
// B-Commerce User Management API - Program.cs
// ============================================================================
// Este arquivo é o ponto de entrada principal da aplicação ASP.NET Core.
// Utiliza o padrão Minimal API do .NET 6+ que simplifica a configuração
// e inicialização da aplicação, eliminando a necessidade das classes
// Startup.cs e Program.cs separadas do .NET Framework.
// ============================================================================

using Microsoft.AspNetCore.Diagnostics;
using UserService.Api.Configurations;
using UserService.Api.Middlewares;
using UserService.Application.Contracts.Keycloak;
using UserService.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Configuração de serviços
builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

var app = builder.Build();

// Middleware de tratamento de exceções
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configuração do pipeline por ambiente
if (app.Environment.IsDevelopment())
{
    // Configura o pipeline para ambiente de desenvolvimento
    app.UseDeveloperExceptionPage();
    
    // Executa testes de integração básicos durante a inicialização
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    
    // Teste de conexão com banco de dados
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserServiceDbContext>();
        await dbContext.Database.CanConnectAsync();
        logger.LogInformation("✅ Teste de conexão Entity Framework: SUCESSO");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Teste de conexão Entity Framework: FALHOU");
    }
    
    // Teste de injeção de dependência
    try
    {
        using var scope = app.Services.CreateScope();
        var keycloakService = scope.ServiceProvider.GetRequiredService<IKeycloakService>();
        logger.LogInformation("✅ Teste de Injeção de Dependência: SUCESSO");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Teste de Injeção de Dependência: FALHOU");
    }
}
else
{
    // Configura o pipeline para ambiente de produção
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Configura cabeçalhos de segurança HTTP
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["X-XSS-Protection"] = "1; mode=block";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Content-Security-Policy"] = 
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'";
    
    await next();
});

// Pipeline principal de middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

// Configura logging de inicialização da aplicação
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("API de Gerenciamento de Usuários B-Commerce iniciando...");
startupLogger.LogInformation("Ambiente: {Environment}", app.Environment.EnvironmentName);
startupLogger.LogInformation("URL do Keycloak: {KeycloakUrl}", builder.Configuration["Keycloak:AuthServerUrl"]);
startupLogger.LogInformation("Realm do Keycloak: {Realm}", builder.Configuration["Keycloak:Realm"]);

// Inicialização da aplicação
try
{
    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "Falha crítica na inicialização da aplicação B-Commerce User API");
    throw;
}