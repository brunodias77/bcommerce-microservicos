# Microsserviços

A plataforma é composta pelos seguintes serviços autônomos:

## 1. User Service (Identidade e Clientes)
**Responsabilidade**: Gerenciamento completo de usuários, autenticação e perfis.
*   Registro e Login (Identity).
*   Gestão de Perfil (Avatar, Dados Pessoais).
*   Gestão de Endereços de Entrega.
*   Preferências de Notificação.
*   Autenticação centralizada (emissão de tokens JWT).

## 2. Catalog Service (Catálogo de Produtos)
**Responsabilidade**: Gestão de produtos, categorias e estoque.
*   CRUD de Produtos e Categorias.
*   Controle de Estoque (Entradas, Saídas, Reservas).
*   Avaliações de Produtos (Reviews).
*   Busca e Listagem de produtos.

## 3. Cart Service (Carrinho de Compras)
**Responsabilidade**: Gestão temporária de itens de compra.
*   Adicionar/Remover itens.
*   Cálculo de subtotal.
*   Aplicação de Cupons (validação via RPC/gRPC ou Eventos com Coupon Service).
*   Suporte a carrinhos anônimos e logados.

## 4. Order Service (Pedidos)
**Responsabilidade**: Ciclo de vida do pedido.
*   Criação de Pedidos (Checkout).
*   Orquestração do fluxo de status (Pendente -> Pago -> Enviado -> Entregue).
*   Histórico de pedidos do usuário.
*   Rastreamento de entrega.

## 5. Payment Service (Pagamentos)
**Responsabilidade**: Processamento financeiro.
*   Integração com Gateways (Stripe, Pagar.me, etc.).
*   Processamento de Cartão de Crédito, PIX, Boleto.
*   Gestão de Reembolsos (Refunds).
*   Tokenização de cartões (Wallet).

## 6. Coupon Service (Promoções)
**Responsabilidade**: Lógica de descontos e promoções.
*   Criação e gestão de cupons.
*   Regras de validação (Validade, Valor Mínimo, Categoria Específica).
*   Contabilização de uso de cupons.

## API Gateway
Existe um **API Gateway (Ocelot)** na frente de todos os serviços, atuando como ponto único de entrada para clientes externos (Front-end/Mobile), lidando com roteamento e agregação básica.
