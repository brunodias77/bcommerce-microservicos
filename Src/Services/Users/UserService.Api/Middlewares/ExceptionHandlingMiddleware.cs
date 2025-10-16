using BuildingBlocks.Results;
using BuildingBlocks.Validations;
using UserService.Domain.Exceptions;

namespace UserService.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu uma exceção não tratada: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, response) = GetErrorResponse(exception);

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(response);
    }

    private (int StatusCode, ApiResponse Response) GetErrorResponse(Exception exception)
    {
        return exception switch
        {
            BusinessException businessEx => (
                StatusCodes.Status400BadRequest,
                ApiResponse.Fail(businessEx.Errors)
            ),
            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                ApiResponse.Fail("NOT_FOUND", notFoundEx.Message)
            ),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                ApiResponse.Fail("UNAUTHORIZED", "Acesso não autorizado")
            ),
            ArgumentException argEx => (
                StatusCodes.Status400BadRequest,
                ApiResponse.Fail("INVALID_ARGUMENT", argEx.Message)
            ),
            _ => GetInternalServerErrorResponse(exception)
        };
    }

    private (int StatusCode, ApiResponse Response) GetInternalServerErrorResponse(Exception exception)
    {
        var message = _environment.IsDevelopment() 
            ? $"{exception.Message}\n{exception.StackTrace}"
            : "Ocorreu um erro interno no servidor. Por favor, tente novamente mais tarde.";

        return (
            StatusCodes.Status500InternalServerError,
            ApiResponse.Fail("INTERNAL_ERROR", message)
        );
    }
}
