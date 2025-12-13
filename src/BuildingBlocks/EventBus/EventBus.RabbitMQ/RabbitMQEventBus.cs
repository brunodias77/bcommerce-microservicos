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
    private IChannel? _consumerChannel;

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
        _queueName = queueName;

        _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
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
            "Published event {EventId} of type {EventName}",
            @event.Id,
            eventName);
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        var eventName = _subsManager.GetEventKey<TEvent>();

        _logger.LogInformation(
            "Subscribing to event {EventName} with {HandlerName}",
            eventName,
            typeof(THandler).Name);

        _subsManager.AddSubscription<TEvent, THandler>();
        StartBasicConsume();
    }

    public void Unsubscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        _subsManager.RemoveSubscription<TEvent, THandler>();
    }

    private async void StartBasicConsume()
    {
        if (_consumerChannel != null) return;

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
            _logger.LogError(ex, "Error processing event {EventName}", eventName);

            // Reject and requeue
            if (_consumerChannel != null)
                await _consumerChannel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        if (!_subsManager.HasSubscriptionsForEvent(eventName))
        {
            _logger.LogWarning("No subscription for event {EventName}", eventName);
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

    private async void SubsManager_OnEventRemoved(object? sender, string eventName)
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

    public void Dispose()
    {
        _consumerChannel?.Dispose();
        _subsManager.Clear();
    }
}
