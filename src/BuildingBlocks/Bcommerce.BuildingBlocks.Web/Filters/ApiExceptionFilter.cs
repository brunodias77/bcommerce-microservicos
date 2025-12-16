using Bcommerce.BuildingBlocks.Core.Exceptions;
using Bcommerce.BuildingBlocks.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Bcommerce.BuildingBlocks.Web.Filters;

public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        _logger.LogError(exception, "Erro capturado pelo ApiExceptionFilter.");

        var statusCode = exception switch
        {
            DomainException => (int)HttpStatusCode.BadRequest,
            ValidationException => (int)HttpStatusCode.BadRequest,
            NotFoundException => (int)HttpStatusCode.NotFound,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var errorResponse = new ErrorResponse(exception.Message);
        
        if (exception is ValidationException validationException)
        {
            errorResponse.ValidationErrors = validationException.Errors
                .SelectMany(kvp => kvp.Value.Select(msg => new ValidationErrorDetails
                {
                    Field = kvp.Key,
                    Message = msg
                }))
                .ToList();
        }

        context.Result = new ObjectResult(ApiResponse<object>.Fail(errorResponse))
        {
            StatusCode = statusCode
        };
        
        context.ExceptionHandled = true;
    }
}
