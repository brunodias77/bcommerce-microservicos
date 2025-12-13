using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Common.Application.Interfaces;

namespace Common.Application.Behaviors;

/// <summary>
/// Pipeline behavior para monitoramento de performance
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly Stopwatch _timer;
    private readonly ILogger<TRequest> _logger;
    private readonly ICurrentUser _currentUser;

    public PerformanceBehavior(
        ILogger<TRequest> logger,
        ICurrentUser currentUser)
    {
        _timer = new Stopwatch();
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next();

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500)
        {
            var requestName = typeof(TRequest).Name;
            var userId = _currentUser.UserId?.ToString() ?? "Anonymous";
            var userName = _currentUser.UserName ?? "Anonymous";

            _logger.LogWarning(
                "Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@UserId} {@UserName} {@Request}",
                requestName,
                elapsedMilliseconds,
                userId,
                userName,
                request);
        }

        return response;
    }
}
