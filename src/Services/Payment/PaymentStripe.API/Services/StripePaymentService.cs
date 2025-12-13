using Microsoft.Extensions.Options;
using PaymentStripe.API.Configuration;
using PaymentStripe.API.Models;
using Stripe;

namespace PaymentStripe.API.Services;

/// <summary>
/// Implementação do serviço de pagamentos usando Stripe
/// </summary>
public class StripePaymentService : IStripePaymentService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly CustomerService _customerService;
    private readonly RefundService _refundService;
    private readonly PaymentMethodService _paymentMethodService;

    public StripePaymentService(
        IOptions<StripeSettings> settings,
        ILogger<StripePaymentService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // Configurar API Key do Stripe
        StripeConfiguration.ApiKey = _settings.SecretKey;

        // Inicializar serviços
        _paymentIntentService = new PaymentIntentService();
        _customerService = new CustomerService();
        _refundService = new RefundService();
        _paymentMethodService = new PaymentMethodService();
    }

    public async Task<PaymentIntentResponse> CreatePaymentIntentAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Criando Payment Intent para pedido {OrderId}, valor: {Amount} centavos",
            request.OrderId,
            request.Amount);

        try
        {
            // Obter ou criar cliente
            var customer = await GetOrCreateCustomerAsync(
                request.CustomerEmail,
                request.CustomerName,
                cancellationToken);

            // Criar Payment Intent
            var options = new PaymentIntentCreateOptions
            {
                Amount = request.Amount,
                Currency = request.Currency ?? _settings.DefaultCurrency,
                Customer = customer.CustomerId,
                Description = request.Description ?? $"Pagamento do pedido {request.OrderId}",
                Metadata = new Dictionary<string, string>
                {
                    { "order_id", request.OrderId.ToString() },
                    { "customer_email", request.CustomerEmail },
                    { "customer_name", request.CustomerName }
                },
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                },
                ReceiptEmail = request.CustomerEmail
            };

            // Adicionar metadados customizados
            if (request.Metadata != null)
            {
                foreach (var kvp in request.Metadata)
                {
                    options.Metadata[kvp.Key] = kvp.Value;
                }
            }

            var paymentIntent = await _paymentIntentService.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Payment Intent criado: {PaymentIntentId} para pedido {OrderId}",
                paymentIntent.Id,
                request.OrderId);

            return new PaymentIntentResponse
            {
                PaymentIntentId = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret,
                Status = paymentIntent.Status,
                Amount = paymentIntent.Amount,
                Currency = paymentIntent.Currency,
                OrderId = request.OrderId,
                CreatedAt = paymentIntent.Created
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erro ao criar Payment Intent para pedido {OrderId}", request.OrderId);
            throw new PaymentException($"Erro ao criar pagamento: {ex.Message}", ex.StripeError?.Code, ex);
        }
    }

    public async Task<PaymentStatusResponse> ConfirmPaymentIntentAsync(
        ConfirmPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Confirmando Payment Intent: {PaymentIntentId}", request.PaymentIntentId);

        try
        {
            var options = new PaymentIntentConfirmOptions
            {
                PaymentMethod = request.PaymentMethodId
            };

            var paymentIntent = await _paymentIntentService.ConfirmAsync(
                request.PaymentIntentId,
                options,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Payment Intent {PaymentIntentId} confirmado. Status: {Status}",
                paymentIntent.Id,
                paymentIntent.Status);

            return MapToPaymentStatusResponse(paymentIntent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erro ao confirmar Payment Intent: {PaymentIntentId}", request.PaymentIntentId);
            throw new PaymentException($"Erro ao confirmar pagamento: {ex.Message}", ex.StripeError?.Code, ex);
        }
    }

    public async Task<PaymentStatusResponse> GetPaymentStatusAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Consultando status do Payment Intent: {PaymentIntentId}", paymentIntentId);

        try
        {
            var paymentIntent = await _paymentIntentService.GetAsync(
                paymentIntentId,
                cancellationToken: cancellationToken);

            return MapToPaymentStatusResponse(paymentIntent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erro ao consultar Payment Intent: {PaymentIntentId}", paymentIntentId);
            throw new PaymentException($"Erro ao consultar pagamento: {ex.Message}", ex.StripeError?.Code, ex);
        }
    }

    public async Task<PaymentStatusResponse> CancelPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelando Payment Intent: {PaymentIntentId}", paymentIntentId);

        try
        {
            var paymentIntent = await _paymentIntentService.CancelAsync(
                paymentIntentId,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Payment Intent {PaymentIntentId} cancelado. Status: {Status}",
                paymentIntent.Id,
                paymentIntent.Status);

            return MapToPaymentStatusResponse(paymentIntent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erro ao cancelar Payment Intent: {PaymentIntentId}", paymentIntentId);
            throw new PaymentException($"Erro ao cancelar pagamento: {ex.Message}", ex.StripeError?.Code, ex);
        }
    }

    public async Task<CustomerResponse> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Criando cliente no Stripe: {Email}", request.Email);

        try
        {
            var options = new CustomerCreateOptions
            {
                Email = request.Email,
                Name = request.Name,
                Phone = request.Phone
            };

            if (request.Address != null)
            {
                options.Address = new AddressOptions
                {
                    Line1 = request.Address.Line1,
                    Line2 = request.Address.Line2,
                    City = request.Address.City,
                    State = request.Address.State,
                    PostalCode = request.Address.PostalCode,
                    Country = request.Address.Country
                };
            }

            var customer = await _customerService.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Cliente criado: {CustomerId}", customer.Id);

            return new CustomerResponse
            {
                CustomerId = customer.Id,
                Email = customer.Email,
                Name = customer.Name,
                CreatedAt = customer.Created
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erro ao criar cliente: {Email}", request.Email);
            throw new PaymentException($"Erro ao criar cliente: {ex.Message}", ex.StripeError?.Code, ex);
        }
    }

    public async Task<CustomerResponse> GetOrCreateCustomerAsync(
        string email,
        string name,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando cliente por email: {Email}", email);

        try
        {
            // Buscar cliente existente
            var searchOptions = new CustomerSearchOptions
            {
                Query = $"email:'{email}'"
            };

            var existingCustomers = await _customerService.SearchAsync(searchOptions, cancellationToken: cancellationToken);

            if (existingCustomers.Data.Any())
            {
                var existing = existingCustomers.Data.First();
                _logger.LogInformation("Cliente encontrado: {CustomerId}", existing.Id);

                return new CustomerResponse
                {
                    CustomerId = existing.Id,
                    Email = existing.Email,
                    Name = existing.Name,
                    CreatedAt = existing.Created
                };
            }

            // Criar novo cliente
            return await CreateCustomerAsync(new CreateCustomerRequest
            {
                Email = email,
                Name = name
            }, cancellationToken);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erro ao buscar/criar cliente: {Email}", email);
            throw new PaymentException($"Erro ao processar cliente: {ex.Message}", ex.StripeError?.Code, ex);
        }
    }

    public async Task<RefundResponse> CreateRefundAsync(
        RefundRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Criando reembolso para Payment Intent: {PaymentIntentId}, valor: {Amount}",
            request.PaymentIntentId,
            request.Amount?.ToString() ?? "total");

        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = request.PaymentIntentId,
                Amount = request.Amount,
                Reason = request.Reason
            };

            var refund = await _refundService.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Reembolso criado: {RefundId}, status: {Status}",
                refund.Id,
                refund.Status);

            return new RefundResponse
            {
                RefundId = refund.Id,
                PaymentIntentId = request.PaymentIntentId,
                Amount = refund.Amount,
                Status = refund.Status,
                Reason = refund.Reason,
                CreatedAt = refund.Created
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erro ao criar reembolso para: {PaymentIntentId}", request.PaymentIntentId);
            throw new PaymentException($"Erro ao criar reembolso: {ex.Message}", ex.StripeError?.Code, ex);
        }
    }

    public async Task<IEnumerable<PaymentMethodResponse>> ListPaymentMethodsAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listando métodos de pagamento do cliente: {CustomerId}", customerId);

        try
        {
            var options = new PaymentMethodListOptions
            {
                Customer = customerId,
                Type = "card"
            };

            var paymentMethods = await _paymentMethodService.ListAsync(options, cancellationToken: cancellationToken);

            return paymentMethods.Data.Select(pm => new PaymentMethodResponse
            {
                PaymentMethodId = pm.Id,
                Type = pm.Type,
                Card = pm.Card != null ? new CardDetails
                {
                    Brand = pm.Card.Brand,
                    Last4 = pm.Card.Last4,
                    ExpMonth = (int)pm.Card.ExpMonth,
                    ExpYear = (int)pm.Card.ExpYear,
                    Funding = pm.Card.Funding
                } : null,
                CreatedAt = pm.Created
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erro ao listar métodos de pagamento: {CustomerId}", customerId);
            throw new PaymentException($"Erro ao listar métodos de pagamento: {ex.Message}", ex.StripeError?.Code, ex);
        }
    }

    private static PaymentStatusResponse MapToPaymentStatusResponse(PaymentIntent paymentIntent)
    {
        return new PaymentStatusResponse
        {
            PaymentIntentId = paymentIntent.Id,
            Status = paymentIntent.Status,
            Amount = paymentIntent.Amount,
            AmountReceived = paymentIntent.AmountReceived,
            Currency = paymentIntent.Currency,
            PaymentMethodType = paymentIntent.PaymentMethodTypes?.FirstOrDefault(),
            LastPaymentError = paymentIntent.LastPaymentError?.Message,
            CreatedAt = paymentIntent.Created,
            SucceededAt = paymentIntent.Status == "succeeded" ? DateTime.UtcNow : null,
            OrderId = paymentIntent.Metadata.TryGetValue("order_id", out var orderId)
                ? Guid.TryParse(orderId, out var guid) ? guid : null
                : null,
            CustomerEmail = paymentIntent.Metadata.TryGetValue("customer_email", out var email) ? email : null
        };
    }
}

/// <summary>
/// Exceção customizada para erros de pagamento
/// </summary>
public class PaymentException : Exception
{
    public string? ErrorCode { get; }

    public PaymentException(string message, string? errorCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
