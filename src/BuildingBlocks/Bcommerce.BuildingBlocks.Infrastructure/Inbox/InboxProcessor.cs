using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bcommerce.BuildingBlocks.Infrastructure.Inbox;

public class InboxProcessor
{
    private readonly IInboxMessageRepository _inboxRepository;
    private readonly IPublisher _publisher;
    private readonly ILogger<InboxProcessor> _logger;

    public InboxProcessor(IInboxMessageRepository inboxRepository, IPublisher publisher, ILogger<InboxProcessor> logger)
    {
        _inboxRepository = inboxRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        var messages = await _inboxRepository.GetUnprocessedMessagesAsync(20);

        foreach (var message in messages)
        {
            try
            {
                // Assuming request type is in Type property or embedded. For simplicity, treating as INotification or IRequest
                // Warning: Deserializing without strict type control can be risky.
                var type = Type.GetType(message.Type);
                if (type != null)
                {
                    var request = JsonConvert.DeserializeObject(message.Content, type);

                    if (request != null)
                    {
                        await _publisher.Publish(request, cancellationToken);
                    }
                }
                else
                {
                    _logger.LogWarning("Tipo da mensagem não encontrado: {MessageType}", message.Type);
                }

                message.ProcessedOnUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                _logger.LogError(ex, "Erro ao processar mensagem do inbox: {MessageId}", message.Id);
            }
            
            await _inboxRepository.UpdateAsync(message);
        }
    }
}
