using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace WebHost.Customization.Middleware;

/// <summary>
/// Middleware para enriquecer traces com informações customizadas
/// </summary>
public class TracingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ActivitySource _activitySource;

    public TracingMiddleware(RequestDelegate next, ActivitySource activitySource)
    {
        _next = next;
        _activitySource = activitySource;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var activity = _activitySource.StartActivity(
            $"{context.Request.Method} {context.Request.Path}",
            ActivityKind.Server);

        if (activity != null)
        {
            // Add custom tags
            activity.SetTag("http.request.id", context.TraceIdentifier);
            activity.SetTag("user.id", context.User?.Identity?.Name ?? "anonymous");

            if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            {
                activity.SetTag("correlation.id", correlationId.ToString());
            }

            try
            {
                await _next(context);

                activity.SetTag("http.response.status", context.Response.StatusCode);

                if (context.Response.StatusCode >= 400)
                {
                    activity.SetStatus(ActivityStatusCode.Error);
                }
            }
            catch (Exception ex)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }
        else
        {
            await _next(context);
        }
    }
}
