namespace Bcommerce.BuildingBlocks.Web.Models;

public class ErrorResponse
{
    public string Message { get; set; }
    public string? Code { get; set; }
    public List<ValidationErrorDetails>? ValidationErrors { get; set; }

    public ErrorResponse(string message, string? code = null)
    {
        Message = message;
        Code = code;
    }
}

public class ValidationErrorDetails
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
