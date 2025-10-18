using BuildingBlocks.Mediator;

namespace UserService.Application.Events.Users;

/// <summary>
/// Evento disparado quando um usuário é criado com sucesso
/// Usado para enviar email de ativação
/// </summary>
public record UserCreatedEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string ActivationToken
) : INotification;