using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebHost.Customization.Filters;

/// <summary>
/// Filtro para validação automática do ModelState
/// </summary>
public class ValidateModelStateFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );

            var problemDetails = new ValidationProblemDetails(errors)
            {
                Status = 400,
                Title = "Erro de Validação",
                Instance = context.HttpContext.Request.Path
            };

            context.Result = new BadRequestObjectResult(problemDetails);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Não faz nada após a execução
    }
}
