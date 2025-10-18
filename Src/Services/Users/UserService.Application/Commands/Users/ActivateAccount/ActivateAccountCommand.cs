using BuildingBlocks.Mediator;

namespace UserService.Application.Commands.Users.ActivateAccount;

public record ActivateAccountCommand(string code) : IRequest<bool>;
