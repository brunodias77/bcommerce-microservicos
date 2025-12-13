# E-commerce Microservices Platform - .NET 8

## Project Overview

This is a production-grade e-commerce platform built with .NET 8 following microservices architecture, Domain-Driven Design (DDD), and Clean Architecture principles.

## Architecture

### Microservices
- **User Service** - Authentication, authorization (ASP.NET Core Identity), user profiles, addresses
- **Catalog Service** - Products, categories, stock management, reviews
- **Cart Service** - Shopping cart management, cart persistence
- **Order Service** - Order processing, tracking, invoices, refunds
- **Payment Service** - Payment processing, multiple gateways (Stripe, PagarMe, MercadoPago)
- **Coupon Service** - Discount coupons, promotions, validation

### Building Blocks (Shared Components)
- **Common.Domain** - Base entities, value objects, domain events
- **Common.Application** - CQRS behaviors, DTOs, exceptions
- **Common.Infrastructure** - Repository patterns, Outbox/Inbox patterns
- **EventBus.Abstractions** - Event bus interfaces
- **EventBus.RabbitMQ** - RabbitMQ implementation
- **WebHost.Customization** - Shared middleware, filters, extensions

## Design Patterns

### Domain-Driven Design (DDD)
- Aggregates and aggregate roots
- Value objects (Money, Address, Email)
- Domain events
- Repository pattern
- Specification pattern

### Clean Architecture
Each microservice is organized in 4 layers:
- **API Layer** - Controllers, gRPC services, webhooks
- **Application Layer** - CQRS (Commands/Queries), DTOs, validators
- **Domain Layer** - Aggregates, entities, domain events, business rules
- **Infrastructure Layer** - Persistence, external integrations

### CQRS (Command Query Responsibility Segregation)
- Commands for write operations
- Queries for read operations
- MediatR for request handling

### Event-Driven Architecture
- **Outbox Pattern** - Ensures reliable event publishing
- **Inbox Pattern** - Ensures idempotent event processing
- Asynchronous communication via RabbitMQ

### Other Patterns
- **Database per Service** - Each microservice has its own PostgreSQL database
- **API Gateway** - Backend for Frontend (BFF) pattern
- **Optimistic Locking** - Version-based concurrency control
- **Soft Delete** - Logical deletion with audit trail
- **Snapshots** - JSONB snapshots for historical data preservation

## Technology Stack

### Backend
- .NET 8 (LTS)
- ASP.NET Core Web API
- Entity Framework Core 8
- PostgreSQL 14+
- RabbitMQ (Event Bus)
- gRPC (inter-service communication)

### Extensions & Libraries
- MediatR (CQRS)
- FluentValidation (validation)
- AutoMapper (object mapping)
- Npgsql.EntityFrameworkCore.PostgreSQL

## Database Architecture

### PostgreSQL Databases
Each service has its own isolated database:
- `user_db` - User service
- `catalog_db` - Catalog service
- `cart_db` - Cart service
- `order_db` - Order service
- `payment_db` - Payment service
- `coupon_db` - Coupon service

### Key Database Features
- UUID primary keys
- Soft delete support (`deleted_at`)
- Optimistic locking (`version`)
- Audit logging
- JSONB for snapshots
- Outbox/Inbox tables per service
- PostgreSQL extensions: `uuid-ossp`, `citext`, `pg_trgm`

### Database Schemas Location
All database schemas are in `/docs/db/`:
- `00_shared_infrastructure.sql` - Shared extensions and functions
- `01_user_service.sql` - User service (ASP.NET Identity + custom tables)
- `02_catalog_service.sql` - Catalog service
- `03_cart_service.sql` - Cart service
- `04_order_service.sql` - Order service
- `05_payment_service.sql` - Payment service
- `06_coupon_service.sql` - Coupon service

## Cross-Service Communication

### Asynchronous (Event-Driven)
- RabbitMQ for publishing/subscribing to integration events
- Outbox pattern ensures at-least-once delivery
- Inbox pattern ensures idempotent processing

### Synchronous (Request/Response)
- gRPC for service-to-service calls
- BFF aggregators for frontend optimization

### Key Integration Events

| Event | Published By | Consumed By |
|-------|-------------|-------------|
| `USER_REGISTERED` | User Service | Email, Marketing |
| `PRODUCT_CREATED` | Catalog Service | Search, Marketing |
| `STOCK_UPDATED` | Catalog Service | Cart, Order |
| `CART_ABANDONED` | Cart Service | Email, Marketing |
| `ORDER_CREATED` | Order Service | Payment, Notification |
| `ORDER_PAID` | Order Service | Catalog, Shipping |
| `PAYMENT_CAPTURED` | Payment Service | Order |
| `COUPON_USED` | Coupon Service | Analytics |

## Getting Started

### Prerequisites
- .NET 8 SDK
- PostgreSQL 14+
- Docker & Docker Compose (for local development)
- RabbitMQ (can run via Docker)

### Setting Up Databases

```bash
# Create databases for each service
createdb user_db
createdb catalog_db
createdb cart_db
createdb order_db
createdb payment_db
createdb coupon_db

# For User Service (uses ASP.NET Identity)
cd src/Services/User.API
dotnet ef migrations add InitialIdentity
dotnet ef database update
psql -d user_db -f docs/db/01_user_service.sql

# For other services
psql -d catalog_db -f docs/db/02_catalog_service.sql
psql -d cart_db -f docs/db/03_cart_service.sql
psql -d order_db -f docs/db/04_order_service.sql
psql -d payment_db -f docs/db/05_payment_service.sql
psql -d coupon_db -f docs/db/06_coupon_service.sql
```

### Running the Application

