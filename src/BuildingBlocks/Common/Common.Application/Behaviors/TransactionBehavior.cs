using Common.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Common.Application.Behaviors;

/// <summary>
/// Pipeline behavior para gerenciamento de transações
/// </summary>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(
        ILogger<TransactionBehavior<TRequest, TResponse>> logger,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        // Apenas aplica transação para Commands (não para Queries)
        if (!requestName.EndsWith("Command"))
        {
            return await next();
        }

        _logger.LogInformation(
            "Begin transaction for {RequestName}",
            requestName);

        try
        {
            var response = await next();

            await _unitOfWork.SaveEntitiesAsync(cancellationToken);

            _logger.LogInformation(
                "Committed transaction for {RequestName}",
                requestName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Transaction failed for {RequestName}",
                requestName);

            throw;
        }
    }
}