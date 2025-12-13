using EventBus.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Shared.IntegrationEvents;

namespace Producer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IEventBus eventBus, ILogger<OrdersController> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// Cria um novo pedido e publica evento no RabbitMQ
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName) || string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            return BadRequest(new { Error = "Nome e email do cliente são obrigatórios" });
        }

        if (request.Items == null || !request.Items.Any())
        {
            return BadRequest(new { Error = "O pedido deve conter pelo menos um item" });
        }

        var orderId = Guid.NewGuid();
        var totalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice);

        // Criar evento de integração
        var integrationEvent = new OrderCreatedIntegrationEvent(
            orderId,
            request.CustomerName,
            request.CustomerEmail,
            totalAmount,
            request.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList());

        _logger.LogInformation(
            "Publicando evento OrderCreatedIntegrationEvent para pedido {OrderId}",
            orderId);

        // Publicar no RabbitMQ
        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogInformation(
            "Evento OrderCreatedIntegrationEvent publicado com sucesso para pedido {OrderId}",
            orderId);

        return CreatedAtAction(
            nameof(GetOrder),
            new { orderId },
            new OrderCreatedResponse
            {
                OrderId = orderId,
                Message = "Pedido criado e evento publicado com sucesso",
                TotalAmount = totalAmount
            });
    }

    /// <summary>
    /// Obtém informações de um pedido (simulado)
    /// </summary>
    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public IActionResult GetOrder(Guid orderId)
    {
        // Simulação - em produção buscaria do banco de dados
        return Ok(new OrderResponse
        {
            OrderId = orderId,
            Status = "Processing",
            Message = "Pedido em processamento"
        });
    }

    /// <summary>
    /// Publica múltiplos eventos de teste
    /// </summary>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(BulkResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateBulkOrders([FromQuery] int count = 10, CancellationToken cancellationToken = default)
    {
        var orders = new List<Guid>();
        var random = new Random();

        for (int i = 0; i < count; i++)
        {
            var orderId = Guid.NewGuid();
            var integrationEvent = new OrderCreatedIntegrationEvent(
                orderId,
                $"Cliente {i + 1}",
                $"cliente{i + 1}@email.com",
                random.Next(100, 10000),
                new List<OrderItemDto>
                {
                    new()
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = $"Produto {random.Next(1, 100)}",
                        Quantity = random.Next(1, 5),
                        UnitPrice = random.Next(10, 500)
                    }
                });

            await _eventBus.PublishAsync(integrationEvent, cancellationToken);
            orders.Add(orderId);
        }

        _logger.LogInformation("{Count} eventos publicados com sucesso", count);

        return Ok(new BulkResponse
        {
            Count = count,
            OrderIds = orders,
            Message = $"{count} pedidos criados e eventos publicados"
        });
    }
}

#region Request/Response Models

public class CreateOrderRequest
{
    public string CustomerName { get; set; } = default!;
    public string CustomerEmail { get; set; } = default!;
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class OrderCreatedResponse
{
    public Guid OrderId { get; set; }
    public string Message { get; set; } = default!;
    public decimal TotalAmount { get; set; }
}

public class OrderResponse
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = default!;
    public string Message { get; set; } = default!;
}

public class BulkResponse
{
    public int Count { get; set; }
    public List<Guid> OrderIds { get; set; } = new();
    public string Message { get; set; } = default!;
}

#endregion
