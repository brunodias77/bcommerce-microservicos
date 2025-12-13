# Payment Stripe API

API de pagamentos integrada com Stripe para processamento de transações.

## Arquitetura

```
┌──────────────────┐     ┌─────────────┐     ┌──────────────────┐
│   Frontend/App   │────▶│ Payment API │────▶│     Stripe       │
│                  │◀────│  (5003)     │◀────│                  │
└──────────────────┘     └──────┬──────┘     └──────────────────┘
                                │
                                │ Eventos
                                ▼
                         ┌─────────────┐
                         │  RabbitMQ   │
                         └─────────────┘
```

## Funcionalidades

- **Payment Intents**: Criar, confirmar, cancelar pagamentos
- **Customers**: Gerenciar clientes no Stripe
- **Refunds**: Processar reembolsos
- **Webhooks**: Receber notificações do Stripe em tempo real
- **Integration Events**: Publicar eventos de pagamento no RabbitMQ

## Endpoints

### Pagamentos

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| POST | `/api/payments/create-payment-intent` | Cria um Payment Intent |
| POST | `/api/payments/confirm` | Confirma um pagamento |
| GET | `/api/payments/{paymentIntentId}` | Consulta status do pagamento |
| POST | `/api/payments/{paymentIntentId}/cancel` | Cancela um pagamento |
| POST | `/api/payments/refund` | Cria um reembolso |

### Clientes

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| POST | `/api/customers` | Cria um cliente |
| GET | `/api/customers/{customerId}/payment-methods` | Lista métodos de pagamento |

### Webhooks

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| POST | `/api/webhooks/stripe` | Recebe eventos do Stripe |

### Utilitários

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/health` | Health check |
| GET | `/api/stripe/publishable-key` | Obtém chave pública |

## Configuração do Stripe

### 1. Obter chaves de API

1. Acesse [Stripe Dashboard](https://dashboard.stripe.com/test/apikeys)
2. Copie a **Secret key** (`sk_test_...`)
3. Copie a **Publishable key** (`pk_test_...`)

### 2. Configurar Webhook

1. Acesse [Stripe Webhooks](https://dashboard.stripe.com/test/webhooks)
2. Clique em "Add endpoint"
3. URL do endpoint: `https://seu-dominio.com/api/webhooks/stripe`
4. Selecione os eventos:
   - `payment_intent.succeeded`
   - `payment_intent.payment_failed`
   - `payment_intent.canceled`
   - `charge.refunded`
5. Copie o **Signing secret** (`whsec_...`)

### 3. Configurar variáveis de ambiente

```bash
# Criar arquivo .env na raiz do projeto
STRIPE_SECRET_KEY=sk_test_sua_chave_secreta
STRIPE_PUBLISHABLE_KEY=pk_test_sua_chave_publica
STRIPE_WEBHOOK_SECRET=whsec_seu_webhook_secret
```

## Como Executar

### Com Docker Compose

```bash
# Definir variáveis de ambiente
export STRIPE_SECRET_KEY=sk_test_...
export STRIPE_PUBLISHABLE_KEY=pk_test_...
export STRIPE_WEBHOOK_SECRET=whsec_...

# Subir todos os serviços
docker-compose up -d

# Ver logs
docker-compose logs -f api-payment
```

### Localmente

```bash
# 1. Subir infraestrutura
docker-compose up -d rabbitmq redis seq

# 2. Configurar appsettings.Development.json com suas chaves

# 3. Rodar a API
cd src/Services/Payment/PaymentStripe.API
dotnet run
```

## Fluxo de Pagamento

### 1. Criar Payment Intent (Backend)

```bash
curl -X POST http://localhost:5003/api/payments/create-payment-intent \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "550e8400-e29b-41d4-a716-446655440000",
    "amount": 10000,
    "currency": "brl",
    "customerEmail": "cliente@email.com",
    "customerName": "João Silva",
    "description": "Pedido #123"
  }'
```

**Resposta:**
```json
{
  "paymentIntentId": "pi_3O...",
  "clientSecret": "pi_3O..._secret_...",
  "status": "requires_payment_method",
  "amount": 10000,
  "currency": "brl"
}
```

### 2. Completar pagamento (Frontend com Stripe.js)

```javascript
// No frontend, usar o clientSecret para completar o pagamento
const stripe = Stripe('pk_test_...');

const { error, paymentIntent } = await stripe.confirmCardPayment(
  clientSecret,
  {
    payment_method: {
      card: cardElement, // Elemento do Stripe
      billing_details: {
        name: 'João Silva',
        email: 'cliente@email.com'
      }
    }
  }
);

if (error) {
  console.error(error.message);
} else if (paymentIntent.status === 'succeeded') {
  console.log('Pagamento realizado com sucesso!');
}
```

### 3. Consultar status

```bash
curl http://localhost:5003/api/payments/pi_3O...
```

### 4. Processar reembolso

```bash
curl -X POST http://localhost:5003/api/payments/refund \
  -H "Content-Type: application/json" \
  -d '{
    "paymentIntentId": "pi_3O...",
    "amount": 5000,
    "reason": "Devolução parcial"
  }'
```

## Eventos de Integração

A API publica os seguintes eventos no RabbitMQ:

| Evento | Descrição |
|--------|-----------|
| `PaymentIntentCreatedIntegrationEvent` | Payment Intent criado |
| `PaymentSucceededIntegrationEvent` | Pagamento bem-sucedido |
| `PaymentFailedIntegrationEvent` | Pagamento falhou |
| `PaymentCancelledIntegrationEvent` | Pagamento cancelado |
| `PaymentRefundedIntegrationEvent` | Reembolso processado |

## Testando com Stripe CLI

```bash
# Instalar Stripe CLI
brew install stripe/stripe-cli/stripe

# Login
stripe login

# Encaminhar webhooks para localhost
stripe listen --forward-to localhost:5003/api/webhooks/stripe

# Em outro terminal, disparar eventos de teste
stripe trigger payment_intent.succeeded
```

## Cartões de Teste

| Número | Descrição |
|--------|-----------|
| 4242 4242 4242 4242 | Pagamento bem-sucedido |
| 4000 0000 0000 9995 | Fundos insuficientes |
| 4000 0000 0000 0002 | Cartão recusado |
| 4000 0000 0000 3220 | Requer autenticação 3D Secure |

**CVV**: Qualquer 3 dígitos
**Data**: Qualquer data futura

## URLs

| Serviço | URL |
|---------|-----|
| Payment API (Swagger) | http://localhost:5003 |
| Stripe Dashboard | https://dashboard.stripe.com |
| Stripe Webhooks | https://dashboard.stripe.com/webhooks |
