namespace Common.Application.Exceptions;

public class BusinessRuleException : Exception
{
    public string Code { get; }

    public BusinessRuleException(string message, string code = "REGRA_DE_NEGOCIO_VIOLADA")
        : base(message)
    {
        Code = code;
    }
}
