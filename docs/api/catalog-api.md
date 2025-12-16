# Catalog Service API

## Base URL
`/api/catalog`

## Endpoints

### Products
*   `GET /api/products` - Listar produtos (com paginação e filtros)
*   `GET /api/products/{id}` - Obter detalhes do produto
*   `POST /api/products` - Criar produto (Admin)
*   `PUT /api/products/{id}` - Atualizar produto (Admin)
*   `DELETE /api/products/{id}` - Remover produto (Admin)

### Categories
*   `GET /api/categories` - Listar árvore de categorias
*   `POST /api/categories` - Criar categoria (Admin)

### Stock
*   `GET /api/products/{id}/stock` - Consultar estoque
*   `POST /api/stock/adjust` - Ajuste manual de estoque (Admin)

### Reviews
*   `GET /api/products/{id}/reviews` - Listar avaliações
*   `POST /api/products/{id}/reviews` - Avaliar produto
