using MediatR;

namespace Common.Domain.Events;

/// <summary>
/// Interface base para todos os eventos de domínio
/// Usa MediatR para publicação e tratamento
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// ID único do evento
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Timestamp de quando o evento ocorreu
    /// </summary>
    DateTime OccurredOn { get; }
}



