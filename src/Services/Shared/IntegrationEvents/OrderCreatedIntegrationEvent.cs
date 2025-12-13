using EventBus.Abstractions;

namespace Shared.IntegrationEvents;

/// <summary>
/// Evento de integração disparado quando um pedido é criado
/// </summary>
public class OrderCreatedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; }
    public string CustomerName { get; }
    public string CustomerEmail { get; }
    public decimal TotalAmount { get; }
    public List<OrderItemDto> Items { get; }

    public OrderCreatedIntegrationEvent(
        Guid orderId,
        string customerName,
        string customerEmail,
        decimal totalAmount,
        List<OrderItemDto> items)
    {
        OrderId = orderId;
        CustomerName = customerName;
        CustomerEmail = customerEmail;
        TotalAmount = totalAmount;
        Items = items;
    }
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
