using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Common.Application.Behaviors;

/// <summary>
/// Pipeline behavior para logging de requests e performance
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Processando {RequestName} {@Request}",
            requestName,
            request);

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation(
                "Processado {RequestName} em {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            if (stopwatch.ElapsedMilliseconds > 3000)
            {
                _logger.LogWarning(
                    "Requisição de longa duração: {RequestName} ({ElapsedMilliseconds}ms)",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Requisição {RequestName} falhou após {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
