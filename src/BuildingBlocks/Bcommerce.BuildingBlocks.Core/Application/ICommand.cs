using MediatR;

namespace Bcommerce.BuildingBlocks.Core.Application;

public interface ICommand<TResponse> : IRequest<TResponse>
{
}

public interface ICommand : IRequest<Unit>
{
}
