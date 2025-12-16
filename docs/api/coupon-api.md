# Coupon Service API

## Base URL
`/api/coupons`

## Endpoints

### Coupon Management
*   `GET /api/coupons` - Listar cupons
*   `GET /api/coupons/{code}` - Obter detalhes do cupom
*   `POST /api/coupons` - Criar cupom (Admin)
*   `PUT /api/coupons/{id}` - Atualizar cupom (Admin)

### Validation
*   `POST /api/coupons/validate` - Validar se cupom é aplicável ao carrinho (RPC)
