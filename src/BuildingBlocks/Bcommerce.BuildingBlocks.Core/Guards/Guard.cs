namespace Bcommerce.BuildingBlocks.Core.Guards;

public static class Guard
{
    public static void Against(bool condition, string message, string parameterName)
    {
        if (condition)
        {
            throw new ArgumentException(message, parameterName);
        }
    }
}
