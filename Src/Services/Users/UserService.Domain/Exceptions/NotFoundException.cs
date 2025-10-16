namespace UserService.Domain.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, object id) 
        : base($"{entityName} com ID '{id}' não foi encontrado")
    {
    }

    public NotFoundException(string entityName, string propertyName, object value) 
        : base($"{entityName} com {propertyName} '{value}' não foi encontrado")
    {
    }
}