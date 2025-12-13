namespace PaymentStripe.API.Models;

/// <summary>
/// Response após criar um Payment Intent
/// </summary>
public class PaymentIntentResponse
{
    /// <summary>
    /// ID do Payment Intent do Stripe
    /// </summary>
    public string PaymentIntentId { get; set; } = default!;

    /// <summary>
    /// Client Secret para uso no frontend
    /// </summary>
    public string ClientSecret { get; set; } = default!;

    /// <summary>
    /// Status atual do pagamento
    /// </summary>
    public string Status { get; set; } = default!;

    /// <summary>
    /// Valor em centavos
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Moeda
    /// </summary>
    public string Currency { get; set; } = default!;

    /// <summary>
    /// ID do pedido associado
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Data de criação
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response com status do pagamento
/// </summary>
public class PaymentStatusResponse
{
    public string PaymentIntentId { get; set; } = default!;
    public string Status { get; set; } = default!;
    public long Amount { get; set; }
    public long? AmountReceived { get; set; }
    public string Currency { get; set; } = default!;
    public string? PaymentMethodType { get; set; }
    public string? LastPaymentError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SucceededAt { get; set; }
    public Guid? OrderId { get; set; }
    public string? CustomerEmail { get; set; }
}

/// <summary>
/// Response após criar cliente
/// </summary>
public class CustomerResponse
{
    public string CustomerId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response de reembolso
/// </summary>
public class RefundResponse
{
    public string RefundId { get; set; } = default!;
    public string PaymentIntentId { get; set; } = default!;
    public long Amount { get; set; }
    public string Status { get; set; } = default!;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response de erro padronizado
/// </summary>
public class ErrorResponse
{
    public string Error { get; set; } = default!;
    public string? Code { get; set; }
    public string? DeclineCode { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}

/// <summary>
/// Response com lista de métodos de pagamento
/// </summary>
public class PaymentMethodResponse
{
    public string PaymentMethodId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public CardDetails? Card { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CardDetails
{
    public string Brand { get; set; } = default!;
    public string Last4 { get; set; } = default!;
    public int ExpMonth { get; set; }
    public int ExpYear { get; set; }
    public string? Funding { get; set; }
}
