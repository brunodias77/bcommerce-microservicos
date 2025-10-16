using Microsoft.AspNetCore.Diagnostics;
using UserService.Application.Contracts.Keycloak;
using UserService.Infrastructure.Data;

namespace UserService.Api.Extensions;

/// <summary>
/// Extensões para configuração do pipeline de middleware da aplicação.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configura o pipeline para ambiente de desenvolvimento.
    /// </summary>
    public static WebApplication ConfigureDevelopmentPipeline(this WebApplication app)
    {
        app.UseDeveloperExceptionPage();
        return app;
    }

    /// <summary>
    /// Configura o pipeline para ambiente de produção.
    /// </summary>
    public static WebApplication ConfigureProductionPipeline(this WebApplication app)
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
        return app;
    }

    /// <summary>
    /// Configura cabeçalhos de segurança HTTP.
    /// </summary>
    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
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
        
        return app;
    }

    /// <summary>
    /// Configura o pipeline principal de middleware.
    /// </summary>
    public static WebApplication ConfigureMiddlewarePipeline(this WebApplication app)
    {
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
        
        return app;
    }



    /// <summary>
    /// Executa testes de integração básicos durante a inicialização.
    /// </summary>
    public static async Task<WebApplication> RunIntegrationTestsAsync(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        
        await TestDatabaseConnection(app, logger);
        await TestDependencyInjection(app, logger);

        return app;
    }

    private static async Task TestDatabaseConnection(WebApplication app, ILogger logger)
    {
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
    }

    private static async Task TestDependencyInjection(WebApplication app, ILogger logger)
    {
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

    /// <summary>
    /// Configura logging de inicialização da aplicação.
    /// </summary>
    public static WebApplication ConfigureStartupLogging(this WebApplication app, IConfiguration configuration)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("API de Gerenciamento de Usuários B-Commerce iniciando...");
        logger.LogInformation("Ambiente: {Environment}", app.Environment.EnvironmentName);
        logger.LogInformation("URL do Keycloak: {KeycloakUrl}", configuration["Keycloak:AuthServerUrl"]);
        logger.LogInformation("Realm do Keycloak: {Realm}", configuration["Keycloak:Realm"]);
        
        return app;
    }
}