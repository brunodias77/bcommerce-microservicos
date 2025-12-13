# Services - Producer e Consumer API

Este diretório contém duas APIs de demonstração que utilizam o EventBus com RabbitMQ para comunicação assíncrona.

## Arquitetura

```
┌──────────────────┐     ┌─────────────┐     ┌──────────────────┐
│   Producer API   │────▶│  RabbitMQ   │────▶│   Consumer API   │
│   (porta 5001)   │     │(porta 5672) │     │   (porta 5002)   │
└──────────────────┘     └─────────────┘     └──────────────────┘
        │                       │                     │
        └───────────────────────┼─────────────────────┘
                                │
                         ┌──────┴──────┐
                         │   Seq Log   │
                         │(porta 8081) │
                         └─────────────┘
```

## Serviços

### Producer API (porta 5001)
- **POST /api/orders** - Cria um pedido e publica evento `OrderCreatedIntegrationEvent`
- **POST /api/orders/bulk?count=10** - Cria múltiplos pedidos para teste
- **GET /api/orders/{orderId}** - Consulta um pedido (simulado)

### Consumer API (porta 5002)
- **GET /api/processedorders** - Lista todos os pedidos processados
- **GET /api/processedorders/{orderId}** - Consulta um pedido processado
- **GET /api/processedorders/stats** - Estatísticas de processamento
- **GET /api/processedorders/recent?count=10** - Últimos pedidos processados

## Como Executar

### Com Docker Compose (Recomendado)

```bash
# Subir toda a infraestrutura
docker-compose up -d

# Ver logs
docker-compose logs -f api-producer api-consumer

# Parar tudo
docker-compose down
```

### Localmente (para desenvolvimento)

1. **Subir apenas a infraestrutura:**
```bash
docker-compose up -d postgres rabbitmq redis seq
```

2. **Rodar Producer API:**
```bash
cd src/Services/Producer/Producer.API
dotnet run
```

3. **Rodar Consumer API (em outro terminal):**
```bash
cd src/Services/Consumer/Consumer.API
dotnet run
```

## Testando

### 1. Criar um pedido (Producer)
```bash
curl -X POST http://localhost:5001/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "João Silva",
    "customerEmail": "joao@email.com",
    "items": [
      {
        "productId": "00000000-0000-0000-0000-000000000001",
        "productName": "Notebook Dell",
        "quantity": 1,
        "unitPrice": 3500.00
      },
      {
        "productId": "00000000-0000-0000-0000-000000000002",
        "productName": "Mouse Logitech",
        "quantity": 2,
        "unitPrice": 150.00
      }
    ]
  }'
```

### 2. Criar pedidos em lote
```bash
curl -X POST "http://localhost:5001/api/orders/bulk?count=5"
```

### 3. Consultar pedidos processados (Consumer)
```bash
# Todos os pedidos
curl http://localhost:5002/api/processedorders

# Estatísticas
curl http://localhost:5002/api/processedorders/stats

# Últimos 5 pedidos
curl "http://localhost:5002/api/processedorders/recent?count=5"
```

## URLs

| Serviço | URL |
|---------|-----|
| Producer API (Swagger) | http://localhost:5001 |
| Consumer API (Swagger) | http://localhost:5002 |
| RabbitMQ Management | http://localhost:15672 (bcommerce/bcommerce123) |
| Seq (Logs) | http://localhost:8081 |
| PostgreSQL | localhost:5432 (bcommerce/bcommerce123) |
| Redis | localhost:6379 (password: bcommerce123) |

## Fluxo de Eventos

1. Producer API recebe requisição POST `/api/orders`
2. Cria evento `OrderCreatedIntegrationEvent`
3. Publica no RabbitMQ (exchange: `ecommerce_event_bus`)
4. Consumer API está inscrito no evento
5. Consumer processa e armazena o pedido em memória
6. Consumer disponibiliza consulta via API
