# Payment Service API

## Base URL
`/api/payment`

## Endpoints

### Payments
*   `GET /api/payments/{id}` - Obter detalhes do pagamento
*   `POST /api/payments/{id}/refund` - Solicitar reembolso (Admin)

### Payment Methods (Wallet)
*   `GET /api/payment-methods` - Listar cartões salvos
*   `POST /api/payment-methods` - Salvar novo cartão
*   `DELETE /api/payment-methods/{id}` - Remover cartão salvo

### Webhooks
*   `POST /api/webhooks/{gateway}` - Receber notificações de pagamento (Stripe/Pagar.me)
