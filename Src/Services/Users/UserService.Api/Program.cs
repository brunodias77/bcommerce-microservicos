using UserService.Api.Configurations;
using UserService.Api.Middlewares;
using FluentMigrator.Runner;
using UserService.Infrastructure;
using UserService.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ===================================================================
// SERVICE CONFIGURATION
// ===================================================================
ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

// ===================================================================
// MIDDLEWARE PIPELINE
// ===================================================================
await ConfigureMiddlewarePipelineAsync(app);

// ===================================================================
// APPLICATION STARTUP
// ===================================================================
await RunApplicationAsync(app);

return;

// ===================================================================
// HELPER METHODS
// ===================================================================

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddControllers();
    services.AddInfrastructure(configuration);
    services.AddApplication(configuration);
    
    // Configuração do FluentMigrator
    ConfigureFluentMigrator(services, configuration);
}

static void ConfigureFluentMigrator(IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection")
                          ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada");

    services.AddFluentMigratorCore()
        .ConfigureRunner(rb => rb
            .AddPostgres()
            .WithGlobalConnectionString(connectionString)
            .ScanIn(typeof(UserServiceDbContext).Assembly).For.Migrations())
        .AddLogging(lb => lb.AddFluentMigratorConsole());
}

static async Task ConfigureMiddlewarePipelineAsync(WebApplication app)
{
    // Exception Handling
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // Environment-specific configuration
    if (app.Environment.IsDevelopment())
    {
        await ConfigureDevelopmentEnvironmentAsync(app);
    }
    else
    {
        ConfigureProductionEnvironment(app);
    }

    // Security Headers
    ConfigureSecurityHeaders(app);

    // API Documentation (Development only)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "API de Usuários v1");
            options.RoutePrefix = "swagger";
        });
    }

    // Core Middleware Pipeline
    app.UseHttpsRedirection();
    app.UseCors("DefaultPolicy");
    app.UseAuthentication();
    app.UseAuthorization();

    // Endpoints
    app.MapHealthChecks("/health");
    app.MapControllers();
}

static async Task ConfigureDevelopmentEnvironmentAsync(WebApplication app)
{
    app.UseDeveloperExceptionPage();

    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("========================================");
    logger.LogInformation("MODO DE DESENVOLVIMENTO - Executando Migrations e Testes de Integração");
    logger.LogInformation("========================================");

    // Executar migrations do FluentMigrator
    await RunFluentMigratorMigrationsAsync(app, logger);

    await RunIntegrationTestsAsync(app, logger);
}

static void ConfigureProductionEnvironment(WebApplication app)
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

static void ConfigureSecurityHeaders(WebApplication app)
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
}

