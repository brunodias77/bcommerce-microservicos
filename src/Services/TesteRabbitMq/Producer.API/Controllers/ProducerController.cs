using Bcommerce.BuildingBlocks.Messaging.Abstractions;
using Microsoft.AspNetCore.Mvc;
using TesteRabbitMq.Contracts;

namespace Producer.API.Controllers;

[ApiController]
[Route("[controller]")]
public class ProducerController : ControllerBase
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ProducerController> _logger;

    public ProducerController(IEventBus eventBus, ILogger<ProducerController> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> PublishMessage([FromBody] string message)
    {
        var eventId = Guid.NewGuid();
        var testEvent = new TestEvent(eventId, DateTime.UtcNow, message);

        _logger.LogInformation("Publishing event {EventId} with message: {Message}", eventId, message);

        await _eventBus.PublishAsync(testEvent);

        return Ok(new { EventId = eventId, Message = message, Status = "Published" });
    }
}
