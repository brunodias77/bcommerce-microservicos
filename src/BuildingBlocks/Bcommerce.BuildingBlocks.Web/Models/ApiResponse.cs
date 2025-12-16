using Newtonsoft.Json;

namespace Bcommerce.BuildingBlocks.Web.Models;

public class ApiResponse<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public ErrorResponse? Error { get; private set; }

    // Construtor para sucesso
    public ApiResponse(T data)
    {
        Success = true;
        Data = data;
        Error = null;
    }

    // Construtor para erro
    public ApiResponse(ErrorResponse error)
    {
        Success = false;
        Data = default;
        Error = error;
    }

    public static ApiResponse<T> Ok(T data) => new ApiResponse<T>(data);
    public static ApiResponse<T> Fail(ErrorResponse error) => new ApiResponse<T>(error);
    public static ApiResponse<T> Fail(string message, string? code = null) => new ApiResponse<T>(new ErrorResponse(message, code));
}
