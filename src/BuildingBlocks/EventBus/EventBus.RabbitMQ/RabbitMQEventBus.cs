using EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventBus.RabbitMQ;

/// <summary>
/// Implementação do Event Bus usando RabbitMQ
/// </summary>
public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly IRabbitMQConnection _connection;
    private readonly IEventBusSubscriptionsManager _subsManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQEventBus> _logger;
    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly SemaphoreSlim _consumerLock = new(1, 1);
    private IChannel? _consumerChannel;
    private bool _consumerStarted;

    public RabbitMQEventBus(
        IRabbitMQConnection connection,
        IEventBusSubscriptionsManager subsManager,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQEventBus> logger,
        string exchangeName = "ecommerce_event_bus",
        string queueName = "")
    {
        _connection = connection;
        _subsManager = subsManager;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _exchangeName = exchangeName;
        _queueName = string.IsNullOrWhiteSpace(queueName) ? GenerateDefaultQueueName() : queueName;

        _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
    }

    private static string GenerateDefaultQueueName()
    {
        var serviceName = Environment.GetEnvironmentVariable("SERVICE_NAME")
                          ?? AppDomain.CurrentDomain.FriendlyName
                          ?? "service";
        var normalized = new string(serviceName
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '_')
            .ToArray());
        return $"{normalized}.events";
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }

        using var channel = _connection.CreateModel();

        var eventName = @event.GetType().Name;

        await channel.ExchangeDeclareAsync(
            exchange: _exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var message = JsonSerializer.Serialize(@event, @event.GetType());
        var body = Encoding.UTF8.GetBytes(message);

        var properties = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = @event.Id.ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: eventName,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Evento publicado {EventId} do tipo {EventName}",
            @event.Id,
            eventName);
    }

    public async Task PublishDynamicAsync(string eventType, string payload, CancellationToken cancellationToken = default)
    {
        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }

        using var channel = _connection.CreateModel();

        await channel.ExchangeDeclareAsync(
            exchange: _exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(payload);

        var properties = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Type = eventType
        };

        await channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: eventType,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Evento dinâmico publicado do tipo {EventType}",
            eventType);
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        var eventName = _subsManager.GetEventKey<TEvent>();

        _logger.LogInformation(
            "Inscrevendo no evento {EventName} com {HandlerName}",
            eventName,
            typeof(THandler).Name);

        _subsManager.AddSubscription<TEvent, THandler>();
        EnsureBindingAsync(eventName).GetAwaiter().GetResult();
        StartBasicConsumeAsync().GetAwaiter().GetResult();
    }

    public void Unsubscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        _subsManager.RemoveSubscription<TEvent, THandler>();
    }

    private async Task StartBasicConsumeAsync()
    {
        await _consumerLock.WaitAsync();
        try
        {
            if (_consumerStarted) return;

            if (!_connection.IsConnected)
            {
                _connection.TryConnect();
            }

            _consumerChannel = _connection.CreateModel();

            await _consumerChannel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            await _consumerChannel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
            consumer.ReceivedAsync += Consumer_Received;

            await _consumerChannel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: false,
                consumer: consumer);

            _consumerStarted = true;
        }
        finally
        {
            _consumerLock.Release();
        }
    }

    private async Task EnsureBindingAsync(string eventName)
    {
        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }

        using var channel = _connection.CreateModel();

        await channel.ExchangeDeclareAsync(
            exchange: _exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        await channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        await channel.QueueBindAsync(
            queue: _queueName,
            exchange: _exchangeName,
            routingKey: eventName);
    }

    private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
    {
        var eventName = eventArgs.RoutingKey;
        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

        try
        {
            await ProcessEvent(eventName, message);

            if (_consumerChannel != null)
                await _consumerChannel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar evento {EventName}", eventName);

            // Reject and requeue
            if (_consumerChannel != null)
                await _consumerChannel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        if (!_subsManager.HasSubscriptionsForEvent(eventName))
        {
            _logger.LogWarning("Nenhuma inscrição para evento {EventName}", eventName);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var handlers = _subsManager.GetHandlersForEvent(eventName);

        foreach (var handlerType in handlers)
        {
            var handler = scope.ServiceProvider.GetService(handlerType);
            if (handler == null) continue;

            var eventType = _subsManager.GetEventTypeByName(eventName);
            if (eventType == null) continue;

            var integrationEvent = JsonSerializer.Deserialize(message, eventType);
            if (integrationEvent == null) continue;

            var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
            var method = concreteType.GetMethod(nameof(IIntegrationEventHandler<IntegrationEvent>.HandleAsync));

            if (method != null)
            {
                await (Task)method.Invoke(handler, new[] { integrationEvent, CancellationToken.None })!;
            }
        }
    }

    private void SubsManager_OnEventRemoved(object? sender, string eventName)
    {
        _ = HandleEventRemovedAsync(eventName);
    }

    private async Task HandleEventRemovedAsync(string eventName)
    {
        try
        {
            if (!_connection.IsConnected)
            {
                _connection.TryConnect();
            }

            using var channel = _connection.CreateModel();
            await channel.QueueUnbindAsync(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: eventName);

            if (_subsManager.IsEmpty)
            {
                if (_consumerChannel != null)
                    await _consumerChannel.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover binding do evento {EventName}", eventName);
        }
    }

    public void Dispose()
    {
        _consumerChannel?.Dispose();
        _consumerLock.Dispose();
        _subsManager.Clear();
    }
}
