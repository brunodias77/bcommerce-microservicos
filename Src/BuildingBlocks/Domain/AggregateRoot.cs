using BuildingBlocks.Mediator;
using BuildingBlocks.Validations;

namespace BuildingBlocks.Domain;

public abstract class AggregateRoot : Entity
{
    /// <summary>
    /// Publica todos os domain events pendentes através do mediator
    /// </summary>
    /// <param name="mediator">Instância do mediator para publicação dos eventos</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Task representando a operação assíncrona</returns>
    public async Task PublishDomainEventsAsync(IMediator mediator, CancellationToken cancellationToken = default)
    {
        if (mediator == null)
            throw new ArgumentNullException(nameof(mediator));

        // Validacoes
        var events = DomainEvents.ToList();

        ClearDomainEvents();

        foreach (var domainEvent in events)
        {
            await mediator.PublishAsync(domainEvent, cancellationToken);
        }
    }
}