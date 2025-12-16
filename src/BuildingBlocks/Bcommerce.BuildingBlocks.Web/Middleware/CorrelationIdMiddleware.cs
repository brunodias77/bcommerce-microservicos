using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Bcommerce.BuildingBlocks.Web.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out StringValues correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers.Append(CorrelationIdHeader, correlationId);
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Append(CorrelationIdHeader, correlationId);
            return Task.CompletedTask;
        });

        // Set trace identifier for logging context
        context.TraceIdentifier = correlationId.ToString();

        await _next(context);
    }
}
