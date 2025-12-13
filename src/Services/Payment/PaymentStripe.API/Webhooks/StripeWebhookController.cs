using EventBus.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PaymentStripe.API.Configuration;
using Shared.IntegrationEvents;
using Stripe;

namespace PaymentStripe.API.Webhooks;

[ApiController]
[Route("api/webhooks")]
public class StripeWebhookController : ControllerBase
{
    private readonly StripeSettings _settings;
    private readonly IEventBus _eventBus;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        IOptions<StripeSettings> settings,
        IEventBus eventBus,
        ILogger<StripeWebhookController> logger)
    {
        _settings = settings.Value;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint para receber webhooks do Stripe
    /// </summary>
    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook(CancellationToken cancellationToken)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(cancellationToken);

        try
        {
            // Validar assinatura do webhook
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _settings.WebhookSecret);

            _logger.LogInformation(
                "Webhook recebido: {EventType}, ID: {EventId}",
                stripeEvent.Type,
                stripeEvent.Id);

            // Processar evento baseado no tipo
            await ProcessStripeEvent(stripeEvent, cancellationToken);

            return Ok(new { received = true });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erro de assinatura do webhook Stripe");
            return BadRequest(new { error = "Assinatura do webhook inválida" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar webhook Stripe");
            return StatusCode(500, new { error = "Erro interno ao processar webhook" });
        }
    }

    private async Task ProcessStripeEvent(Event stripeEvent, CancellationToken cancellationToken)
    {
        switch (stripeEvent.Type)
        {
            case Events.PaymentIntentSucceeded:
                await HandlePaymentIntentSucceeded(stripeEvent, cancellationToken);
                break;

            case Events.PaymentIntentPaymentFailed:
                await HandlePaymentIntentFailed(stripeEvent, cancellationToken);
                break;

            case Events.PaymentIntentCanceled:
                await HandlePaymentIntentCanceled(stripeEvent, cancellationToken);
                break;

            case Events.ChargeRefunded:
                await HandleChargeRefunded(stripeEvent, cancellationToken);
                break;

            case Events.PaymentIntentCreated:
                _logger.LogInformation("Payment Intent criado via webhook");
                break;

            case Events.PaymentIntentProcessing:
                _logger.LogInformation("Payment Intent em processamento");
                break;

            default:
                _logger.LogInformation("Evento não tratado: {EventType}", stripeEvent.Type);
                break;
        }
    }

    private async Task HandlePaymentIntentSucceeded(Event stripeEvent, CancellationToken cancellationToken)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        _logger.LogInformation(
            "💰 Pagamento bem-sucedido: {PaymentIntentId}, Valor: {Amount}",
            paymentIntent.Id,
            paymentIntent.AmountReceived);

        // Extrair OrderId dos metadados
        Guid? orderId = null;
        if (paymentIntent.Metadata.TryGetValue("order_id", out var orderIdStr))
        {
            Guid.TryParse(orderIdStr, out var parsed);
            orderId = parsed;
        }

        var customerEmail = paymentIntent.Metadata.GetValueOrDefault("customer_email", paymentIntent.ReceiptEmail);

        var integrationEvent = new PaymentSucceededIntegrationEvent(
            paymentIntent.Id,
            orderId ?? Guid.Empty,
            paymentIntent.AmountReceived,
            paymentIntent.Currency,
            customerEmail ?? string.Empty);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogInformation(
            "Evento PaymentSucceededIntegrationEvent publicado para pedido {OrderId}",
            orderId);
    }

    private async Task HandlePaymentIntentFailed(Event stripeEvent, CancellationToken cancellationToken)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        _logger.LogWarning(
            "❌ Pagamento falhou: {PaymentIntentId}, Erro: {Error}",
            paymentIntent.Id,
            paymentIntent.LastPaymentError?.Message);

        Guid? orderId = null;
        if (paymentIntent.Metadata.TryGetValue("order_id", out var orderIdStr))
        {
            Guid.TryParse(orderIdStr, out var parsed);
            orderId = parsed;
        }

        var integrationEvent = new PaymentFailedIntegrationEvent(
            paymentIntent.Id,
            orderId ?? Guid.Empty,
            paymentIntent.LastPaymentError?.Message ?? "Falha no pagamento",
            paymentIntent.LastPaymentError?.Code);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogInformation(
            "Evento PaymentFailedIntegrationEvent publicado para pedido {OrderId}",
            orderId);
    }

    private async Task HandlePaymentIntentCanceled(Event stripeEvent, CancellationToken cancellationToken)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        _logger.LogInformation(
            "🚫 Pagamento cancelado: {PaymentIntentId}",
            paymentIntent.Id);

        Guid? orderId = null;
        if (paymentIntent.Metadata.TryGetValue("order_id", out var orderIdStr))
        {
            Guid.TryParse(orderIdStr, out var parsed);
            orderId = parsed;
        }

        var integrationEvent = new PaymentCancelledIntegrationEvent(
            paymentIntent.Id,
            orderId ?? Guid.Empty,
            paymentIntent.CancellationReason ?? "Cancelado");

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);
    }

    private async Task HandleChargeRefunded(Event stripeEvent, CancellationToken cancellationToken)
    {
        var charge = stripeEvent.Data.Object as Charge;
        if (charge == null) return;

        _logger.LogInformation(
            "💸 Reembolso processado: {ChargeId}, Valor: {Amount}",
            charge.Id,
            charge.AmountRefunded);

        Guid? orderId = null;
        if (charge.Metadata.TryGetValue("order_id", out var orderIdStr))
        {
            Guid.TryParse(orderIdStr, out var parsed);
            orderId = parsed;
        }

        var integrationEvent = new PaymentRefundedIntegrationEvent(
            charge.Id,
            charge.PaymentIntentId,
            orderId ?? Guid.Empty,
            charge.AmountRefunded,
            charge.RefundedReason);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);
    }
}
