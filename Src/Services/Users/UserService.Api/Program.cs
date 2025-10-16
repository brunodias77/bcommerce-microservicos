// ============================================================================
// B-Commerce User Management API - Program.cs
// ============================================================================
// Este arquivo é o ponto de entrada principal da aplicação ASP.NET Core.
// Utiliza o padrão Minimal API do .NET 6+ que simplifica a configuração
// e inicialização da aplicação, eliminando a necessidade das classes
// Startup.cs e Program.cs separadas do .NET Framework.
// ============================================================================

using UserService.Api.Configurations;
using UserService.Api.Extensions;
using UserService.Api.Middlewares;

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
    app.ConfigureDevelopmentPipeline();
    await app.RunIntegrationTestsAsync();
}
else
{
    app.ConfigureProductionPipeline();
}

// Pipeline de middleware principal
app.UseSecurityHeaders()
   .ConfigureMiddlewarePipeline()
   .ConfigureStartupLogging(builder.Configuration);

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