using Microsoft.AspNetCore.Mvc;
using PaymentStripe.API.Models;
using PaymentStripe.API.Services;

namespace PaymentStripe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private readonly IStripePaymentService _paymentService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        IStripePaymentService paymentService,
        ILogger<CustomersController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Cria um novo cliente no Stripe
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.CreateCustomerAsync(request, cancellationToken);

            _logger.LogInformation("Cliente criado: {CustomerId}", result.CustomerId);

            return CreatedAtAction(
                nameof(GetCustomerPaymentMethods),
                new { customerId = result.CustomerId },
                result);
        }
        catch (PaymentException ex)
        {
            _logger.LogError(ex, "Erro ao criar cliente: {Email}", request.Email);
            return BadRequest(new ErrorResponse
            {
                Error = ex.Message,
                Code = ex.ErrorCode
            });
        }
    }

    /// <summary>
    /// Lista os métodos de pagamento de um cliente
    /// </summary>
    [HttpGet("{customerId}/payment-methods")]
    [ProducesResponseType(typeof(IEnumerable<PaymentMethodResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCustomerPaymentMethods(
        string customerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.ListPaymentMethodsAsync(customerId, cancellationToken);
            return Ok(result);
        }
        catch (PaymentException ex)
        {
            _logger.LogError(ex, "Erro ao listar métodos de pagamento: {CustomerId}", customerId);
            return BadRequest(new ErrorResponse
            {
                Error = ex.Message,
                Code = ex.ErrorCode
            });
        }
    }
}
