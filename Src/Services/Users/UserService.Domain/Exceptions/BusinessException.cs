using BuildingBlocks.Validations;

namespace UserService.Domain.Exceptions;

public class BusinessException : Exception
{
    public List<Error> Errors { get; }

    public BusinessException(string message) : base(message)
    {
        Errors = new List<Error> { new Error("BUSINESS_ERROR", message) };
    }

    public BusinessException(string code, string message) : base(message)
    {
        Errors = new List<Error> { new Error(code, message) };
    }

    public BusinessException(List<Error> errors) : base("Erro de negócio")
    {
        Errors = errors ?? new List<Error>();
    }

    public BusinessException(ValidationHandler validation) : base("Erro de validação")
    {
        Errors = validation.Errors.ToList();
    }
}