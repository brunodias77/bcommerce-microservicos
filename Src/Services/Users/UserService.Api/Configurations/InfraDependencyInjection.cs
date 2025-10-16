using UserService.Application.Contracts;
using UserService.Infrastructure.Services.Email;
using UserService.Infrastructure.Services.Security;

namespace UserService.Api.Configurations;

public static class InfraDependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddServices(services);
    }
    
    /// <summary>
    /// Registra os serviços específicos da aplicação no container de DI.
    /// Estes serviços implementam a lógica de negócio da aplicação.
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    private static void AddServices(IServiceCollection services)
    {
        // Serviço para criptografia de senhas (bcrypt, argon2, etc.)
        services.AddScoped<IPasswordEncripter, PasswordEncripter>();
        
        // Serviço para envio de emails (ativação, boas-vindas, reset de senha)
        services.AddScoped<IEmailService, EmailService>();
        
        // Serviço para geração e validação de tokens (ativação, reset, etc.)
        services.AddScoped<ITokenService, TokenService>();
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