using Common.Application.Interfaces;

namespace Common.Infrastructure.Services;

/// <summary>
/// Implementação do serviço de data/hora
/// </summary>
public class DateTimeService : IDateTime
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}
