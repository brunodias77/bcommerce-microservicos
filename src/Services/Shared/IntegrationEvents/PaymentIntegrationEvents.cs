using EventBus.Abstractions;

namespace Shared.IntegrationEvents;

/// <summary>
/// Evento disparado quando um Payment Intent é criado
/// </summary>
public class PaymentIntentCreatedIntegrationEvent : IntegrationEvent
{
    public string PaymentIntentId { get; }
    public Guid OrderId { get; }
    public long Amount { get; }
    public string Currency { get; }
    public string CustomerEmail { get; }

    public PaymentIntentCreatedIntegrationEvent(
        string paymentIntentId,
        Guid orderId,
        long amount,
        string currency,
        string customerEmail)
    {
        PaymentIntentId = paymentIntentId;
        OrderId = orderId;
        Amount = amount;
        Currency = currency;
        CustomerEmail = customerEmail;
    }
}

/// <summary>
/// Evento disparado quando um pagamento é bem-sucedido
/// </summary>
public class PaymentSucceededIntegrationEvent : IntegrationEvent
{
    public string PaymentIntentId { get; }
    public Guid OrderId { get; }
    public long AmountReceived { get; }
    public string Currency { get; }
    public string CustomerEmail { get; }

    public PaymentSucceededIntegrationEvent(
        string paymentIntentId,
        Guid orderId,
        long amountReceived,
        string currency,
        string customerEmail)
    {
        PaymentIntentId = paymentIntentId;
        OrderId = orderId;
        AmountReceived = amountReceived;
        Currency = currency;
        CustomerEmail = customerEmail;
    }
}

/// <summary>
/// Evento disparado quando um pagamento falha
/// </summary>
public class PaymentFailedIntegrationEvent : IntegrationEvent
{
    public string PaymentIntentId { get; }
    public Guid OrderId { get; }
    public string ErrorMessage { get; }
    public string? ErrorCode { get; }

    public PaymentFailedIntegrationEvent(
        string paymentIntentId,
        Guid orderId,
        string errorMessage,
        string? errorCode = null)
    {
        PaymentIntentId = paymentIntentId;
        OrderId = orderId;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Evento disparado quando um pagamento é cancelado
/// </summary>
public class PaymentCancelledIntegrationEvent : IntegrationEvent
{
    public string PaymentIntentId { get; }
    public Guid OrderId { get; }
    public string Reason { get; }

    public PaymentCancelledIntegrationEvent(
        string paymentIntentId,
        Guid orderId,
        string reason)
    {
        PaymentIntentId = paymentIntentId;
        OrderId = orderId;
        Reason = reason;
    }
}

/// <summary>
/// Evento disparado quando um reembolso é processado
/// </summary>
public class PaymentRefundedIntegrationEvent : IntegrationEvent
{
    public string RefundId { get; }
    public string PaymentIntentId { get; }
    public Guid OrderId { get; }
    public long AmountRefunded { get; }
    public string? Reason { get; }

    public PaymentRefundedIntegrationEvent(
        string refundId,
        string paymentIntentId,
        Guid orderId,
        long amountRefunded,
        string? reason = null)
    {
        RefundId = refundId;
        PaymentIntentId = paymentIntentId;
        OrderId = orderId;
        AmountRefunded = amountRefunded;
        Reason = reason;
    }
}
