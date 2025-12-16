namespace Bcommerce.BuildingBlocks.Core.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key)
        : base($"A entidade \"{name}\" ({key}) não foi encontrada.")
    {
    }
}
