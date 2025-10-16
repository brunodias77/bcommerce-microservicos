using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using UserService.Application.Contracts;
using UserService.Application.Contracts.Keycloak;
using UserService.Infrastructure.Data;
using UserService.Infrastructure.Services.Email;
using UserService.Infrastructure.Services.Keycloak;
using UserService.Infrastructure.Services.Security;

namespace UserService.Api.Configurations;

public static class InfraDependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddEntityFramework(services, configuration);
        AddApplicationServices(services);
        AddKeycloakIntegration(services, configuration);
        AddLogging(services);
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
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
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
        services.Configure<KeycloakSettings>(configuration.GetSection(KeycloakSettings.SectionName));
        services.AddHttpClient<IKeycloakService, KeycloakService>();
        services.AddScoped<IKeycloakService, KeycloakService>();
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