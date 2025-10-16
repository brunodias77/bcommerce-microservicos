using BuildingBlocks.Mediator;
using BuildingBlocks.Results;

namespace UserService.Application.Commands.Users.Create;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, ApiResponse<Guid>>
{
    public Task<ApiResponse<Guid>> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        return null;
    }
}