using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Microsoft.Extensions.Logging;

namespace Common.Infrastructure.Resilience;

/// <summary>
/// Políticas de resiliência centralizadas usando Polly
/// </summary>
public static class ResiliencePolicies
{
    // ========================================
    // RETRY POLICIES
    // ========================================

    /// <summary>
    /// Política de retry exponencial para HTTP
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy()
    {
        return Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(msg => (int)msg.StatusCode >= 500 || (int)msg.StatusCode == 429)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) +
                    TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)), // Jitter
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning(
                        "Request failed. Waiting {Delay}ms before retry {Retry}. Reason: {Reason}",
                        timespan.TotalMilliseconds,
                        retryCount,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()
                    );
                });
    }

    /// <summary>
    /// Política de retry para operações de banco de dados
    /// </summary>
    public static AsyncRetryPolicy GetDatabaseRetryPolicy()
    {
        return Policy
            .Handle<Exception>(ex => IsTransientError(ex))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning(
                        "Database operation failed. Retry {Retry} after {Delay}ms. Error: {Error}",
                        retryCount,
                        timespan.TotalMilliseconds,
                        exception.Message
                    );
                });
    }

    // ========================================
    // CIRCUIT BREAKER POLICIES
    // ========================================

    /// <summary>
    /// Política de Circuit Breaker para HTTP
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetHttpCircuitBreakerPolicy()
    {
        return Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(msg => (int)msg.StatusCode >= 500 || (int)msg.StatusCode == 429)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning(
                        "Circuit breaker opened for {Duration}s. Reason: {Reason}",
                        duration.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()
                    );
                },
                onReset: context =>
                {
                    var logger = context.GetLogger();
                    logger?.LogInformation("Circuit breaker reset - service recovered");
                },
                onHalfOpen: () =>
                {
                    // Logger não disponível aqui, mas podemos adicionar telemetria
                }
            );
    }

    /// <summary>
    /// Política de Circuit Breaker avançada para serviços críticos
    /// </summary>
    public static AsyncCircuitBreakerPolicy GetAdvancedCircuitBreakerPolicy()
    {
        return Policy
            .Handle<Exception>()
            .AdvancedCircuitBreakerAsync(
                failureThreshold: 0.5, // 50% de falhas
                samplingDuration: TimeSpan.FromSeconds(10),
                minimumThroughput: 8,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, duration, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogError(
                        exception,
                        "Advanced circuit breaker opened for {Duration}s",
                        duration.TotalSeconds
                    );
                },
                onReset: context =>
                {
                    var logger = context.GetLogger();
                    logger?.LogInformation("Advanced circuit breaker reset");
                }
            );
    }

    // ========================================
    // TIMEOUT POLICIES
    // ========================================

    /// <summary>
    /// Política de timeout para HTTP
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetHttpTimeoutPolicy(int seconds = 10)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            timeout: TimeSpan.FromSeconds(seconds),
            timeoutStrategy: TimeoutStrategy.Optimistic,
            onTimeoutAsync: (context, timespan, task) =>
            {
                var logger = context.GetLogger();
                logger?.LogWarning(
                    "Request timed out after {Timeout}s",
                    timespan.TotalSeconds
                );
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Política de timeout pessimista (cancela operação)
    /// </summary>
    public static AsyncTimeoutPolicy GetPessimisticTimeoutPolicy(int seconds = 30)
    {
        return Policy.TimeoutAsync(
            timeout: TimeSpan.FromSeconds(seconds),
            timeoutStrategy: TimeoutStrategy.Pessimistic,
            onTimeoutAsync: (context, timespan, task) =>
            {
                var logger = context.GetLogger();
                logger?.LogError(
                    "Operation cancelled after {Timeout}s timeout",
                    timespan.TotalSeconds
                );
                return Task.CompletedTask;
            });
    }

    // ========================================
    // COMBINED POLICIES (WRAP)
    // ========================================

    /// <summary>
    /// Política completa: Timeout -> Retry -> Circuit Breaker
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetFullResiliencePolicy()
    {
        var timeout = GetHttpTimeoutPolicy(10);
        var retry = GetHttpRetryPolicy();
        var circuitBreaker = GetHttpCircuitBreakerPolicy();

        // Ordem: Circuit Breaker (externo) -> Retry (meio) -> Timeout (interno)
        return Policy.WrapAsync(circuitBreaker, retry, timeout);
    }

    /// <summary>
    /// Política para chamadas entre microserviços
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetMicroservicePolicy()
    {
        return Policy.WrapAsync(
            GetHttpCircuitBreakerPolicy(),
            GetHttpRetryPolicy(),
            GetHttpTimeoutPolicy(5) // Timeout mais curto para serviços internos
        );
    }

    // ========================================
    // FALLBACK POLICIES
    // ========================================

    /// <summary>
    /// Política de fallback com valor padrão
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy()
    {
        return Policy<HttpResponseMessage>
            .Handle<Exception>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .FallbackAsync(
                fallbackValue: new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("{\"error\": \"Service temporarily unavailable\"}")
                },
                onFallbackAsync: (outcome, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning(
                        "Fallback activated. Reason: {Reason}",
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()
                    );
                    return Task.CompletedTask;
                });
    }

    // ========================================
    // BULKHEAD POLICIES (Rate Limiting)
    // ========================================

    /// <summary>
    /// Política de Bulkhead para limitar concorrência
    /// </summary>
    public static IAsyncPolicy GetBulkheadPolicy(int maxParallelization = 10, int maxQueuingActions = 20)
    {
        return Policy.BulkheadAsync(
            maxParallelization: maxParallelization,
            maxQueuingActions: maxQueuingActions,
            onBulkheadRejectedAsync: context =>
            {
                var logger = context.GetLogger();
                logger?.LogWarning("Bulkhead rejected - too many concurrent requests");
                return Task.CompletedTask;
            });
    }

    // ========================================
    // HELPERS
    // ========================================

    private static bool IsTransientError(Exception exception)
    {
        return exception is TimeoutException ||
               exception is HttpRequestException ||
               (exception.InnerException != null && IsTransientError(exception.InnerException));
    }

    private static ILogger? GetLogger(this Context context)
    {
        if (context.TryGetValue("Logger", out var logger))
        {
            return logger as ILogger;
        }
        return null;
    }
}
