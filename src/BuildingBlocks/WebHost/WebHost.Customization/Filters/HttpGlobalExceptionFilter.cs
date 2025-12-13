using Common.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Net;


namespace WebHost.Customization.Filters;

/// <summary>
/// Filtro global para tratamento de exceções em controllers
/// </summary>
public class HttpGlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<HttpGlobalExceptionFilter> _logger;

    public HttpGlobalExceptionFilter(ILogger<HttpGlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(
            context.Exception,
            "An exception occurred: {ExceptionMessage}",
            context.Exception.Message);

        var problemDetails = context.Exception switch
        {
            ValidationException validationEx => new ValidationProblemDetails(validationEx.Errors)
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Validation Error",
                Instance = context.HttpContext.Request.Path
            },
            NotFoundException notFoundEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.NotFound,
                Title = "Resource Not Found",
                Detail = notFoundEx.Message,
                Instance = context.HttpContext.Request.Path
            },
            BusinessRuleException businessEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Business Rule Violation",
                Detail = businessEx.Message,
                Instance = context.HttpContext.Request.Path,
                Extensions = { { "code", businessEx.Code } }
            },
            _ => new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred",
                Instance = context.HttpContext.Request.Path
            }
        };

        context.Result = new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };

        context.ExceptionHandled = true;
    }
}