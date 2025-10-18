using BuildingBlocks.Mediator;
using Microsoft.Extensions.Logging;
using UserService.Infrastructure.Contracts;

namespace UserService.Application.Events.Users;

/// <summary>
/// Handler responsável por processar o evento de criação de usuário
/// Envia email de ativação quando um usuário é criado
/// </summary>
public class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(IEmailService emailService, ILogger<UserCreatedEventHandler> logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(UserCreatedEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processando evento de criação de usuário para: {Email}", notification.Email);

        try
        {
            // Envia email de ativação
            var result = await _emailService.SendAccountActivationEmailAsync(
                notification.Email,
                notification.FirstName,
                notification.ActivationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Email de ativação enviado com sucesso para: {Email}", notification.Email);
            }
            else
            {
                _logger.LogError("Falha ao enviar email de ativação para {Email}: {Error}", 
                    notification.Email, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar evento de criação de usuário para: {Email}", 
                notification.Email);
        }
    }
}