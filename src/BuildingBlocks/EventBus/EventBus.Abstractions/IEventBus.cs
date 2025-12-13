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
    /// Publica um evento de integração de forma dinâmica (usado pelo Outbox Pattern)
    /// </summary>
    /// <param name="eventType">Nome do tipo do evento</param>
    /// <param name="payload">Payload JSON serializado do evento</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task PublishDynamicAsync(string eventType, string payload, CancellationToken cancellationToken = default);

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