using EventBus.Abstractions;
using Microsoft.AspNetCore.Mvc;
using PaymentStripe.API.Models;
using PaymentStripe.API.Services;
using Shared.IntegrationEvents;

namespace PaymentStripe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IStripePaymentService _paymentService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IStripePaymentService paymentService,
        IEventBus eventBus,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// Cria um Payment Intent para iniciar o processo de pagamento
    /// </summary>
    /// <remarks>
    /// O client_secret retornado deve ser usado no frontend para completar o pagamento
    /// usando o Stripe.js ou Stripe Elements.
    /// </remarks>
    [HttpPost("create-payment-intent")]
    [ProducesResponseType(typeof(PaymentIntentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePaymentIntent(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.CreatePaymentIntentAsync(request, cancellationToken);

            // Publicar evento de Payment Intent criado
            var integrationEvent = new PaymentIntentCreatedIntegrationEvent(
                result.PaymentIntentId,
                request.OrderId,
                result.Amount,
                result.Currency,
                request.CustomerEmail);

            await _eventBus.PublishAsync(integrationEvent, cancellationToken);

            _logger.LogInformation(
                "Payment Intent {PaymentIntentId} criado para pedido {OrderId}",
                result.PaymentIntentId,
                request.OrderId);

            return CreatedAtAction(
                nameof(GetPaymentStatus),
                new { paymentIntentId = result.PaymentIntentId },
                result);
        }
        catch (PaymentException ex)
        {
            _logger.LogError(ex, "Erro ao criar Payment Intent para pedido {OrderId}", request.OrderId);
            return BadRequest(new ErrorResponse
            {
                Error = ex.Message,
                Code = ex.ErrorCode
            });
        }
    }

    /// <summary>
    /// Confirma um Payment Intent com o método de pagamento
    /// </summary>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(PaymentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmPayment(
        [FromBody] ConfirmPaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.ConfirmPaymentIntentAsync(request, cancellationToken);

            if (result.Status == "succeeded")
            {
                var integrationEvent = new PaymentSucceededIntegrationEvent(
                    result.PaymentIntentId,
                    result.OrderId ?? Guid.Empty,
                    result.AmountReceived ?? result.Amount,
                    result.Currency,
                    result.CustomerEmail ?? string.Empty);

                await _eventBus.PublishAsync(integrationEvent, cancellationToken);

                _logger.LogInformation(
                    "Pagamento {PaymentIntentId} confirmado com sucesso",
                    result.PaymentIntentId);
            }

            return Ok(result);
        }
        catch (PaymentException ex)
        {
            _logger.LogError(ex, "Erro ao confirmar pagamento: {PaymentIntentId}", request.PaymentIntentId);
            return BadRequest(new ErrorResponse
            {
                Error = ex.Message,
                Code = ex.ErrorCode
            });
        }
    }

    /// <summary>
    /// Obtém o status de um Payment Intent
    /// </summary>
    [HttpGet("{paymentIntentId}")]
    [ProducesResponseType(typeof(PaymentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatus(
        string paymentIntentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.GetPaymentStatusAsync(paymentIntentId, cancellationToken);
            return Ok(result);
        }
        catch (PaymentException ex)
        {
            _logger.LogError(ex, "Erro ao consultar pagamento: {PaymentIntentId}", paymentIntentId);
            return NotFound(new ErrorResponse
            {
                Error = ex.Message,
                Code = ex.ErrorCode
            });
        }
    }

    /// <summary>
    /// Cancela um Payment Intent
    /// </summary>
    [HttpPost("{paymentIntentId}/cancel")]
    [ProducesResponseType(typeof(PaymentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelPayment(
        string paymentIntentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.CancelPaymentIntentAsync(paymentIntentId, cancellationToken);

            var integrationEvent = new PaymentCancelledIntegrationEvent(
                result.PaymentIntentId,
                result.OrderId ?? Guid.Empty,
                "Cancelado pelo usuário");

            await _eventBus.PublishAsync(integrationEvent, cancellationToken);

            _logger.LogInformation("Pagamento {PaymentIntentId} cancelado", paymentIntentId);

            return Ok(result);
        }
        catch (PaymentException ex)
        {
            _logger.LogError(ex, "Erro ao cancelar pagamento: {PaymentIntentId}", paymentIntentId);
            return BadRequest(new ErrorResponse
            {
                Error = ex.Message,
                Code = ex.ErrorCode
            });
        }
    }

    /// <summary>
    /// Cria um reembolso
    /// </summary>
    [HttpPost("refund")]
    [ProducesResponseType(typeof(RefundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRefund(
        [FromBody] RefundRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.CreateRefundAsync(request, cancellationToken);

            // Buscar detalhes do pagamento para o evento
            var payment = await _paymentService.GetPaymentStatusAsync(request.PaymentIntentId, cancellationToken);

            var integrationEvent = new PaymentRefundedIntegrationEvent(
                result.RefundId,
                result.PaymentIntentId,
                payment.OrderId ?? Guid.Empty,
                result.Amount,
                result.Reason);

            await _eventBus.PublishAsync(integrationEvent, cancellationToken);

            _logger.LogInformation(
                "Reembolso {RefundId} criado para pagamento {PaymentIntentId}",
                result.RefundId,
                request.PaymentIntentId);

            return Ok(result);
        }
        catch (PaymentException ex)
        {
            _logger.LogError(ex, "Erro ao criar reembolso: {PaymentIntentId}", request.PaymentIntentId);
            return BadRequest(new ErrorResponse
            {
                Error = ex.Message,
                Code = ex.ErrorCode
            });
        }
    }
}
