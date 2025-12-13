using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace EventBus.RabbitMQ;

/// <summary>
/// Implementação da conexão persistente com RabbitMQ
/// </summary>
public class RabbitMQConnection : IRabbitMQConnection
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMQConnection> _logger;
    private readonly int _retryCount;
    private IConnection? _connection;
    private bool _disposed;
    private readonly object _syncRoot = new();

    public RabbitMQConnection(
        IConnectionFactory connectionFactory,
        ILogger<RabbitMQConnection> logger,
        int retryCount = 5)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _retryCount = retryCount;
    }

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public bool TryConnect()
    {
        _logger.LogInformation("Tentando conectar ao RabbitMQ...");

        lock (_syncRoot)
        {
            if (IsConnected) return true;

            var policy = Policy
                .Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(
                    _retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            exception,
                            "Falha ao conectar ao RabbitMQ. Tentativa {RetryCount} de {MaxRetries}. Aguardando {TimeSpan}s...",
                            retryCount,
                            _retryCount,
                            timeSpan.TotalSeconds);
                    });

            policy.Execute(() =>
            {
                _connection = _connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            if (IsConnected)
            {
                _connection!.ConnectionShutdownAsync += OnConnectionShutdown;
                _connection.CallbackExceptionAsync += OnCallbackException;
                _connection.ConnectionBlockedAsync += OnConnectionBlocked;

                _logger.LogInformation(
                    "Conexão RabbitMQ estabelecida com '{HostName}'",
                    _connection.Endpoint.HostName);

                return true;
            }

            _logger.LogCritical("Não foi possível estabelecer conexão com RabbitMQ");
            return false;
        }
    }

    public IChannel CreateModel()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Não há conexão RabbitMQ disponível para criar um canal");
        }

        return _connection!.CreateChannelAsync().GetAwaiter().GetResult();
    }

    private Task OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return Task.CompletedTask;

        _logger.LogWarning("Conexão RabbitMQ bloqueada. Razão: {Reason}", e.Reason);
        return Task.CompletedTask;
    }

    private Task OnCallbackException(object sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return Task.CompletedTask;

        _logger.LogWarning(e.Exception, "Exceção no callback RabbitMQ");
        return Task.CompletedTask;
    }

    private Task OnConnectionShutdown(object sender, ShutdownEventArgs e)
    {
        if (_disposed) return Task.CompletedTask;

        _logger.LogWarning("Conexão RabbitMQ encerrada. Razão: {ReplyText}", e.ReplyText);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao descartar conexão RabbitMQ");
        }
    }
}
