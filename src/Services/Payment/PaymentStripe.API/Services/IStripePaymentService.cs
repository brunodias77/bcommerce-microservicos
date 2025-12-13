using PaymentStripe.API.Models;

namespace PaymentStripe.API.Services;

/// <summary>
/// Interface do serviço de pagamentos Stripe
/// </summary>
public interface IStripePaymentService
{
    /// <summary>
    /// Cria um Payment Intent
    /// </summary>
    Task<PaymentIntentResponse> CreatePaymentIntentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirma um Payment Intent
    /// </summary>
    Task<PaymentStatusResponse> ConfirmPaymentIntentAsync(ConfirmPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o status de um Payment Intent
    /// </summary>
    Task<PaymentStatusResponse> GetPaymentStatusAsync(string paymentIntentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancela um Payment Intent
    /// </summary>
    Task<PaymentStatusResponse> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cria um cliente no Stripe
    /// </summary>
    Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém ou cria um cliente pelo email
    /// </summary>
    Task<CustomerResponse> GetOrCreateCustomerAsync(string email, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processa reembolso
    /// </summary>
    Task<RefundResponse> CreateRefundAsync(RefundRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista métodos de pagamento de um cliente
    /// </summary>
    Task<IEnumerable<PaymentMethodResponse>> ListPaymentMethodsAsync(string customerId, CancellationToken cancellationToken = default);
}
