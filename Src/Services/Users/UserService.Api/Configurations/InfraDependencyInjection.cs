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
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
                               ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        AddDatabase(services, configuration);
        AddServices(services, configuration);
        AddKeycloak(services, configuration);
        services.AddMigrations(connectionString);
    }
    public static IServiceCollection AddMigrations(this IServiceCollection services, string connectionString)
    {
        return services;
        // return services
        //     .AddFluentMigratorCore()
        //     .ConfigureRunner(rb => rb
        //         .AddPostgres()
        //         .WithGlobalConnectionString(connectionString)
        //         .ScanIn(typeof(InitialSetup).Assembly).For.Migrations())
        //     .AddLogging(lb => lb.AddFluentMigratorConsole());
    }

    
    /// <summary>
    /// Configura o Entity Framework e o banco de dados PostgreSQL.
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    /// <param name="configuration">Configuração da aplicação</param>
    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<UserServiceDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            
            // Configurações adicionais para desenvolvimento
            if (configuration.GetValue<bool>("Development:LogSqlQueries", false))
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });
    }

    /// <summary>
    /// Registra os serviços específicos da aplicação no container de DI.
    /// Estes serviços implementam a lógica de negócio da aplicação.
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    /// <param name="configuration">Configuração da aplicação</param>
    private static void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        // Serviço para criptografia de senhas (bcrypt, argon2, etc.)
        services.AddScoped<IPasswordEncripter, PasswordEncripter>();
        
        // Serviço para envio de emails (ativação, boas-vindas, reset de senha)
        services.AddScoped<IEmailService, EmailService>();
        
        // Serviço para geração e validação de tokens (ativação, reset, etc.)
        services.AddScoped<ITokenService, TokenService>();
    }
    
    
    /// <summary>
    /// Configura serviços do Keycloak para integração com o provedor de identidade
    /// </summary>
    private static void AddKeycloak(IServiceCollection services, IConfiguration configuration)
    {
        // Registra as configurações do Keycloak no container de DI
        services.Configure<KeycloakSettings>(
            configuration.GetSection(KeycloakSettings.SectionName));

        // Configura HttpClient para comunicação com o servidor Keycloak
        // Usado para operações administrativas como criação de usuários
        services.AddHttpClient<IKeycloakService, KeycloakService>();

        // Registra o serviço do Keycloak com escopo por requisição
        services.AddScoped<IKeycloakService, KeycloakService>();
    }
    
    /// <summary>
    /// Configura provedores de logging para monitoramento e debugging
    /// </summary>
    private static void AddLogging(IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging(builder =>
        {
            // Remove provedores padrão para configuração customizada
            builder.ClearProviders();
            
            // Adiciona logging no console para todas as aplicações
            builder.AddConsole();
            
            // Em desenvolvimento, adiciona logging de debug para mais detalhes
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                builder.AddDebug();
            }
        });
    }
    
    /// <summary>
    /// Configura as configurações de email para envio de notificações
    /// </summary>
    private static void AddEmailSettings(IServiceCollection services, IConfiguration configuration)
    {
        // services.Configure<EmailSettings>(
        //     configuration.GetSection(EmailSettings.SectionName));
    }
    
    

    
    public static void RunMigrations(IServiceProvider serviceProvider)
    {
        // using var scope = serviceProvider.CreateScope();
        // var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        //
        // runner.MigrateUp();
    }

    public static void RollbackMigrations(IServiceProvider serviceProvider, long targetVersion = 0)
    {
        // using var scope = serviceProvider.CreateScope();
        // var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        //
        // if (targetVersion == 0)
        // {
        //     runner.MigrateDown(0);
        // }
        // else
        // {
        //     runner.MigrateDown(targetVersion);
        // }
    }
}