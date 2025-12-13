namespace Common.Domain.Events;

/// <summary>
/// Classe base para eventos de domínio
/// </summary>
public abstract class DomainEventBase : IDomainEvent
{
    public Guid EventId { get; }
    public DateTime OccurredOn { get; }

    protected DomainEventBase()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }
}
