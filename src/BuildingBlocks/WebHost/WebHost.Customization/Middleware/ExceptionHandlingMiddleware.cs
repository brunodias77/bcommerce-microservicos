using Common.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace WebHost.Customization.Middleware;

/// <summary>
/// Middleware global para tratamento de exceções
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        _logger.LogError(
            exception,
            "An unhandled exception occurred. CorrelationId: {CorrelationId}",
            correlationId);

        var (statusCode, title, errors) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "Validation Error",
                validationEx.Errors
            ),
            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                "Resource Not Found",
                new Dictionary<string, string[]> { { "error", new[] { notFoundEx.Message } } }
            ),
            BusinessRuleException businessEx => (
                HttpStatusCode.BadRequest,
                "Business Rule Violation",
                new Dictionary<string, string[]> { { businessEx.Code, new[] { businessEx.Message } } }
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                new Dictionary<string, string[]> { { "error", new[] { "An unexpected error occurred" } } }
            )
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new
        {
            type = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status = (int)statusCode,
            errors,
            traceId = correlationId,
            instance = context.Request.Path.Value
        };

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
