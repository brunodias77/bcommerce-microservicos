# Mensageria e Eventos

A comunicação assíncrona é o pilar de integração entre os microsserviços do Bcommerce.

## Tecnologias
*   **Broker**: RabbitMQ
*   **Abstração**: MassTransit

## Padrão de Publicação/Consumo
Utilizamos o padrão **Publish/Subscribe**.
*   Um serviço publica um evento (ex: `OrderCreatedEvent`) sem saber quem vai ouvir.
*   Múltiplos serviços podem assinar esse evento (ex: `PaymentService` para cobrar, `StockService` para baixar estoque, `EmailService` para notificar).

## Topologia (RabbitMQ)
O MassTransit configura automaticamente a topologia baseada em **Exchanges** do tipo `Topic`.
*   Cada evento é uma Exchange.
*   Cada Consumidor cria uma Queue.
*   A Queue é bindada na Exchange do evento que ela consome.

Exemplo:
*   Exchange: `Bcommerce.Contracts.Order.Events:OrderCreatedEvent`
*   Queue A: `payment-service-order-created` (Bindada na Exchange acima)
*   Queue B: `notification-service-order-created` (Bindada na Exchange acima)

## Eventos de Integração
Os contratos dos eventos ficam na biblioteca compartilhada `Bcommerce.Contracts`. Isso garante tipagem forte e versionamento de contratos entre os serviços.

### Naming Convention
*   **Passado**: Eventos representam fatos que já ocorreram.
    *   `UserRegisteredEvent`
    *   `OrderPaidEvent`
    *   `StockReservedEvent`

## Configuração Resiliente
Todos os consumidores são configurados com:
1.  **Retry Policy**: Tenta reprocessar a mensagem algumas vezes em caso de falha transiente.
2.  **Circuit Breaker**: Interrompe o consumo se o serviço estiver instável.
3.  **Dead Letter Queue (DLQ)**: Se todas as tentativas falharem, a mensagem vai para uma fila de erro (`_error`) para análise manual, sem perda de dados.
