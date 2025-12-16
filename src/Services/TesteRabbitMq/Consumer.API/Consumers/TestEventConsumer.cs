using MassTransit;
using TesteRabbitMq.Contracts;

namespace Consumer.API.Consumers;

public class TestEventConsumer : IConsumer<TestEvent>
{
    private readonly ILogger<TestEventConsumer> _logger;

    public TestEventConsumer(ILogger<TestEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<TestEvent> context)
    {
        _logger.LogInformation("Received event {EventId}: {Message}", context.Message.EventId, context.Message.Message);
        return Task.CompletedTask;
    }
}
