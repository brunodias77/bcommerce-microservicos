# Order Service API

## Base URL
`/api/orders`

## Endpoints

### Orders
*   `POST /api/orders` - Criar pedido (Checkout)
*   `GET /api/orders` - Listar pedidos do usuário
*   `GET /api/orders/{id}` - Obter detalhes do pedido
*   `POST /api/orders/{id}/cancel` - Cancelar pedido

### Tracking
*   `GET /api/orders/{id}/tracking` - Obter rastreamento da entrega

### Admin Ops
*   `PUT /api/orders/{id}/status` - Atualizar status (Sistema ou Admin)
*   `POST /api/orders/{id}/invoice` - Emitir Nota Fiscal
