using EventBus.Abstractions;
using Shared.IntegrationEvents;
using System.Collections.Concurrent;

namespace Consumer.API.EventHandlers;

/// <summary>
/// Handler para processar eventos de pedido criado
/// </summary>
public class OrderCreatedIntegrationEventHandler : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    private readonly ILogger<OrderCreatedIntegrationEventHandler> _logger;
    private static readonly ConcurrentDictionary<Guid, ProcessedOrder> ProcessedOrders = new();

    public OrderCreatedIntegrationEventHandler(ILogger<OrderCreatedIntegrationEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(OrderCreatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "========================================================");
        _logger.LogInformation(
            "📦 Processando evento OrderCreatedIntegrationEvent");
        _logger.LogInformation(
            "   Event ID: {EventId}", @event.Id);
        _logger.LogInformation(
            "   Order ID: {OrderId}", @event.OrderId);
        _logger.LogInformation(
            "   Cliente: {CustomerName} ({CustomerEmail})",
            @event.CustomerName,
            @event.CustomerEmail);
        _logger.LogInformation(
            "   Total: R$ {TotalAmount:N2}", @event.TotalAmount);
        _logger.LogInformation(
            "   Itens: {ItemCount}", @event.Items.Count);

        foreach (var item in @event.Items)
        {
            _logger.LogInformation(
                "      - {ProductName} x {Quantity} = R$ {Total:N2}",
                item.ProductName,
                item.Quantity,
                item.Quantity * item.UnitPrice);
        }

        // Simular processamento assíncrono
        await Task.Delay(100, cancellationToken);

        // Armazenar pedido processado (em memória para demonstração)
        var processedOrder = new ProcessedOrder
        {
            OrderId = @event.OrderId,
            CustomerName = @event.CustomerName,
            CustomerEmail = @event.CustomerEmail,
            TotalAmount = @event.TotalAmount,
            ItemsCount = @event.Items.Count,
            ProcessedAt = DateTime.UtcNow,
            EventId = @event.Id
        };

        ProcessedOrders.TryAdd(@event.OrderId, processedOrder);

        _logger.LogInformation(
            "✅ Evento processado com sucesso! Pedido {OrderId} registrado.",
            @event.OrderId);
        _logger.LogInformation(
            "========================================================");
    }

    /// <summary>
    /// Obtém todos os pedidos processados (para consulta via API)
    /// </summary>
    public static IEnumerable<ProcessedOrder> GetProcessedOrders() => ProcessedOrders.Values;

    /// <summary>
    /// Obtém um pedido processado específico
    /// </summary>
    public static ProcessedOrder? GetProcessedOrder(Guid orderId) =>
        ProcessedOrders.TryGetValue(orderId, out var order) ? order : null;

    /// <summary>
    /// Obtém estatísticas dos pedidos processados
    /// </summary>
    public static ProcessingStats GetStats() => new()
    {
        TotalProcessed = ProcessedOrders.Count,
        TotalAmount = ProcessedOrders.Values.Sum(o => o.TotalAmount),
        LastProcessedAt = ProcessedOrders.Values.MaxBy(o => o.ProcessedAt)?.ProcessedAt
    };
}

public class ProcessedOrder
{
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; } = default!;
    public string CustomerEmail { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public int ItemsCount { get; set; }
    public DateTime ProcessedAt { get; set; }
    public Guid EventId { get; set; }
}

public class ProcessingStats
{
    public int TotalProcessed { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? LastProcessedAt { get; set; }
}
