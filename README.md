# Database Schemas - E-commerce Microservices

## 📋 Visão Geral

Este documento descreve os schemas de banco de dados otimizados para uma arquitetura de microserviços de e-commerce em **.NET 8**. Cada serviço possui seu próprio banco de dados isolado (Database per Service pattern).

## 🏗️ Arquitetura

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              API GATEWAY                                     │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
        ┌───────────────────────────┼───────────────────────────┐
        │               │           │           │               │
        ▼               ▼           ▼           ▼               ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│    User     │ │   Catalog   │ │    Cart     │ │    Order    │ │   Payment   │
│   Service   │ │   Service   │ │   Service   │ │   Service   │ │   Service   │
│  (Identity) │ │             │ │             │ │             │ │             │
└──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └──────┬──────┘
       │               │               │               │               │
       ▼               ▼               ▼               ▼               ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│  user_db    │ │ catalog_db  │ │  cart_db    │ │  order_db   │ │ payment_db  │
│ PostgreSQL  │ │ PostgreSQL  │ │ PostgreSQL  │ │ PostgreSQL  │ │ PostgreSQL  │
└─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘

                              ┌─────────────┐
                              │   Coupon    │
                              │   Service   │
                              └──────┬──────┘
                                     │
                                     ▼
                              ┌─────────────┐
                              │ coupon_db   │
                              │ PostgreSQL  │
                              └─────────────┘
```

## 📁 Estrutura dos Arquivos

```
schemas/
├── 00_shared_infrastructure.sql  # Extensions e funções compartilhadas
├── 01_user_service.sql           # ASP.NET Identity + extensões customizadas
├── 02_catalog_service.sql        # Produtos, categorias, estoque
├── 03_cart_service.sql           # Carrinho de compras
├── 04_order_service.sql          # Pedidos e histórico
├── 05_payment_service.sql        # Pagamentos e transações
└── 06_coupon_service.sql         # Cupons e promoções
```

## 🔐 User Service - ASP.NET Core Identity

O User Service utiliza o **ASP.NET Core Identity** para autenticação e autorização, estendido com tabelas customizadas para dados adicionais.

### Tabelas do Identity (gerenciadas pelo EF Core)

| Tabela             | Descrição                                            |
| ------------------ | ---------------------------------------------------- |
| `AspNetUsers`      | Usuários (email, senha, telefone, 2FA, lockout)      |
| `AspNetRoles`      | Roles do sistema (Customer, Admin, Manager, Support) |
| `AspNetUserRoles`  | Relação N:N entre usuários e roles                   |
| `AspNetUserClaims` | Claims específicas do usuário                        |
| `AspNetRoleClaims` | Claims associadas às roles                           |
| `AspNetUserLogins` | Logins externos (Google, Facebook, etc)              |
| `AspNetUserTokens` | Tokens (refresh, reset password, 2FA)                |

### Tabelas Customizadas (extensões)

| Tabela                          | Descrição                                          |
| ------------------------------- | -------------------------------------------------- |
| `user_profiles`                 | Dados estendidos (nome, CPF, avatar, preferências) |
| `addresses`                     | Endereços de entrega e cobrança                    |
| `user_favorite_products`        | Produtos favoritos (ref. cross-service)            |
| `user_login_history`            | Histórico de logins para auditoria                 |
| `user_sessions`                 | Gerenciamento de dispositivos/sessões              |
| `user_notifications`            | Notificações in-app                                |
| `user_notification_preferences` | Preferências de notificação                        |

### Modelo C# - ApplicationUser

```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    // Propriedades herdadas do Identity:
    // Id, UserName, Email, PasswordHash, PhoneNumber,
    // EmailConfirmed, TwoFactorEnabled, LockoutEnd, etc.

    // Navegações customizadas
    public virtual UserProfile? Profile { get; set; }
    public virtual ICollection<Address> Addresses { get; set; }
    public virtual ICollection<UserFavoriteProduct> FavoriteProducts { get; set; }
    public virtual ICollection<UserSession> Sessions { get; set; }
    public virtual ICollection<UserNotification> Notifications { get; set; }
    public virtual UserNotificationPreference? NotificationPreferences { get; set; }
}
```

### Configuração do Identity no Program.cs

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    // Password
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;

    // Lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // User
    options.User.RequireUniqueEmail = true;

    // SignIn
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<UserDbContext>()
.AddDefaultTokenProviders();
```

### Ordem de Execução - User Service

1. **Criar o banco**: `createdb user_db`
2. **Executar migration do Identity**: `dotnet ef database update`
3. **Executar tabelas customizadas**: `psql -d user_db -f 01_user_service.sql`
4. **Executar FKs** (seção 7 do arquivo SQL)
5. **Executar Seed de Roles** (seção 8 do arquivo SQL)

## 🔄 Padrões Implementados

### 1. Outbox Pattern

Cada serviço possui tabela `{service}_outbox_events` para garantir consistência eventual:

