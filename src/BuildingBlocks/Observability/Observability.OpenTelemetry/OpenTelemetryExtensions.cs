using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Observability.Abstractions;

namespace Observability.OpenTelemetry;

public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Configura OpenTelemetry completo (Traces, Metrics, Logs)
    /// </summary>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        string serviceVersion)
    {
        var otelSettings = configuration.GetSection("OpenTelemetry").Get<OpenTelemetrySettings>()
                           ?? new OpenTelemetrySettings();

        // Resource Definition (comum para traces e metrics)
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(
                serviceName: serviceName,
                serviceVersion: serviceVersion,
                serviceInstanceId: Environment.MachineName)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = configuration["Environment"] ?? "development",
                ["host.name"] = Environment.MachineName,
                ["service.namespace"] = "ecommerce"
            });

        // ========================================
        // DISTRIBUTED TRACING
        // ========================================
        services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource(serviceName)

                    // ASP.NET Core Instrumentation
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.request.path", request.Path);
                            activity.SetTag("http.request.method", request.Method);
                            activity.SetTag("http.request.user_agent", request.Headers.UserAgent.ToString());
                        };
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.response.status_code", response.StatusCode);
                        };
                    })

                    // HTTP Client Instrumentation
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequestMessage = (activity, request) =>
                        {
                            activity.SetTag("http.request.url", request.RequestUri?.ToString());
                        };
                        options.EnrichWithHttpResponseMessage = (activity, response) =>
                        {
                            activity.SetTag("http.response.success", response.IsSuccessStatusCode);
                        };
                    })

                    // Entity Framework Core Instrumentation
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.EnrichWithIDbCommand = (activity, command) =>
                        {
                            activity.SetTag("db.query.text", command.CommandText);
                        };
                    })

                    // SQL Client Instrumentation
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    });

                // Exporters
                if (otelSettings.UseConsoleExporter)
                {
                    tracerProviderBuilder.AddConsoleExporter();
                }

                // Jaeger exporter não habilitado por padrão; usar OTLP ou Console

                if (otelSettings.UseOtlpExporter && !string.IsNullOrEmpty(otelSettings.OtlpEndpoint))
                {
                    tracerProviderBuilder.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otelSettings.OtlpEndpoint);
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
                }
            })

            // ========================================
            // METRICS
            // ========================================
            .WithMetrics(meterProviderBuilder =>
            {
                meterProviderBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .AddMeter(serviceName)

                    // ASP.NET Core Metrics
                    .AddAspNetCoreInstrumentation()

                    // HTTP Client Metrics
                    .AddHttpClientInstrumentation()

                    // Runtime Metrics
                    .AddRuntimeInstrumentation()

                    // Process Metrics
                    .AddProcessInstrumentation();

                // Exporters
                if (otelSettings.UseConsoleExporter)
                {
                    meterProviderBuilder.AddConsoleExporter();
                }

                if (otelSettings.UsePrometheus)
                {
                    meterProviderBuilder.AddPrometheusExporter();
                }

                if (otelSettings.UseOtlpExporter && !string.IsNullOrEmpty(otelSettings.OtlpEndpoint))
                {
                    meterProviderBuilder.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otelSettings.OtlpEndpoint);
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
                }
            });

        // Custom Metrics Service
        services.AddSingleton<IMetricsService, MetricsService>();

        return services;
    }

    /// <summary>
    /// Adiciona Prometheus scraping endpoint
    /// </summary>
    public static IApplicationBuilder UsePrometheusMetrics(this IApplicationBuilder app)
    {
        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        return app;
    }
}
