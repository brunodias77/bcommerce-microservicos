namespace EventBus.Abstractions;

/// <summary>
/// Classe base para Integration Events (eventos entre serviços)
/// </summary>
public abstract class IntegrationEvent
{
    public Guid Id { get; }
    public DateTime OccurredOn { get; }
    public string EventType { get; }

    protected IntegrationEvent()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        EventType = GetType().Name;
    }

    protected IntegrationEvent(Guid id, DateTime occurredOn)
    {
        Id = id;
        OccurredOn = occurredOn;
        EventType = GetType().Name;
    }
}