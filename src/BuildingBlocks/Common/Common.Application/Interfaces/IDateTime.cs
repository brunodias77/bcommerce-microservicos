namespace Common.Application.Interfaces;

/// <summary>
/// Interface para abstrair DateTime (facilita testes)
/// </summary>
public interface IDateTime
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}
