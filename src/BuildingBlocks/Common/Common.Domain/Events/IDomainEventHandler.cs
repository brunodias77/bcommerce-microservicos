using MediatR;

namespace Common.Domain.Events;

/// <summary>
/// Interface para handlers de eventos de domínio
/// </summary>
public interface IDomainEventHandler<in TDomainEvent> : INotificationHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
}