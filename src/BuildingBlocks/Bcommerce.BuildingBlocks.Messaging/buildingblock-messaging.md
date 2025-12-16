# Documentação Bcommerce.BuildingBlocks.Messaging

Este projeto abstrai a complexidade do **MassTransit** e **RabbitMQ**, fornecendo interfaces limpas para publicação e consumo de eventos de integração.

## Sumário

1. [Abstrações (Interfaces)](#1-abstrações)
2. [Configuração (MassTransit)](#2-configuração)
3. [Transactional Outbox Publisher](#3-transactional-outbox-publisher)
4. [Filtros (Middleware)](#4-filtros)

---

## 1. Abstrações

**Problema:** Acoplar a aplicação diretamente ao MassTransit dificulta testes unitários e futuras trocas de tecnologia.
**Solução:** Definimos interfaces agnósticas no namespace `Abstractions`.

*   `IIntegrationEvent`: Marcador para eventos que trafegam entre microsserviços. Deve conter `EventId` e `OccurredOn`.
*   `IEventBus`: Interface principal para publicar mensagens.

**Exemplo Prático (Definindo um Evento):**

```csharp
using Bcommerce.BuildingBlocks.Messaging.Abstractions;

public record PedidoCriadoEvent(Guid PedidoId, decimal ValorTotal) : IntegrationEvent;
```

**Exemplo Prático (Publicando):**

```csharp
public class MeuServico
{
    private readonly IEventBus _eventBus;

    public MeuServico(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Notificar(Guid id)
    {
        await _eventBus.PublishAsync(new PedidoCriadoEvent(id, 100.00m));
    }
}
```

---

## 2. Configuração

**Problema:** Configurar o RabbitMQ, credenciais, retries e serialização repetidamente em cada microsserviço.
**Solução:** A extensão `AddMessageBus` centraliza essa configuração.

**Uso no `Program.cs`:**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Registra MassTransit, RabbitMQ, Consumers, etc.
// O assembly atual é passado para descobrir Consumers automaticamente.
builder.Services.AddMessageBus(builder.Configuration, typeof(Program).Assembly);
```

---

## 3. Transactional Outbox Publisher

**Problema:** O worker que processa a tabela `OutboxMessage` (implementado no Infrastructure) precisa de uma forma de despachar a mensagem deserializada para o broker real sem conhecer detalhes de implementação do broker.
**Solução:** `OutboxPublisher` é uma implementação de `IOutboxPublisher` (do Infrastructure) que usa o `IEventBus` (do Messaging) para fazer o envio final.

**Fluxo:**
1. Aplicação salva evento na tabela `OutboxMessage` (no banco).
2. Worker (BackgroundService) lê o banco.
3. Worker chama `OutboxPublisher.PublishAsync(evento)`.
4. `OutboxPublisher` chama `MassTransitEventBus.PublishAsync(evento)`.
5. Mensagem vai para o RabbitMQ.

---

## 4. Filtros (Middleware)

O MassTransit permite interceptar o processamento de mensagens (Pipeline). Implementamos filtros globais para consistência.

### LoggingFilter
Logs automáticos de "Processando mensagem X" e "Mensagem processada com sucesso/erro". Isso garante observabilidade padronizada sem poluir os Consumers com logs manuais de entrada/saída.

### IdempotencyFilter (Stub)
Prevê interceptar mensagens duplicadas (pelo `MessageId`) usando um armazenamento externo (como Redis) para garantir que uma mesma mensagem não cause efeitos colaterais duas vezes.

**Exemplo de Consumer (Limpo, graças aos filtros):**

```csharp
public class PedidoCriadoConsumer : IConsumer<PedidoCriadoEvent>
{
    public async Task Consume(ConsumeContext<PedidoCriadoEvent> context)
    {
        // Não precisa de try-catch ou logs de "inicio/fim", os filtros cuidam disso.
        var evento = context.Message;
        await _servico.Processar(evento.PedidoId);
    }
}
```
