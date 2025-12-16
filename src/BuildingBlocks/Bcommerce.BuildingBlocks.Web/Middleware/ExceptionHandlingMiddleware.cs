using Bcommerce.BuildingBlocks.Core.Exceptions;
using Bcommerce.BuildingBlocks.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net;

namespace Bcommerce.BuildingBlocks.Web.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        _logger.LogError(exception, "Ocorreu uma exceção não tratada.");

        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = GetErrorResponse(exception);
        response.StatusCode = GetStatusCode(exception);

        var json = JsonConvert.SerializeObject(ApiResponse<object>.Fail(errorResponse), new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        
        await response.WriteAsync(json);
    }

    private static ErrorResponse GetErrorResponse(Exception exception)
    {
        var response = new ErrorResponse(exception.Message);

        if (exception is ValidationException validationException)
        {
            response.ValidationErrors = validationException.Errors
                .SelectMany(kvp => kvp.Value.Select(msg => new ValidationErrorDetails
                {
                    Field = kvp.Key,
                    Message = msg
                }))
                .ToList();
        }

        return response;
    }

    private static int GetStatusCode(Exception exception) => exception switch
    {
        DomainException => (int)HttpStatusCode.BadRequest,
        ValidationException => (int)HttpStatusCode.BadRequest,
        NotFoundException => (int)HttpStatusCode.NotFound,
        UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
        _ => (int)HttpStatusCode.InternalServerError
    };
}