static Task RunFluentMigratorMigrationsAsync(WebApplication app, ILogger logger)
{
    try
    {
        using var scope = app.Services.CreateScope();
        
        logger.LogInformation("🔄 Iniciando execução das migrations do FluentMigrator...");
        
        // Verificar se o IMigrationRunner está registrado
        var runner = scope.ServiceProvider.GetService<IMigrationRunner>();
        if (runner == null)
        {
            logger.LogError("❌ IMigrationRunner não está registrado no container de DI!");
            throw new InvalidOperationException("IMigrationRunner não foi registrado corretamente");
        }
        
        logger.LogInformation("✅ IMigrationRunner encontrado no container de DI");
        
        // Verificar connection string
        var configuration = app.Configuration;
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        logger.LogInformation("🔗 Connection String: {ConnectionString}", connectionString);
        
        // Verificar assembly das migrations
        var assembly = typeof(UserServiceDbContext).Assembly;
        logger.LogInformation("📦 Assembly das migrations: {AssemblyName}", assembly.FullName);
        
        // Listar migrations encontradas
        var migrationInfos = runner.MigrationLoader.LoadMigrations();
        logger.LogInformation("📋 Total de migrations encontradas: {Count}", migrationInfos.Count());
        
        foreach (var migration in migrationInfos)
        {
            logger.LogInformation("  - Migration: {Version} - {Description}", 
                migration.Key, migration.Value.Migration.GetType().Name);
        }

        // Verificar se há migrations pendentes
        if (runner.HasMigrationsToApplyUp())
        {
            logger.LogInformation("📋 Migrations pendentes encontradas. Executando...");
            
            // Executar migrations uma por vez para identificar qual está falhando
            var pendingMigrations = runner.MigrationLoader.LoadMigrations()
                .Where(m => !runner.HasMigrationsToApplyDown(m.Key))
                .OrderBy(m => m.Key);
                
            foreach (var migration in pendingMigrations)
            {
                try
                {
                    logger.LogInformation("🔄 Executando migration: {Version} - {Description}", 
                        migration.Key, migration.Value.Migration.GetType().Name);
                    runner.MigrateUp(migration.Key);
                    logger.LogInformation("✅ Migration {Version} executada com sucesso!", migration.Key);
                }
                catch (Exception migrationEx)
                {
                    logger.LogError(migrationEx, "❌ Erro na migration {Version}: {Message}", 
                        migration.Key, migrationEx.Message);
                    throw;
                }
            }
            
            logger.LogInformation("✅ Todas as migrations executadas com sucesso!");
        }
        else
        {
            logger.LogInformation("✅ Todas as migrations já estão aplicadas. Banco de dados atualizado!");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Erro ao executar migrations do FluentMigrator: {Message}", ex.Message);
        logger.LogError("❌ Stack Trace: {StackTrace}", ex.StackTrace);
        throw; // Re-throw para interromper a aplicação em caso de erro crítico
    }
    
    return Task.CompletedTask;
}

static async Task RunIntegrationTestsAsync(WebApplication app, ILogger logger)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    // Database Connection Test
    await TestDatabaseConnectionAsync(services, logger);

    // Dependency Injection Test
    TestDependencyInjection(services, logger);

    logger.LogInformation("========================================");
}

static async Task TestDatabaseConnectionAsync(IServiceProvider services, ILogger logger)
{
    try
    {
        var dbContext = services.GetRequiredService<UserService.Infrastructure.Data.UserServiceDbContext>();
        var canConnect = await dbContext.Database.CanConnectAsync();

        if (canConnect)
        {
            logger.LogInformation("✅ Teste de Conexão com o Banco de Dados: SUCESSO");
        }
        else
        {
            logger.LogWarning("⚠️  Teste de Conexão com o Banco de Dados: Conectado, mas sem resposta de consulta");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Teste de Conexão com o Banco de Dados: FALHOU - {Message}", ex.Message);    }
}

static void TestDependencyInjection(IServiceProvider services, ILogger logger)
{
    try
    {
        var keycloakService = services.GetRequiredService<UserService.Application.Contracts.Keycloak.IKeycloakService>();
        logger.LogInformation("✅ Teste de Injeção de Dependência: SUCESSO");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Teste de Injeção de Dependência: FALHOU - {Message}", ex.Message);
    }
}

static async Task RunApplicationAsync(WebApplication app)
{
    LogStartupInformation(app);

    try
    {
        await app.RunAsync();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "Falha crítica durante a inicialização da aplicação");
        throw;
    }
}

static void LogStartupInformation(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var configuration = app.Configuration;

    logger.LogInformation("========================================");
    logger.LogInformation("B-Commerce - API de Gerenciamento de Usuários");
    logger.LogInformation("========================================");
    logger.LogInformation("Ambiente: {Environment}", app.Environment.EnvironmentName);
    logger.LogInformation("URL do Keycloak: {KeycloakUrl}", configuration["Keycloak:Url"]);
    logger.LogInformation("Realm do Keycloak: {Realm}", configuration["Keycloak:Realm"]);
    logger.LogInformation("Admin Username: {AdminUsername}", configuration["Keycloak:AdminUsername"]);
    logger.LogInformation("KEYCLOAK_URL env: {EnvUrl}", Environment.GetEnvironmentVariable("KEYCLOAK_URL"));
    logger.LogInformation("KEYCLOAK_REALM env: {EnvRealm}", Environment.GetEnvironmentVariable("KEYCLOAK_REALM"));
    logger.LogInformation("========================================");
}