using Consumer.API.EventHandlers;
using Microsoft.AspNetCore.Mvc;

namespace Consumer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcessedOrdersController : ControllerBase
{
    private readonly ILogger<ProcessedOrdersController> _logger;

    public ProcessedOrdersController(ILogger<ProcessedOrdersController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Obtém todos os pedidos processados
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProcessedOrder>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var orders = OrderCreatedIntegrationEventHandler.GetProcessedOrders()
            .OrderByDescending(o => o.ProcessedAt)
            .ToList();

        _logger.LogInformation("Retornando {Count} pedidos processados", orders.Count);

        return Ok(orders);
    }

    /// <summary>
    /// Obtém um pedido processado específico
    /// </summary>
    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(ProcessedOrder), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid orderId)
    {
        var order = OrderCreatedIntegrationEventHandler.GetProcessedOrder(orderId);

        if (order == null)
        {
            return NotFound(new { Error = $"Pedido {orderId} não encontrado" });
        }

        return Ok(order);
    }

    /// <summary>
    /// Obtém estatísticas dos pedidos processados
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ProcessingStats), StatusCodes.Status200OK)]
    public IActionResult GetStats()
    {
        var stats = OrderCreatedIntegrationEventHandler.GetStats();

        _logger.LogInformation(
            "Estatísticas: {TotalProcessed} pedidos, R$ {TotalAmount:N2} total",
            stats.TotalProcessed,
            stats.TotalAmount);

        return Ok(stats);
    }

    /// <summary>
    /// Obtém os últimos N pedidos processados
    /// </summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(IEnumerable<ProcessedOrder>), StatusCodes.Status200OK)]
    public IActionResult GetRecent([FromQuery] int count = 10)
    {
        var orders = OrderCreatedIntegrationEventHandler.GetProcessedOrders()
            .OrderByDescending(o => o.ProcessedAt)
            .Take(count)
            .ToList();

        return Ok(orders);
    }
}
