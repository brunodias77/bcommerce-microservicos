using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Bcommerce.BuildingBlocks.Observability.Logging;

public static class LoggingConfiguration
{
    public static IHostBuilder UseCustomSerilog(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .WriteTo.Console();

            // Opicional: Configurar leitura do appsettings.json aqui se necessário
            // configuration.ReadFrom.Configuration(context.Configuration);
        });
    }
}
