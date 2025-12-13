namespace EventBus.Abstractions;

/// <summary>
/// Interface para handlers de Integration Events
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}