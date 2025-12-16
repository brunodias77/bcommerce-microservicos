# Consistência de Dados e Transações Distribuídas

Em microserviços, não temos transações ACID distribuídas (Two-Phase Commit é evitado por performance). Adotamos o modelo de **Consistência Eventual**.

## Transactional Outbox Pattern
Para evitar o problema de "Salvar no banco mas falhar ao publicar no RabbitMQ" (ou vice-versa), utilizamos o padrão **Outbox**.

1.  Quando uma ação ocorre (ex: Criar Pedido), salvamos a entidade `Order` e o evento `OrderCreatedEvent` na mesma transação de banco de dados (tabela `OutboxMessages`).
2.  O commit no banco garante atomicidade local.
3.  Um processo em background (o *Outbox Publisher* do MassTransit) lê a tabela de Outbox e publica as mensagens no RabbitMQ.
4.  Se falhar, ele tenta novamente. Isso garante **At-Least-Once Delivery**.

## Inbox Pattern / Idempotência
Como recebemos garantia de entrega "pelo menos uma vez", podemos receber a mesma mensagem duplicada. Para evitar processar duas vezes (ex: cobrar o cliente duas vezes), usamos o padrão **Inbox**.

1.  O consumidor verifica na tabela `InboxMessages` se já processou aquele `MessageId`.
2.  Se já processou, ignora.
3.  Se não, processa e salva o ID na tabela Inbox na mesma transação do negócio.

## Sagas (Coreografia)
Processos de negócio longos e complexos são modelados como Sagas Coreografadas (baseadas em eventos). Não há um orquestrador central; cada serviço reage a eventos e publica novos eventos.

**Exemplo: Fluxo de Pedido**
1.  **Order Service**: Cria pedido (Status: PENDING) -> Publica `OrderCreated`.
2.  **Catalog Service**: Ouve `OrderCreated` -> Reserva estoque -> Publica `StockReserved` (ou `StockReservationFailed`).
3.  **Payment Service**: Ouve `OrderCreated` (ou `StockReserved`) -> Processa pagamento -> Publica `PaymentApproved` (ou `PaymentRejected`).
4.  **Order Service**: Ouve `PaymentApproved` -> Muda status para PAID -> Publica `OrderPaid`.
    *   Se ouvir `PaymentRejected` -> Cancela pedido -> Publica `OrderCancelled`.
5.  **Catalog Service**: Ouve `OrderCancelled` -> Libera estoque reservado.

Esse modelo garante que, eventualmente, todos os serviços estarão consistentes (Pedido cancelado e Estoque liberado, ou Pedido Pago e Estoque baixado).
