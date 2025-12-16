using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;

namespace Bcommerce.BuildingBlocks.Observability.Metrics;

public static class MetricsConfiguration
{
    public static IHostApplicationBuilder AddCustomMetrics(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddPrometheusExporter(); // Exporta para o endpoint /metrics
            });

        return builder;
    }

    public static WebApplication UseCustomMetrics(this WebApplication app)
    {
        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        return app;
    }
}
