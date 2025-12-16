using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bcommerce.BuildingBlocks.Web.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var statusCode = context.Response?.StatusCode;
            var level = statusCode >= 500 ? LogLevel.Error : LogLevel.Information;

            _logger.Log(level, "Requisição finalizada: {Method} {Path} - Status: {StatusCode} - Tempo: {Elapsed}ms",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                sw.ElapsedMilliseconds);
        }
    }
}
