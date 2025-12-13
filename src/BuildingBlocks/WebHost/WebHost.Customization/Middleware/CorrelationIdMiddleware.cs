using Microsoft.AspNetCore.Http;

namespace WebHost.Customization.Middleware;

/// <summary>
/// Middleware para garantir Correlation ID em todas as requisições
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeaderName = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Adiciona ao response header também
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId)
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            context.Items["CorrelationId"] = correlationId.ToString();
            return correlationId.ToString();
        }

        var newCorrelationId = Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = newCorrelationId;
        context.Request.Headers.TryAdd(CorrelationIdHeaderName, newCorrelationId);
        return newCorrelationId;
    }
}
