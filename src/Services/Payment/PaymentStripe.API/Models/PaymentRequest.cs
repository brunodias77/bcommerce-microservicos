using System.ComponentModel.DataAnnotations;

namespace PaymentStripe.API.Models;

/// <summary>
/// Request para criar um Payment Intent
/// </summary>
public class CreatePaymentRequest
{
    /// <summary>
    /// ID do pedido associado ao pagamento
    /// </summary>
    [Required]
    public Guid OrderId { get; set; }

    /// <summary>
    /// Valor do pagamento em centavos (ex: R$ 100,00 = 10000)
    /// </summary>
    [Required]
    [Range(50, 99999999, ErrorMessage = "Valor mínimo é 50 centavos")]
    public long Amount { get; set; }

    /// <summary>
    /// Moeda (padrão: brl)
    /// </summary>
    public string Currency { get; set; } = "brl";

    /// <summary>
    /// Descrição do pagamento
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Email do cliente
    /// </summary>
    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; } = default!;

    /// <summary>
    /// Nome do cliente
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = default!;

    /// <summary>
    /// Metadados adicionais
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Request para confirmar um pagamento
/// </summary>
public class ConfirmPaymentRequest
{
    /// <summary>
    /// ID do Payment Intent do Stripe
    /// </summary>
    [Required]
    public string PaymentIntentId { get; set; } = default!;

    /// <summary>
    /// ID do método de pagamento (cartão tokenizado)
    /// </summary>
    [Required]
    public string PaymentMethodId { get; set; } = default!;
}

/// <summary>
/// Request para criar um cliente no Stripe
/// </summary>
public class CreateCustomerRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = default!;

    [MaxLength(20)]
    public string? Phone { get; set; }

    public AddressRequest? Address { get; set; }
}

public class AddressRequest
{
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "BR";
}

/// <summary>
/// Request para reembolso
/// </summary>
public class RefundRequest
{
    [Required]
    public string PaymentIntentId { get; set; } = default!;

    /// <summary>
    /// Valor do reembolso em centavos (null = reembolso total)
    /// </summary>
    public long? Amount { get; set; }

    /// <summary>
    /// Motivo do reembolso
    /// </summary>
    public string? Reason { get; set; }
}
