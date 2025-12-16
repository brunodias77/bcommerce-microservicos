# Documento de Requisitos do Produto (PRD) - BCommerce Microservices

## 1. Visão do Produto
O **BCommerce Microservices** é uma plataforma de e-commerce moderna, escalável e resiliente, construída sobre uma arquitetura de microsserviços. O objetivo é fornecer uma solução robusta para gestão de identidade, catálogo, pedidos, pagamentos e promoções, capaz de suportar alto tráfego e garantir a consistência de dados através de padrões arquiteturais avançados como CQRS e Event-Driven Architecture.

## 2. Escopo do Projeto
O sistema é composto por um conjunto de serviços autônomos que se comunicam de forma assíncrona, garantindo baixo acoplamento e alta coesão.

### Serviços Principais
1.  **User Service**: Gestão de identidade e perfis.
2.  **Catalog Service**: Gestão de produtos e estoque.
3.  **Cart Service**: Gestão de carrinhos de compras.
4.  **Order Service**: Gestão do ciclo de vida dos pedidos.
5.  **Payment Service**: Processamento de pagamentos.
6.  **Coupon Service**: Gestão de cupons e promoções.

## 3. Requisitos Funcionais

### 3.1. User Service
*   **Gestão de Identidade**: Registro, login (JWT), refresh token, confirmação de e-mail, recuperação de senha.
*   **Perfil do Usuário**: Gerenciar dados pessoais (CPF, data de nascimento), avatar, preferências (moeda, idioma).
*   **Endereços**: CRUD de endereços com suporte a múltiplas entregas e endereço padrão.
*   **Notificações**: Sistema interno de notificações e preferências de comunicação (Email, SMS, Push).
*   **Segurança**: Histórico de login e gestão de sessões ativas por dispositivo.

### 3.2. Catalog Service
*   **Categorias**: Estrutura hierárquica (árvore) de categorias.
*   **Produtos**: Gestão completa (SKU, preços, dimensões, SEO), variantes e atributos dinâmicos.
*   **Imagens**: Gestão de galeria de imagens por produto.
*   **Estoque**: Controle de movimentação (entrada/saída) e reservas temporárias.
*   **Avaliações**: Sistema de reviews com aprovação e resposta do vendedor.

### 3.3. Cart Service
*   **Gestão de Carrinho**: Adicionar/remover itens, atualizar quantidades.
*   **Carrinho Anônimo**: Suporte a carrinhos sem login (baseado em sessão) com merge posterior.
*   **Validação em Tempo Real**: Verificação de estoque e preços (snapshots) ao manipular itens.
*   **Carrinhos Salvos**: Lista de desejos ou carrinhos salvos para depois.
*   **Recuperação**: Detecção de carrinhos abandonados.

### 3.4. Order Service
*   **Checkout**: Criação de pedidos com snapshot completo de dados (endereço, itens, preços).
*   **Fluxo de Status**: Máquina de estados (Pendente -> Pago -> Enviado -> Entregue/Cancelado).
*   **Rastreamento**: Histórico detalhado de tracking e eventos de entrega.
*   **Cancelamento/Reembolso**: Gestão de cancelamentos e solicitações de reembolso.
*   **Faturamento**: Emissão e registro de notas fiscais (NF-e).

### 3.5. Payment Service
*   **Processamento**: Suporte a múltiplos gateways (Stripe, Pagar.me) e métodos (Cartão, PIX, Boleto).
*   **Carteira Digital**: Salvar cartões (tokenizados) para compras futuras (One-Click Buy).
*   **Transações**: Registro detalhado de transações, capturas e estornos.
*   **Segurança**: Idempotência em transações e análise de fraude.

### 3.6. Coupon Service
*   **Criação de Cupons**: Tipos variados (Percentual, Fixo, Frete Grátis, Compre X Leve Y).
*   **Regras de Validação**: Valor mínimo, categorias específicas, validade, limite de uso global/por usuário.
*   **Aplicação**: Cálculo de desconto e reserva de cupom durante o checkout.

## 4. Requisitos Não Funcionais (Arquitetura)

### 4.1. Tecnologias
*   **Backend**: .NET (Core)
*   **Banco de Dados**: PostgreSQL (uma instância/database por serviço)
*   **Mensageria**: RabbitMQ (MassTransit)
*   **Cache**: Redis
*   **API Gateway**: Ocelot
*   **Observabilidade**: Serilog, OpenTelemetry, Seq/Jaeger

### 4.2. Padrões Arquiteturais
*   **Clean Architecture**: Separação clara de camadas (Domain, Application, Infrastructure, API).
*   **DDD (Domain-Driven Design)**: Modelagem rica com Aggregates, Entities, Value Objects.
*   **CQRS**: Segregação de responsabilidade entre Comandos (escrita) e Consultas (leitura).
*   **Event Sourcing (Ligeiro)** / **Event-Driven**: Comunicação assíncrona para consistência eventual.
*   **Outbox/Inbox Pattern**: Garantia de entrega e processamento de mensagens (At-Least-Once / Exactly-Once).

### 4.3. Qualidade e Resiliência
*   **Auditoria**: Logs de auditoria em todas as operações críticas (quem, quando, o que mudou).
*   **Concorrência**: Otimistic Concurrency Control (versionamento de registros).
*   **Testes**: Suporte a testes unitários, de integração e carga (K6).

## 5. Modelo de Dados (Destaques)
*   **Snapshots**: Dados críticos (endereço de entrega, preço do produto no momento da compra) são copiados (denormalizados) para garantir imutabilidade histórica.
*   **Idempotência**: Uso de chaves de idempotência em operações financeiras e mensageria.
*   **Extensibilidade**: Uso de campos JSONB (PostgreSQL) para atributos dinâmicos (ex: especificações de produto, metadados de pagamento).

---
*Este documento foi gerado automaticamente com base na análise da documentação técnica e schemas de banco de dados do projeto.*

## 6. Status de Implementação (Building Blocks)

Os seguintes componentes transversais (Building Blocks) foram implementados para dar suporte aos microsserviços:

*   **Bcommerce.BuildingBlocks.Core**: Padrões DDD (Entity, AggregateRoot, ValueObject), CQRS (ICommand, IQuery), Resultados e Exceções de Domínio.
*   **Bcommerce.BuildingBlocks.Infrastructure**: Abstrações de dados (EF Core, Repositories), Unit of Work, Interceptores de Auditoria e Padrão Outbox/Inbox.
*   **Bcommerce.BuildingBlocks.Messaging**: Integração com MassTransit/RabbitMQ, Publisher de Outbox, Event Bus e Filtros de Consumo.
*   **Bcommerce.BuildingBlocks.Web**: Padronização de APIs, Middlewares (Exception, Logging, CorrelationId), Filtros de Validação e Modelos de Resposta (ApiResponse).
*   **Bcommerce.BuildingBlocks.Security**: Geração de tokens JWT, definições de Políticas de Autorização e Extensões de Claims.
*   **Bcommerce.BuildingBlocks.Caching**: Abstração unificada de Cache (`ICacheService`) com implementações para Redis e Memory.
*   **Bcommerce.BuildingBlocks.Observability**: Configuração centralizada de Logging (Serilog), Métricas (Prometheus) e Tracing (OpenTelemetry).