```sql
CREATE TABLE {service}_outbox_events (
    id UUID PRIMARY KEY,
    aggregate_type VARCHAR(100) NOT NULL,
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMPTZ,
    error_message TEXT,
    retry_count INT DEFAULT 0
);
```

### 2. Inbox Pattern (Idempotência)

Previne processamento duplicado de mensagens:

```sql
CREATE TABLE {service}_inbox_messages (
    id UUID PRIMARY KEY,
    message_type VARCHAR(100) NOT NULL,
    processed_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### 3. Optimistic Locking

Campo `version` para controle de concorrência:

```sql
version INT NOT NULL DEFAULT 1
-- Trigger incrementa automaticamente em cada UPDATE
```

### 4. Soft Delete

Campo `deleted_at` para exclusão lógica:

```sql
deleted_at TIMESTAMPTZ
-- NULL = ativo, NOT NULL = excluído
```

### 5. Audit Logging

Cada serviço mantém log de auditoria:

```sql
CREATE TABLE {service}_audit_logs (
    id UUID PRIMARY KEY,
    entity_type VARCHAR(100) NOT NULL,
    entity_id UUID NOT NULL,
    action VARCHAR(50) NOT NULL,
    old_values JSONB,
    new_values JSONB,
    user_id UUID,
    ip_address VARCHAR(45),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

## 📊 Serviços e Tabelas

### User Service (01_user_service.sql)

**Identity (EF Core Managed):**
| Tabela | Descrição |
|--------|-----------|
| `AspNetUsers` | Dados de autenticação do usuário |
| `AspNetRoles` | Roles (Customer, Admin, Manager, Support) |
| `AspNetUserRoles` | Associação usuário-role |
| `AspNetUserClaims` | Claims do usuário |
| `AspNetRoleClaims` | Claims da role |
| `AspNetUserLogins` | Provedores externos (OAuth) |
| `AspNetUserTokens` | Tokens de refresh/reset/2FA |

**Custom (SQL Managed):**
| Tabela | Descrição |
|--------|-----------|
| `user_profiles` | Perfil estendido (nome, CPF, avatar) |
| `addresses` | Endereços de entrega/cobrança |
| `user_favorite_products` | Favoritos (cross-service) |
| `user_login_history` | Histórico de logins |
| `user_sessions` | Sessões ativas |
| `user_notifications` | Notificações in-app |
| `user_notification_preferences` | Preferências |

### Catalog Service (02_catalog_service.sql)

| Tabela               | Descrição               |
| -------------------- | ----------------------- |
| `categories`         | Categorias hierárquicas |
| `products`           | Catálogo de produtos    |
| `product_images`     | Imagens dos produtos    |
| `stock_movements`    | Histórico de estoque    |
| `stock_reservations` | Reservas temporárias    |
| `product_reviews`    | Avaliações              |

### Cart Service (03_cart_service.sql)

| Tabela              | Descrição                      |
| ------------------- | ------------------------------ |
| `carts`             | Carrinhos (logados e anônimos) |
| `cart_items`        | Itens do carrinho              |
| `cart_activity_log` | Log de atividades              |
| `saved_carts`       | Carrinhos salvos               |

### Order Service (04_order_service.sql)

| Tabela                  | Descrição               |
| ----------------------- | ----------------------- |
| `orders`                | Pedidos                 |
| `order_items`           | Itens do pedido         |
| `order_status_history`  | Histórico de status     |
| `order_tracking_events` | Eventos de rastreamento |
| `order_invoices`        | Notas fiscais           |
| `order_refunds`         | Reembolsos              |

### Payment Service (05_payment_service.sql)

| Tabela                 | Descrição                   |
| ---------------------- | --------------------------- |
| `user_payment_methods` | Métodos de pagamento salvos |
| `payments`             | Pagamentos                  |
| `payment_transactions` | Transações com gateway      |
| `payment_refunds`      | Reembolsos                  |
| `payment_chargebacks`  | Contestações                |
| `payment_webhooks`     | Webhooks recebidos          |

### Coupon Service (06_coupon_service.sql)

| Tabela                       | Descrição                 |
| ---------------------------- | ------------------------- |
| `coupons`                    | Cupons de desconto        |
| `coupon_eligible_categories` | Categorias elegíveis      |
| `coupon_eligible_products`   | Produtos elegíveis        |
| `coupon_eligible_users`      | Usuários elegíveis        |
| `coupon_usages`              | Registro de uso           |
| `coupon_reservations`        | Reservas durante checkout |

## 🔗 Referências Cross-Service

Como cada serviço tem seu próprio banco, referências entre serviços são feitas por UUID **sem Foreign Keys**:

```sql
-- No Order Service
user_id UUID NOT NULL,      -- Ref. AspNetUsers.Id (sem FK)
coupon_id UUID,             -- Ref. Coupon Service (sem FK)
product_id UUID NOT NULL,   -- Ref. Catalog Service (sem FK)
```

### Snapshots

Para dados que precisam ser preservados, usamos JSONB snapshots:

```sql
-- Endereço no momento do pedido
shipping_address JSONB NOT NULL,

-- Produto no momento da compra
product_snapshot JSONB NOT NULL,

-- Cupom aplicado
coupon_snapshot JSONB,
```

## 📈 Eventos entre Serviços

### Eventos Publicados

| Serviço | Evento             | Consumers                   |
| ------- | ------------------ | --------------------------- |
| User    | `USER_REGISTERED`  | Email, Marketing            |
| User    | `USER_DELETED`     | Todos                       |
| User    | `PROFILE_UPDATED`  | Order, Payment              |
| Catalog | `PRODUCT_CREATED`  | Search, Marketing           |
| Catalog | `STOCK_UPDATED`    | Cart, Order                 |
| Catalog | `PRICE_CHANGED`    | Cart                        |
| Cart    | `CART_ABANDONED`   | Email, Marketing            |
| Cart    | `CART_CONVERTED`   | Order                       |
| Order   | `ORDER_CREATED`    | Payment, Notification       |
| Order   | `ORDER_PAID`       | Catalog (estoque), Shipping |
| Order   | `ORDER_SHIPPED`    | Notification                |
| Order   | `ORDER_CANCELLED`  | Catalog (estoque), Payment  |
| Payment | `PAYMENT_CAPTURED` | Order                       |
| Payment | `PAYMENT_FAILED`   | Order, Notification         |
| Payment | `REFUND_COMPLETED` | Order                       |
| Coupon  | `COUPON_USED`      | Analytics                   |
| Coupon  | `COUPON_EXPIRED`   | Marketing                   |

## 🚀 Como Usar

### 1. Criar Databases

```bash
# Criar um database por serviço
createdb user_db
createdb catalog_db
createdb cart_db
createdb order_db
createdb payment_db
createdb coupon_db
```

### 2. User Service (com Identity)

```bash
# 1. Criar migration do Identity
cd src/Services/User.API
dotnet ef migrations add InitialIdentity

# 2. Aplicar migration (cria tabelas AspNet*)
dotnet ef database update

# 3. Executar tabelas customizadas
psql -d user_db -f schemas/01_user_service.sql

# 4. Executar FKs (descomentar seção 7 do SQL)
# 5. Executar Seed de Roles (descomentar seção 8 do SQL)
```

### 3. Outros Serviços

```bash
# Executar schemas diretamente
psql -d catalog_db -f schemas/02_catalog_service.sql
psql -d cart_db -f schemas/03_cart_service.sql
psql -d order_db -f schemas/04_order_service.sql
psql -d payment_db -f schemas/05_payment_service.sql
psql -d coupon_db -f schemas/06_coupon_service.sql
```

## 🔧 Configuração do DbContext

### UserDbContext

```csharp
public class UserDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<UserFavoriteProduct> FavoriteProducts => Set<UserFavoriteProduct>();
    public DbSet<UserSession> Sessions => Set<UserSession>();
    public DbSet<UserLoginHistory> LoginHistory => Set<UserLoginHistory>();
    public DbSet<UserNotification> Notifications => Set<UserNotification>();
    public DbSet<UserNotificationPreference> NotificationPreferences => Set<UserNotificationPreference>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Importante: configura Identity

        // Suas configurações customizadas
        builder.ApplyConfigurationsFromAssembly(typeof(UserDbContext).Assembly);
    }
}
```

## ✅ Melhorias Implementadas

1. **ASP.NET Core Identity** - Autenticação robusta e testada
2. **Soft Delete** (`deleted_at`) - Auditoria e recuperação
3. **Versionamento** (`version`) - Optimistic locking
4. **Outbox/Inbox Pattern** - Consistência eventual
5. **Audit Logs** - Rastreabilidade completa
6. **Snapshots JSONB** - Preservação de dados históricos
7. **Índices otimizados** - Performance de consultas
8. **Views materializadas** - Agregações performáticas
9. **Constraints robustos** - Integridade de dados
10. **Triggers automáticos** - `updated_at`, `version`, histórico
11. **Idempotency Keys** - Operações seguras de retry
12. **Reservas de estoque** - Evita overselling
13. **Gerenciamento de sessões** - Controle de dispositivos
14. **Notificações in-app** - Sistema completo

## 📦 Pacotes NuGet Necessários

```xml
<!-- User Service -->
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.*" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.*" />

<!-- Todos os serviços -->
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.*" />
```

## 📝 Notas

- PostgreSQL 14+ recomendado
- .NET 8 LTS
- Extensions necessárias: `uuid-ossp`, `citext`, `pg_trgm`
- Considerar particionamento para tabelas de alta volumetria
- Implementar jobs para limpeza periódica (tokens expirados, sessões, etc.)
- O Identity já gerencia: email confirmation, password reset, 2FA, lockout
