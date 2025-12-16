using MediatR;

namespace Bcommerce.BuildingBlocks.Core.Application;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}
