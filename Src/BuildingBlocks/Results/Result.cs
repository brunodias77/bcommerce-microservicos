using BuildingBlocks.Validations;

namespace BuildingBlocks.Results;

/// <summary>
/// Representa o resultado de uma operação que pode ter sucesso ou falha
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? ErrorMessage { get; }
    public List<Error>? Errors { get; }

    protected Result(bool isSuccess, string? errorMessage = null, List<Error>? errors = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Errors = errors;
    }

    /// <summary>
    /// Cria um resultado de sucesso
    /// </summary>
    public static Result Success() => new(true);

    /// <summary>
    /// Cria um resultado de falha com uma mensagem de erro
    /// </summary>
    public static Result Failure(string errorMessage) => new(false, errorMessage);

    /// <summary>
    /// Cria um resultado de falha com múltiplos erros
    /// </summary>
    public static Result Failure(List<Error> errors) => new(false, errors: errors);

    /// <summary>
    /// Cria um resultado de falha a partir de um ValidationHandler
    /// </summary>
    public static Result Failure(ValidationHandler validation) => new(false, errors: validation.Errors.ToList());

    /// <summary>
    /// Conversão implícita de bool para Result
    /// </summary>
    public static implicit operator Result(bool success) => success ? Success() : Failure("Operação falhou");
}

/// <summary>
/// Representa o resultado de uma operação que pode ter sucesso ou falha com um valor de retorno
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(bool isSuccess, T? value = default, string? errorMessage = null, List<Error>? errors = null)
        : base(isSuccess, errorMessage, errors)
    {
        Value = value;
    }

    /// <summary>
    /// Cria um resultado de sucesso com um valor
    /// </summary>
    public static Result<T> Success(T value) => new(true, value);

    /// <summary>
    /// Cria um resultado de sucesso sem valor
    /// </summary>
    public static new Result<T> Success() => new(true);

    /// <summary>
    /// Cria um resultado de falha com uma mensagem de erro
    /// </summary>
    public static new Result<T> Failure(string errorMessage) => new(false, errorMessage: errorMessage);

    /// <summary>
    /// Cria um resultado de falha com múltiplos erros
    /// </summary>
    public static new Result<T> Failure(List<Error> errors) => new(false, errors: errors);

    /// <summary>
    /// Cria um resultado de falha a partir de um ValidationHandler
    /// </summary>
    public static new Result<T> Failure(ValidationHandler validation) => new(false, errors: validation.Errors.ToList());

    /// <summary>
    /// Conversão implícita de T para Result<T>
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);
}