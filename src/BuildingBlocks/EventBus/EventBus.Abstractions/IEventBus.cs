namespace EventBus.Abstractions;

/// <summary>
/// Interface para o Event Bus (mensageria entre serviços)
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publica um evento de integração
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;

    /// <summary>
    /// Se inscreve para receber eventos de um tipo específico
    /// </summary>
    void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>;

    /// <summary>
    /// Remove inscrição de um tipo de evento
    /// </summary>
    void Unsubscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>;
}