using MassTransit;
using Microsoft.Extensions.Logging;

namespace Bcommerce.BuildingBlocks.Messaging.MassTransit.Filters;

public class IdempotencyFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    private readonly ILogger<IdempotencyFilter<T>> _logger;

    public IdempotencyFilter(ILogger<IdempotencyFilter<T>> logger)
    {
        _logger = logger;
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        // TODO: Implementar verificação real de idempotência (Redis/Banco)
        // Por enquanto, apenas loga.
        
        var messageId = context.MessageId;
        _logger.LogInformation("Verificando idempotência para a mensagem {MessageId}", messageId);

        await next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("idempotency");
    }
}
