using Bcommerce.User.Domain.Repositories;
using Bcommerce.User.Domain.Users;
using Bcommerce.User.Infrastructure.Data;
using Bcommerce.User.Infrastructure.Identity;
using Bcommerce.User.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bcommerce.User.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<UserDbContext>(options =>
            options.UseNpgsql(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(UserDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            })
            // Use Snake Case naming convention usually used in Postgres, or stick to configured names
            .UseSnakeCaseNamingConvention()
        );

        services.AddIdentityConfiguration();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }
}
