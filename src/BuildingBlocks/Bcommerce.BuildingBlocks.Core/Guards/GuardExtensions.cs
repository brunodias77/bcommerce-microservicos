using System.Runtime.CompilerServices;

namespace Bcommerce.BuildingBlocks.Core.Guards;

public static class GuardExtensions
{
    public static void Null(this object? input, string parameterName, string? message = null)
    {
        if (input is null)
        {
            throw new ArgumentNullException(parameterName, message ?? $"A entrada obrigatória {parameterName} estava nula.");
        }
    }

    public static void NullOrEmpty(this string? input, string parameterName, string? message = null)
    {
        GuardExtensions.Null(input, parameterName, message);
        if (input == string.Empty)
        {
            throw new ArgumentException(message ?? $"A entrada obrigatória {parameterName} estava vazia.", parameterName);
        }
    }

    public static void NullOrWhiteSpace(this string? input, string parameterName, string? message = null)
    {
        GuardExtensions.Null(input, parameterName, message);
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException(message ?? $"A entrada obrigatória {parameterName} estava vazia.", parameterName);
        }
    }
}