```bash
# Using Docker Compose (recommended for local dev)
docker-compose up -d

# Or run individual services
cd src/Services/User.API
dotnet run

cd src/Services/Catalog.API
dotnet run

# Continue for other services...
```

## Project Structure

```
.
├── docs/                          # Documentation
│   ├── db/                        # Database schemas
│   ├── estrutura-de-pastas.md     # Detailed folder structure
│   └── list-commands.md           # Command reference
├── docker/                        # Docker configurations
│   └── docker-compose.yml
├── src/
│   ├── BuildingBlocks/            # Shared libraries
│   │   ├── Common/
│   │   │   ├── Common.Domain/
│   │   │   ├── Common.Application/
│   │   │   └── Common.Infrastructure/
│   │   ├── EventBus/
│   │   │   ├── EventBus.Abstractions/
│   │   │   └── EventBus.RabbitMQ/
│   │   └── WebHost/
│   │       └── WebHost.Customization/
│   └── Services/                  # Microservices
│       ├── User/
│       │   ├── User.API/
│       │   ├── User.Application/
│       │   ├── User.Domain/
│       │   ├── User.Infrastructure/
│       │   └── User.Tests/
│       ├── Catalog/
│       ├── Cart/
│       ├── Order/
│       ├── Payment/
│       └── Coupon/
├── Bcommerce-Microservices.sln
└── README.md
```

## Development Guidelines

### Code Organization
- Each microservice follows Clean Architecture
- Separation of concerns across layers
- Domain logic isolated in Domain layer
- Infrastructure concerns in Infrastructure layer

### Testing Strategy
- Unit tests for domain logic
- Integration tests with TestContainers
- E2E tests for critical flows

### Best Practices
- Use value objects for domain concepts
- Implement business rules in domain entities
- Validate commands with FluentValidation
- Use MediatR behaviors for cross-cutting concerns
- Apply Repository pattern for data access
- Implement Outbox pattern for reliable event publishing
- Use Inbox pattern for idempotent event processing

## Cross-Service References

Since each service has its own database, cross-service references are made by UUID **without foreign keys**:

```csharp
// In Order Service
public Guid UserId { get; set; }        // Ref to User.Id (no FK)
public Guid ProductId { get; set; }     // Ref to Product.Id (no FK)
public Guid? CouponId { get; set; }     // Ref to Coupon.Id (no FK)
```

For data that needs to be preserved, use JSONB snapshots:

```csharp
public JsonDocument ShippingAddress { get; set; }    // Address at order time
public JsonDocument ProductSnapshot { get; set; }    // Product at purchase time
public JsonDocument? CouponSnapshot { get; set; }    // Applied coupon
```

## Security

### Authentication & Authorization
- ASP.NET Core Identity (User Service)
- JWT tokens for API authentication
- Role-based authorization (Customer, Admin, Manager, Support)
- Claims-based authorization

### Data Protection
- Passwords hashed with Identity
- Sensitive data encrypted at rest
- HTTPS/TLS for all communications
- API rate limiting

## Common Tasks

### Adding a New Migration
```bash
cd src/Services/[ServiceName].API
dotnet ef migrations add [MigrationName]
dotnet ef database update
```

### Running Tests
```bash
dotnet test
```

### Building Solution
```bash
dotnet build Bcommerce-Microservices.sln
```

### Publishing a Service
```bash
cd src/Services/[ServiceName].API
dotnet publish -c Release -o ./publish
```

## Troubleshooting

### Database Connection Issues
- Verify PostgreSQL is running
- Check connection strings in `appsettings.json`
- Ensure databases are created

### RabbitMQ Connection Issues
- Verify RabbitMQ is running (default port 5672)
- Check event bus configuration
- Review RabbitMQ management UI (port 15672)

### EF Core Migration Issues
- Ensure you're in the correct API project directory
- Verify DbContext configuration
- Check connection string

## Additional Resources

- [Detailed folder structure](docs/estrutura-de-pastas.md)
- [Database schemas documentation](README.md)
- [Command reference](docs/list-commands.md)

## Contributing

When contributing to this project:
1. Follow Clean Architecture principles
2. Implement proper error handling
3. Add unit tests for new features
4. Update documentation as needed
5. Follow DDD patterns for domain logic
6. Use CQRS for commands and queries

## Notes for Claude Code

### When working on this codebase:

1. **Architecture awareness**: This is a microservices project with separate databases per service
2. **Cross-service references**: Never add foreign keys across services - use UUIDs and events
3. **CQRS pattern**: Commands go in `Commands/` folders, Queries in `Queries/` folders
4. **Domain logic**: Business rules belong in Domain entities, not in Application or API layers
5. **Event sourcing**: Use Outbox pattern when publishing events, Inbox for consuming
6. **Database changes**: User Service uses EF migrations, other services may use SQL scripts
7. **Testing**: Add tests in the corresponding `.Tests` project for each service

### Key file locations:
- Database schemas: `/docs/db/*.sql`
- Shared code: `/src/BuildingBlocks/`
- Service code: `/src/Services/[ServiceName]/`
- Each service has: API, Application, Domain, Infrastructure, Tests projects

### When adding new features:
1. Start in Domain layer (entities, value objects, events)
2. Add Application layer (commands/queries with handlers)
3. Expose via API layer (controllers)
4. Add Infrastructure as needed (repositories, external services)
5. Write tests

### Common patterns in this codebase:
- Aggregate roots inherit from base Entity class
- Commands use MediatR `IRequest<Result<T>>`
- Repositories use generic `IRepository<T>` interface
- Events implement `IDomainEvent` or `IntegrationEvent`
- DTOs use AutoMapper for mapping
