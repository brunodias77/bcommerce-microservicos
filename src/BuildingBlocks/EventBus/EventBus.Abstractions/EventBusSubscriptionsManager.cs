namespace EventBus.Abstractions;

/// <summary>
/// Implementação do gerenciador de inscrições
/// </summary>
public class EventBusSubscriptionsManager : IEventBusSubscriptionsManager
{
    private readonly Dictionary<string, List<Type>> _handlers = new();
    private readonly List<Type> _eventTypes = new();

    public bool IsEmpty => _handlers.Count == 0;
    public event EventHandler<string>? OnEventRemoved;

    public void AddSubscription<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        var eventName = GetEventKey<TEvent>();
        var handlerType = typeof(THandler);

        if (!HasSubscriptionsForEvent(eventName))
        {
            _handlers.Add(eventName, new List<Type>());
        }

        if (_handlers[eventName].Contains(handlerType))
        {
            throw new ArgumentException(
                $"Handler Type {handlerType.Name} already registered for '{eventName}'",
                nameof(handlerType));
        }

        _handlers[eventName].Add(handlerType);

        if (!_eventTypes.Contains(typeof(TEvent)))
        {
            _eventTypes.Add(typeof(TEvent));
        }
    }

    public void RemoveSubscription<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        var eventName = GetEventKey<TEvent>();
        var handlerType = typeof(THandler);

        if (_handlers.TryGetValue(eventName, out var handlers))
        {
            handlers.Remove(handlerType);

            if (handlers.Count == 0)
            {
                _handlers.Remove(eventName);
                var eventType = _eventTypes.SingleOrDefault(e => e.Name == eventName);
                if (eventType != null)
                {
                    _eventTypes.Remove(eventType);
                }

                OnEventRemoved?.Invoke(this, eventName);
            }
        }
    }

    public bool HasSubscriptionsForEvent(string eventName)
    {
        return _handlers.ContainsKey(eventName);
    }

    public Type? GetEventTypeByName(string eventName)
    {
        return _eventTypes.SingleOrDefault(t => t.Name == eventName);
    }

    public IEnumerable<Type> GetHandlersForEvent(string eventName)
    {
        return _handlers.TryGetValue(eventName, out var handlers) 
            ? handlers 
            : Enumerable.Empty<Type>();
    }

    public string GetEventKey<T>()
    {
        return typeof(T).Name;
    }

    public void Clear()
    {
        _handlers.Clear();
        _eventTypes.Clear();
    }
}
