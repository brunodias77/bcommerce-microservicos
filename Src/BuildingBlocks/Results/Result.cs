using BuildingBlocks.Validations;

namespace BuildingBlocks.Results;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }
    public Error? Error => Errors.FirstOrDefault();

    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors ?? Array.Empty<Error>();
    }

    public static Result Success() => new(true, Array.Empty<Error>());
    public static Result Failure(Error error) => new(false, new[] { error });
    public static Result Failure(IReadOnlyList<Error> errors) => new(false, errors);
    public static Result Failure(string message) => new(false, new[] { new Error("ERROR", message) });

    public static Result<T> Success<T>(T value) => new(value, true, Array.Empty<Error>());
    public static Result<T> Failure<T>(Error error) => new(default!, false, new[] { error });
    public static Result<T> Failure<T>(IReadOnlyList<Error> errors) => new(default!, false, errors);
    public static Result<T> Failure<T>(string message) => new(default!, false, new[] { new Error("ERROR", message) });
}

public class Result<T> : Result
{
    public T Value { get; }

    internal Result(T value, bool isSuccess, IReadOnlyList<Error> errors) : base(isSuccess, errors)
    {
        Value = value;
    }
}