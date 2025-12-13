namespace EventBus.Abstractions;

/// <summary>
/// Gerenciador de inscrições do Event Bus
/// </summary>
public interface IEventBusSubscriptionsManager
{
    bool IsEmpty { get; }
    event EventHandler<string> OnEventRemoved;

    void AddSubscription<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>;

    void RemoveSubscription<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>;

    bool HasSubscriptionsForEvent(string eventName);
    Type? GetEventTypeByName(string eventName);
    IEnumerable<Type> GetHandlersForEvent(string eventName);
    string GetEventKey<T>();
    void Clear();
}