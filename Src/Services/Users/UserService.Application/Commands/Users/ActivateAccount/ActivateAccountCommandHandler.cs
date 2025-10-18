using BuildingBlocks.Mediator;

namespace UserService.Application.Commands.Users.ActivateAccount;

public class ActivateAccountCommandHandler : IRequestHandler<ActivateAccountCommand, bool>
{
    public Task<bool> HandleAsync(ActivateAccountCommand request, CancellationToken cancellationToken = default)
    {
        
        throw new NotImplementedException();
    }
}