using BuildingBlocks.Data;
using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using UserService.Application.Contracts;
using UserService.Application.Contracts.Keycloak;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Contracts;
using UserService.Infrastructure.Data;
using UserService.Infrastructure.Data.Repositories;
using UserService.Infrastructure.Services.Email;
using UserService.Infrastructure.Services.Keycloak;
using UserService.Infrastructure.Services.Security;

namespace UserService.Api.Configurations;

public static class InfraDependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddEntityFramework(services, configuration);
        AddRepositories(services);
        AddApplicationServices(services);
        AddKeycloakIntegration(services, configuration);
        AddLogging(services);
    }
    
    /// <summary>
    /// Registra repositórios e Unit of Work no container de DI.
    /// </summary>
    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserTokenRepository, UserTokenRepository>();
    }

    /// <summary>
    /// Configura Entity Framework Core com PostgreSQL.
    /// </summary>
    private static void AddEntityFramework(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                              ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada");

        services.AddDbContext<UserServiceDbContext>(options =>
        {
            ConfigureNpgsql(options, connectionString);
            ConfigureLogging(options, configuration);
        });
    }

    private static void ConfigureNpgsql(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(UserServiceDbContext).Assembly.FullName);
            // Removido EnableRetryOnFailure para permitir transações manuais
            // npgsqlOptions.EnableRetryOnFailure(
            //     maxRetryCount: 3,
            //     maxRetryDelay: TimeSpan.FromSeconds(30),
            //     errorCodesToAdd: null);
        });
    }

    private static void ConfigureLogging(DbContextOptionsBuilder options, IConfiguration configuration)
    {
        if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
        {
            options.EnableSensitiveDataLogging();
        }
    }

    /// <summary>
    /// Registra serviços da camada de aplicação.
    /// </summary>
    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IPasswordEncripter, PasswordEncripter>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ITokenService, TokenService>();
    }

    /// <summary>
    /// Configura integração com Keycloak.
    /// </summary>
    private static void AddKeycloakIntegration(IServiceCollection services, IConfiguration configuration)
    {
        // Resolve as variáveis de ambiente corretamente
        var keycloakUrl = Environment.GetEnvironmentVariable("KEYCLOAK_URL") ?? configuration["Keycloak:Url"] ?? "http://localhost:8080";
        var keycloakRealm = Environment.GetEnvironmentVariable("KEYCLOAK_REALM") ?? configuration["Keycloak:Realm"] ?? "b-commerce";
        var adminUsername = Environment.GetEnvironmentVariable("KEYCLOAK_ADMIN_USER") ?? configuration["Keycloak:AdminUsername"] ?? "admin";
        var adminPassword = Environment.GetEnvironmentVariable("KEYCLOAK_ADMIN_PASSWORD") ?? configuration["Keycloak:AdminPassword"] ?? "admin123";
        
        // Debug logging
        Console.WriteLine($"🔍 DEBUG - Keycloak URL para HttpClient: '{keycloakUrl}'");
        Console.WriteLine($"🔍 DEBUG - URL é válida: {Uri.IsWellFormedUriString(keycloakUrl, UriKind.Absolute)}");
        
        services.Configure<KeycloakSettings>(options =>
        {
            options.Url = keycloakUrl;
            options.Realm = keycloakRealm;
            options.AdminUsername = adminUsername;
            options.AdminPassword = adminPassword;
            options.BackendClientId = configuration["Keycloak:BackendClientId"] ?? "backend-api";
            options.BackendClientSecret = configuration["Keycloak:BackendClientSecret"] ?? "backend-api-secret";
            options.FrontendClientId = configuration["Keycloak:FrontendClientId"] ?? "frontend-app";
            options.TokenExpirationMinutes = configuration.GetValue<int>("Keycloak:TokenExpirationMinutes", 60);
            options.RefreshTokenExpirationDays = configuration.GetValue<int>("Keycloak:RefreshTokenExpirationDays", 7);
        });
        
        services.AddHttpClient<IKeycloakService, KeycloakService>(client =>
        {
            Console.WriteLine($"🔍 DEBUG - Configurando HttpClient com URL: '{keycloakUrl}'");
            if (!string.IsNullOrEmpty(keycloakUrl) && Uri.IsWellFormedUriString(keycloakUrl, UriKind.Absolute))
            {
                client.BaseAddress = new Uri(keycloakUrl);
                Console.WriteLine($"✅ DEBUG - BaseAddress configurado: {client.BaseAddress}");
            }
            else
            {
                Console.WriteLine($"❌ DEBUG - URL inválida ou vazia: '{keycloakUrl}'");
            }
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        // Remove a segunda declaração que pode estar causando conflito
        // services.AddScoped<IKeycloakService, KeycloakService>();
    }

    /// <summary>
    /// Configura logging estruturado.
    /// </summary>
    private static void AddLogging(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
    }
}