using Bcommerce.BuildingBlocks.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bcommerce.BuildingBlocks.Web.Filters;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .SelectMany(x => x.Value.Errors.Select(e => new ValidationErrorDetails
                {
                    Field = x.Key,
                    Message = e.ErrorMessage
                }))
                .ToList();

            var errorResponse = new ErrorResponse("Erros de validação encontrados.")
            {
                ValidationErrors = errors
            };

            context.Result = new BadRequestObjectResult(ApiResponse<object>.Fail(errorResponse));
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
