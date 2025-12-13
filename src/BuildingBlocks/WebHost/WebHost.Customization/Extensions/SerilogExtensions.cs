using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace WebHost.Customization.Extensions;

public static class SerilogExtensions
{
    public static void AddSerilogLogging(this WebApplicationBuilder builder, string applicationName)
    {
        var configuration = builder.Configuration;

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", applicationName)
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .WriteTo.Console()
            .CreateLogger();

        builder.Host.UseSerilog(Log.Logger, dispose: true);
    }

    public static IApplicationBuilder UseSerilogRequestLoggingWithEnrichment(this IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = 
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                if (httpContext.Items.TryGetValue("CorrelationId", out var correlationId))
                {
                    diagnosticContext.Set("CorrelationId", correlationId);
                }

                diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
                diagnosticContext.Set("UserId", httpContext.User?.Identity?.Name);
                diagnosticContext.Set("Protocol", httpContext.Request.Protocol);
                diagnosticContext.Set("Scheme", httpContext.Request.Scheme);
                diagnosticContext.Set("Host", httpContext.Request.Host.ToString());
            };
        });

        return app;
    }
}

