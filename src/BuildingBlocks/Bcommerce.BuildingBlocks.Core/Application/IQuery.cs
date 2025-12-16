using MediatR;

namespace Bcommerce.BuildingBlocks.Core.Application;

public interface IQuery<TResponse> : IRequest<TResponse>
{
}
