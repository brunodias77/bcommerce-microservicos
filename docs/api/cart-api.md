# Cart Service API

## Base URL
`/api/cart`

## Endpoints

### Cart Management
*   `GET /api/cart` - Obter carrinho atual (anônimo ou logado)
*   `DELETE /api/cart` - Limpar carrinho

### Items
*   `POST /api/cart/items` - Adicionar item ao carrinho
*   `PUT /api/cart/items/{itemId}` - Atualizar quantidade
*   `DELETE /api/cart/items/{itemId}` - Remover item

### Coupon
*   `POST /api/cart/coupon` - Aplicar cupom de desconto
*   `DELETE /api/cart/coupon` - Remover cupom
