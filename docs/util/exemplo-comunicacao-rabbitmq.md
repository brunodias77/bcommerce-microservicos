# Comunicação entre Microserviços com RabbitMQ

Este documento mostra, com exemplos práticos, como publicar e consumir eventos de integração entre serviços usando o Event Bus baseado em RabbitMQ presente neste repositório.

## Conceitos

- Exchange: `direct` e durável, por padrão `ecommerce_event_bus`.
- Queue por serviço: cada microserviço consome de uma fila própria (ex.: `order-service.events`).
- Routing key: nome do evento (`EventType`), usado para fazer o bind fila↔exchange.
- Evento: classe que deriva de `IntegrationEvent`.
- Handler: classe que implementa `IIntegrationEventHandler<TEvent>`.

## Registro no DI

```csharp
// Startup/Program de cada serviço
using EventBus.Abstractions;
using EventBus.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// 1) Registrar o manager de subscrições
services.AddSingleton<IEventBusSubscriptionsManager, EventBusSubscriptionsManager>();

// 2) Registrar a conexão RabbitMQ (implemente IRabbitMQConnection conforme seu ambiente)
services.AddSingleton<IRabbitMQConnection>(sp =>
{
    // Exemplo: instanciar sua implementação concreta de IRabbitMQConnection
    // return new RabbitMQConnection(hostname, username, password, logger);
    throw new NotImplementedException("Registre sua implementação de IRabbitMQConnection");
});

// 3) Registrar o EventBus com queueName do microserviço
services.AddSingleton<IEventBus>(sp =>
{
    var connection = sp.GetRequiredService<IRabbitMQConnection>();
    var subsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();
    var logger = sp.GetRequiredService<ILogger<RabbitMQEventBus>>();

    // Use um nome de fila único por serviço
    var queueName = "order-service.events"; // ou defina via env var SERVICE_NAME

    return new RabbitMQEventBus(
        connection,
        subsManager,
        sp,
        logger,
        exchangeName: "ecommerce_event_bus",
        queueName: queueName);
});
```

Observação: se `queueName` não for informado, o `RabbitMQEventBus` gera automaticamente `<SERVICE_NAME>.events` usando a env var `SERVICE_NAME` ou o `FriendlyName` do AppDomain.

## Definição de um Evento (Producer → Consumer)

```csharp
using EventBus.Abstractions;

public sealed class OrderCreatedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public decimal Total { get; }

    public OrderCreatedIntegrationEvent(Guid orderId, Guid userId, decimal total)
    {
        OrderId = orderId;
        UserId = userId;
        Total = total;
    }
}
```

## Publicando um Evento (Producer)

```csharp
using EventBus.Abstractions;

public class OrderService
{
    private readonly IEventBus _eventBus;

    public OrderService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task CreateOrderAsync(Guid orderId, Guid userId, decimal total, CancellationToken ct)
    {
        // ... lógica de criação do pedido ...

        var evt = new OrderCreatedIntegrationEvent(orderId, userId, total);
        await _eventBus.PublishAsync(evt, ct);
    }
}
```

Ao publicar, o `RabbitMQEventBus`:
- Declara o exchange `ecommerce_event_bus` (durável).
- Serializa o evento como JSON.
- Publica usando `routingKey` igual ao nome do evento (ex.: `OrderCreatedIntegrationEvent`).

## Consumindo um Evento (Consumer)

```csharp
using EventBus.Abstractions;

public class OrderCreatedIntegrationEventHandler : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    public async Task HandleAsync(OrderCreatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        // Exemplo: iniciar processamento de pagamento/logística/etc.
        // await _paymentService.ProcessAsync(@event.OrderId, @event.Total, cancellationToken);
        await Task.CompletedTask;
    }
}
```

Registrar o handler e assinar o evento no startup do serviço consumidor:

```csharp
using EventBus.Abstractions;

// Registrar o handler no DI
services.AddTransient<OrderCreatedIntegrationEventHandler>();

// Após construir o ServiceProvider:
var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

// Assinar eventos
eventBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();
```

Ao assinar, o `RabbitMQEventBus`:
- Garante a declaração de `exchange` e `queue` do serviço.
- Realiza `QueueBind` da fila do serviço para o `exchange` com `routingKey = EventType` (ex.: `OrderCreatedIntegrationEvent`).
- Inicia o consumo assíncrono da fila e entrega mensagens aos handlers registrados via DI.

## Diretrizes de Fila por Serviço

- Use um `queueName` exclusivo por serviço (ex.: `user-service.events`, `payment-service.events`).
- Um serviço pode assinar múltiplos eventos; cada assinatura cria um `bind` adicional para o mesmo `queueName` com o `routingKey` do evento.
- Em `Unsubscribe`, o Event Bus realiza `QueueUnbind` para manter a fila apenas nos eventos desejados.

## Boas Práticas

- Idempotência: combine com os padrões `Inbox/Outbox` (vide BuildingBlocks) para garantir processamento único e entrega confiável.
- Observabilidade: monitore taxa de processamento, reentregas e erros; incorpore logs/correlation-id dos middlewares.
- Resiliência: em produtores, considere reintentos e circuit breakers nas dependências externas; consumidores devem tratar exceções e `nack` com requeue quando apropriado.
