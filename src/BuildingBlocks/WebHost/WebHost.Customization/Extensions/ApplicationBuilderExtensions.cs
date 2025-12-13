using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using WebHost.Customization.Middleware;

namespace WebHost.Customization.Extensions;

/// <summary>
/// Extensões para configuração do pipeline de requisições
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configura o pipeline de requisições com middleware customizados
    /// </summary>
    public static IApplicationBuilder UseCustomMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        return app;
    }

    /// <summary>
    /// Configura Swagger UI
    /// </summary>
    public static IApplicationBuilder UseCustomSwagger(
        this IApplicationBuilder app,
        string title,
        string version = "v1")
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"{title} {version}");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = title;
        });

        return app;
    }

    /// <summary>
    /// Configura Health Checks UI
    /// </summary>
    public static IApplicationBuilder UseCustomHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.ToString()
                    }),
                    totalDuration = report.TotalDuration.ToString()
                });
                await context.Response.WriteAsync(result);
            }
        });

        return app;
    }

    /// <summary>
    /// Migra automaticamente o banco de dados em ambiente de desenvolvimento
    /// </summary>
    public static IApplicationBuilder MigrateDatabase<TContext>(this IApplicationBuilder app)
        where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var environment = services.GetRequiredService<IHostEnvironment>();

        if (environment.IsDevelopment())
        {
            var context = services.GetRequiredService<TContext>();
            context.Database.Migrate();
        }

        return app;
    }
}