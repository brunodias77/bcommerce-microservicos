# Estrutura de Pastas - E-commerce Microservices (Senior Level)

```
ecommerce-platform/
│
├── .github/
│   ├── workflows/                    # CI/CD pipelines
│   │   ├── user-service.yml
│   │   ├── catalog-service.yml
│   │   ├── cart-service.yml
│   │   ├── order-service.yml
│   │   ├── payment-service.yml
│   │   └── coupon-service.yml
│   └── dependabot.yml
│
├── docs/
│   ├── architecture/                 # Diagramas de arquitetura
│   │   ├── system-context.md
│   │   ├── container-diagram.md
│   │   ├── event-flow.md
│   │   └── deployment.md
│   ├── bd/                          # Schemas de banco (seus arquivos atuais)
│   │   ├── 00_shared_infrastructure.sql
│   │   ├── 01_user_service.sql
│   │   ├── 02_catalog_service.sql
│   │   ├── 03_cart_service.sql
│   │   ├── 04_order_service.sql
│   │   ├── 05_payment_service.sql
│   │   └── 06_coupon_service.sql
│   ├── api/                         # Documentação de APIs
│   │   ├── openapi/
│   │   └── postman/
│   ├── events/                      # Catálogo de eventos
│   │   ├── event-catalog.md
│   │   └── event-schemas/
│   └── runbooks/                    # Guias operacionais
│       ├── deployment.md
│       ├── monitoring.md
│       └── troubleshooting.md
│
├── src/
│   │
│   ├── BuildingBlocks/              # Código compartilhado entre serviços
│   │   │
│   │   ├── Common/
│   │   │   ├── Common.Domain/
│   │   │   │   ├── Entities/
│   │   │   │   │   ├── Entity.cs
│   │   │   │   │   ├── AggregateRoot.cs
│   │   │   │   │   └── IAuditableEntity.cs
│   │   │   │   ├── Events/
│   │   │   │   │   ├── IDomainEvent.cs
│   │   │   │   │   └── DomainEventBase.cs
│   │   │   │   ├── Repositories/
│   │   │   │   │   └── IRepository.cs
│   │   │   │   ├── Specifications/
│   │   │   │   │   └── ISpecification.cs
│   │   │   │   └── ValueObjects/
│   │   │   │       ├── Money.cs
│   │   │   │       ├── Address.cs
│   │   │   │       └── Email.cs
│   │   │   │
│   │   │   ├── Common.Application/
│   │   │   │   ├── Behaviors/
│   │   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   │   └── TransactionBehavior.cs
│   │   │   │   ├── DTOs/
│   │   │   │   │   ├── PagedResult.cs
│   │   │   │   │   └── Result.cs
│   │   │   │   ├── Exceptions/
│   │   │   │   │   ├── NotFoundException.cs
│   │   │   │   │   ├── ValidationException.cs
│   │   │   │   │   └── BusinessRuleException.cs
│   │   │   │   ├── Mappings/
│   │   │   │   │   └── IMapFrom.cs
│   │   │   │   └── Interfaces/
│   │   │   │       ├── ICurrentUser.cs
│   │   │   │       └── IDateTime.cs
│   │   │   │
│   │   │   └── Common.Infrastructure/
│   │   │       ├── Persistence/
│   │   │       │   ├── BaseDbContext.cs
│   │   │       │   ├── Configurations/
│   │   │       │   │   └── AuditableEntityConfiguration.cs
│   │   │       │   └── Repositories/
│   │   │       │       └── RepositoryBase.cs
│   │   │       ├── Services/
│   │   │       │   ├── DateTimeService.cs
│   │   │       │   └── CurrentUserService.cs
│   │   │       ├── Outbox/
│   │   │       │   ├── OutboxMessage.cs
│   │   │       │   ├── IOutboxRepository.cs
│   │   │       │   └── OutboxProcessor.cs
│   │   │       └── Inbox/
│   │   │           ├── InboxMessage.cs
│   │   │           └── IInboxRepository.cs
│   │   │
│   │   ├── EventBus/
│   │   │   ├── EventBus.Abstractions/
│   │   │   │   ├── IEventBus.cs
│   │   │   │   ├── IIntegrationEventHandler.cs
│   │   │   │   └── IntegrationEvent.cs
│   │   │   │
│   │   │   ├── EventBus.RabbitMQ/
│   │   │   │   ├── RabbitMQEventBus.cs
│   │   │   │   ├── RabbitMQConnection.cs
│   │   │   │   └── Extensions/
│   │   │   │
│   │   │   └── EventBus.AzureServiceBus/
│   │   │       └── AzureServiceBusEventBus.cs
│   │   │
│   │   ├── WebHost/
│   │   │   └── WebHost.Customization/
│   │   │       ├── Extensions/
│   │   │       │   ├── ServiceCollectionExtensions.cs
│   │   │       │   └── ApplicationBuilderExtensions.cs
│   │   │       ├── Filters/
│   │   │       │   ├── HttpGlobalExceptionFilter.cs
│   │   │       │   └── ValidateModelStateFilter.cs
│   │   │       └── Middleware/
│   │   │           ├── RequestLoggingMiddleware.cs
│   │   │           └── CorrelationIdMiddleware.cs
│   │   │
│   │   └── Observability/
│   │       ├── Observability.Abstractions/
│   │       │   └── IMetricsService.cs
│   │       │
│   │       └── Observability.OpenTelemetry/
│   │           ├── OpenTelemetryExtensions.cs
│   │           └── MetricsService.cs
│   │
│   ├── Services/
│   │   │
│   │   ├── User/                    # User Service
│   │   │   ├── User.API/
│   │   │   │   ├── Controllers/
│   │   │   │   │   ├── AccountController.cs
│   │   │   │   │   ├── ProfileController.cs
│   │   │   │   │   ├── AddressController.cs
│   │   │   │   │   └── NotificationController.cs
│   │   │   │   ├── Extensions/
│   │   │   │   │   └── ServiceCollectionExtensions.cs
│   │   │   │   ├── IntegrationEvents/
│   │   │   │   │   ├── Events/
│   │   │   │   │   │   ├── UserRegisteredIntegrationEvent.cs
│   │   │   │   │   │   ├── UserProfileUpdatedIntegrationEvent.cs
│   │   │   │   │   │   └── AddressAddedIntegrationEvent.cs
│   │   │   │   │   └── Handlers/
│   │   │   │   ├── Grpc/
│   │   │   │   │   └── Services/
│   │   │   │   │       └── UserGrpcService.cs
│   │   │   │   ├── appsettings.json
│   │   │   │   ├── appsettings.Development.json
│   │   │   │   ├── Dockerfile
│   │   │   │   └── Program.cs
│   │   │   │
│   │   │   ├── User.Application/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── RegisterUser/
│   │   │   │   │   │   ├── RegisterUserCommand.cs
│   │   │   │   │   │   ├── RegisterUserCommandHandler.cs
│   │   │   │   │   │   └── RegisterUserCommandValidator.cs
│   │   │   │   │   ├── UpdateProfile/
│   │   │   │   │   ├── AddAddress/
│   │   │   │   │   └── UpdateNotificationPreferences/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetUserProfile/
│   │   │   │   │   │   ├── GetUserProfileQuery.cs
│   │   │   │   │   │   └── GetUserProfileQueryHandler.cs
│   │   │   │   │   ├── GetUserAddresses/
│   │   │   │   │   └── GetUserNotifications/
│   │   │   │   ├── DTOs/
│   │   │   │   │   ├── UserProfileDto.cs
│   │   │   │   │   ├── AddressDto.cs
│   │   │   │   │   └── NotificationDto.cs
│   │   │   │   ├── Mappings/
│   │   │   │   │   └── UserMappingProfile.cs
│   │   │   │   ├── Services/
│   │   │   │   │   ├── IUserService.cs
│   │   │   │   │   └── UserService.cs
│   │   │   │   ├── Validators/
│   │   │   │   └── DependencyInjection.cs
│   │   │   │
│   │   │   └── Payment.Tests/
│   │   │
│   │   └── Coupon/                  # Coupon Service
│   │       ├── Coupon.API/
│   │       │   ├── Controllers/
│   │       │   │   └── CouponsController.cs
│   │       │   ├── IntegrationEvents/
│   │       │   │   ├── Events/
│   │       │   │   │   ├── CouponCreatedIntegrationEvent.cs
│   │       │   │   │   ├── CouponUsedIntegrationEvent.cs
│   │       │   │   │   └── CouponExpiredIntegrationEvent.cs
│   │       │   │   └── Handlers/
│   │       │   │       └── OrderCreatedIntegrationEventHandler.cs
│   │       │   ├── Grpc/
│   │       │   │   └── Services/
│   │       │   │       └── CouponGrpcService.cs
│   │       │   ├── BackgroundServices/
│   │       │   │   ├── CouponExpirationService.cs
│   │       │   │   └── CouponReservationCleanupService.cs
│   │       │   ├── Dockerfile
│   │       │   └── Program.cs
│   │       │
│   │       ├── Coupon.Application/
│   │       │   ├── Commands/
│   │       │   │   ├── CreateCoupon/
│   │       │   │   ├── ValidateCoupon/
│   │       │   │   ├── ApplyCoupon/
│   │       │   │   ├── ReleaseCoupon/
│   │       │   │   └── DeactivateCoupon/
│   │       │   ├── Queries/
│   │       │   │   ├── GetCoupon/
│   │       │   │   ├── GetUserCoupons/
│   │       │   │   └── GetCouponMetrics/
│   │       │   └── DTOs/
│   │       │
│   │       ├── Coupon.Domain/
│   │       │   ├── AggregatesModel/
│   │       │   │   └── CouponAggregate/
│   │       │   │       ├── Coupon.cs
│   │       │   │       ├── CouponUsage.cs
│   │       │   │       ├── CouponReservation.cs
│   │       │   │       ├── CouponType.cs
│   │       │   │       ├── CouponScope.cs
│   │       │   │       └── ICouponRepository.cs
│   │       │   ├── Events/
│   │       │   ├── Exceptions/
│   │       │   └── Services/
│   │       │       └── CouponValidationService.cs
│   │       │
│   │       ├── Coupon.Infrastructure/
│   │       │   ├── Persistence/
│   │       │   │   ├── CouponDbContext.cs
│   │       │   │   ├── Configurations/
│   │       │   │   ├── Repositories/
│   │       │   │   └── Migrations/
│   │       │   └── DependencyInjection.cs
│   │       │
│   │       └── Coupon.Tests/
│   │
│   └── ApiGateways/
│       │
│       ├── Web.Bff/                 # Backend for Frontend - Web
│       │   ├── Controllers/
│       │   ├── Aggregators/
│       │   │   ├── ProductAggregatorService.cs
│       │   │   ├── CheckoutAggregatorService.cs
│       │   │   └── UserProfileAggregatorService.cs
│       │   ├── appsettings.json
│       │   ├── Dockerfile
│       │   └── Program.cs
│       │
│       ├── Mobile.Bff/              # Backend for Frontend - Mobile
│       │   └── (similar structure)
│       │
│       └── Admin.Bff/               # Backend for Frontend - Admin
│           └── (similar structure)
│
├── tests/
│   ├── LoadTests/
│   │   ├── k6/
│   │   └── jmeter/
│   ├── E2E/
│   │   └── Playwright/
│   └── PerformanceTests/
│
├── infra/                           # Infrastructure as Code
│   │
│   ├── kubernetes/
│   │   ├── base/
│   │   │   ├── namespaces/
│   │   │   ├── configmaps/
│   │   │   ├── secrets/
│   │   │   └── services/
│   │   ├── overlays/
│   │   │   ├── development/
│   │   │   ├── staging/
│   │   │   └── production/
│   │   └── helm/
│   │       ├── user-service/
│   │       ├── catalog-service/
│   │       ├── cart-service/
│   │       ├── order-service/
│   │       ├── payment-service/
│   │       └── coupon-service/
│   │
│   ├── terraform/
│   │   ├── modules/
│   │   │   ├── aks/
│   │   │   ├── postgres/
│   │   │   ├── redis/
│   │   │   ├── rabbitmq/
│   │   │   └── monitoring/
│   │   ├── environments/
│   │   │   ├── dev/
│   │   │   ├── staging/
│   │   │   └── production/
│   │   └── variables.tf
│   │
│   ├── docker/
│   │   ├── docker-compose.yml
│   │   ├── docker-compose.override.yml
│   │   └── docker-compose.production.yml
│   │
│   └── scripts/
│       ├── setup-database.sh
│       ├── seed-data.sh
│       ├── backup-database.sh
│       └── deploy.sh
│
├── monitoring/
│   ├── grafana/
│   │   ├── dashboards/
│   │   │   ├── services-overview.json
│   │   │   ├── business-metrics.json
│   │   │   └── infrastructure.json
│   │   └── provisioning/
│   ├── prometheus/
│   │   ├── prometheus.yml
│   │   └── alerts/
│   ├── jaeger/
│   │   └── jaeger-config.yml
│   └── elasticsearch/
│       └── logstash-pipelines/
│
├── scripts/
│   ├── migrations/
│   │   ├── run-all-migrations.sh
│   │   └── rollback-migrations.sh
│   ├── db/
│   │   ├── backup.sh
│   │   ├── restore.sh
│   │   └── seed-test-data.sql
│   └── deployment/
│       ├── deploy-all.sh
│       ├── deploy-service.sh
│       └── rollback.sh
│
├── .editorconfig
├── .gitignore
├── .dockerignore
├── Directory.Build.props             # Shared MSBuild properties
├── Directory.Packages.props          # Central Package Management
├── global.json
├── nuget.config
├── README.md
├── CHANGELOG.md
├── CONTRIBUTING.md
└── ecommerce-platform.sln
```

## Características-chave desta estrutura:

### 1. **BuildingBlocks** (Código Compartilhado)
- **Common**: Classes base, interfaces e padrões compartilhados
- **EventBus**: Abstração de mensageria (RabbitMQ, Azure Service Bus)
- **WebHost**: Middleware, filtros e extensões HTTP compartilhadas
- **Observability**: Telemetria, métricas e tracing

### 2. **Clean Architecture por Serviço**
Cada microsserviço segue Clean Architecture com 4 camadas:
- **API**: Controllers, gRPC, webhooks, background services
- **Application**: CQRS (Commands/Queries), DTOs, validators
- **Domain**: Agregados, entidades, eventos de domínio, regras de negócio
- **Infrastructure**: Persistência, integrações externas, implementações

### 3. **Padrões Implementados**
- ✅ **CQRS** (Command Query Responsibility Segregation)
- ✅ **Event Sourcing** (Outbox/Inbox Pattern)
- ✅ **Domain-Driven Design** (Aggregates, Value Objects, Domain Events)
- ✅ **Clean Architecture** (separação clara de responsabilidades)
- ✅ **API Gateway Pattern** (BFFs para diferentes clientes)
- ✅ **Repository Pattern**
- ✅ **Unit of Work Pattern**
- ✅ **Specification Pattern**

### 4. **Comunicação entre Serviços**
- **Assíncrona**: Event Bus (RabbitMQ/Azure Service Bus)
- **Síncrona**: gRPC para comunicação service-to-service
- **BFF**: Agregadores para otimizar chamadas do frontend

### 5. **Observabilidade**
- **Logs**: Elasticsearch + Logstash + Kibana (ELK)
- **Métricas**: Prometheus + Grafana
- **Tracing**: Jaeger/OpenTelemetry
- **Health Checks**: ASP.NET Core Health Checks

### 6. **DevOps e CI/CD**
- **IaC**: Terraform + Kubernetes (Helm Charts)
- **Containers**: Docker + Docker Compose
- **CI/CD**: GitHub Actions (por serviço)
- **Environments**: Dev, Staging, Production

### 7. **Testes**
- **Unit Tests**: xUnit por serviço
- **Integration Tests**: TestContainers
- **E2E Tests**: Playwright
- **Load Tests**: k6 ou JMeter

### 8. **Segurança**
- ASP.NET Core Identity (User Service)
- JWT Authentication
- API Gateway (rate limiting, auth)
- Secrets Management (Azure Key Vault / Kubernetes Secrets)

---

## Próximos Passos Recomendados:

1. **Implementar BuildingBlocks primeiro** - código compartilhado
2. **Começar com User Service** - fundação do sistema
3. **Catalog Service** - core do e-commerce
4. **Iterativamente adicionar os demais serviços**
5. **Configurar Observability desde o início**
6. **Automatizar CI/CD**ection.cs
│   │   │   │
│   │   │   ├── User.Domain/
│   │   │   │   ├── AggregatesModel/
│   │   │   │   │   ├── UserAggregate/
│   │   │   │   │   │   ├── User.cs
│   │   │   │   │   │   ├── UserProfile.cs
│   │   │   │   │   │   ├── Address.cs
│   │   │   │   │   │   ├── UserNotification.cs
│   │   │   │   │   │   ├── UserSession.cs
│   │   │   │   │   │   └── IUserRepository.cs
│   │   │   │   ├── Events/
│   │   │   │   │   ├── UserRegisteredDomainEvent.cs
│   │   │   │   │   ├── ProfileUpdatedDomainEvent.cs
│   │   │   │   │   └── AddressAddedDomainEvent.cs
│   │   │   │   ├── Exceptions/
│   │   │   │   │   ├── UserDomainException.cs
│   │   │   │   │   └── InvalidEmailException.cs
│   │   │   │   └── Services/
│   │   │   │
│   │   │   ├── User.Infrastructure/
│   │   │   │   ├── Persistence/
│   │   │   │   │   ├── UserDbContext.cs
│   │   │   │   │   ├── Configurations/
│   │   │   │   │   │   ├── UserConfiguration.cs
│   │   │   │   │   │   ├── UserProfileConfiguration.cs
│   │   │   │   │   │   ├── AddressConfiguration.cs
│   │   │   │   │   │   └── OutboxConfiguration.cs
│   │   │   │   │   ├── Repositories/
│   │   │   │   │   │   ├── UserRepository.cs
│   │   │   │   │   │   └── OutboxRepository.cs
│   │   │   │   │   └── Migrations/
│   │   │   │   ├── Identity/
│   │   │   │   │   ├── ApplicationUser.cs
│   │   │   │   │   ├── ApplicationRole.cs
│   │   │   │   │   └── IdentityExtensions.cs
│   │   │   │   ├── Services/
│   │   │   │   │   ├── TokenService.cs
│   │   │   │   │   └── EmailService.cs
│   │   │   │   └── DependencyInjection.cs
│   │   │   │
│   │   │   └── User.Tests/
│   │   │       ├── Unit/
│   │   │       │   ├── Domain/
│   │   │       │   └── Application/
│   │   │       ├── Integration/
│   │   │       └── Fixtures/
│   │   │
│   │   ├── Catalog/                 # Catalog Service
│   │   │   ├── Catalog.API/
│   │   │   │   ├── Controllers/
│   │   │   │   │   ├── CategoriesController.cs
│   │   │   │   │   ├── ProductsController.cs
│   │   │   │   │   ├── StockController.cs
│   │   │   │   │   └── ReviewsController.cs
│   │   │   │   ├── IntegrationEvents/
│   │   │   │   │   ├── Events/
│   │   │   │   │   │   ├── ProductCreatedIntegrationEvent.cs
│   │   │   │   │   │   ├── ProductPriceChangedIntegrationEvent.cs
│   │   │   │   │   │   ├── StockUpdatedIntegrationEvent.cs
│   │   │   │   │   │   └── ProductOutOfStockIntegrationEvent.cs
│   │   │   │   │   └── Handlers/
│   │   │   │   │       └── OrderPlacedIntegrationEventHandler.cs
│   │   │   │   ├── Grpc/
│   │   │   │   │   └── Services/
│   │   │   │   │       ├── CatalogGrpcService.cs
│   │   │   │   │       └── StockGrpcService.cs
│   │   │   │   ├── BackgroundServices/
│   │   │   │   │   ├── StockReservationCleanupService.cs
│   │   │   │   │   └── ProductStatsRefreshService.cs
│   │   │   │   ├── Dockerfile
│   │   │   │   └── Program.cs
│   │   │   │
│   │   │   ├── Catalog.Application/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateProduct/
│   │   │   │   │   ├── UpdateProduct/
│   │   │   │   │   ├── UpdateStock/
│   │   │   │   │   ├── ReserveStock/
│   │   │   │   │   └── ReleaseStock/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetProducts/
│   │   │   │   │   ├── GetProductById/
│   │   │   │   │   ├── SearchProducts/
│   │   │   │   │   └── GetProductReviews/
│   │   │   │   ├── DTOs/
│   │   │   │   └── Services/
│   │   │   │
│   │   │   ├── Catalog.Domain/
│   │   │   │   ├── AggregatesModel/
│   │   │   │   │   ├── CategoryAggregate/
│   │   │   │   │   │   ├── Category.cs
│   │   │   │   │   │   └── ICategoryRepository.cs
│   │   │   │   │   ├── ProductAggregate/
│   │   │   │   │   │   ├── Product.cs
│   │   │   │   │   │   ├── ProductImage.cs
│   │   │   │   │   │   ├── ProductReview.cs
│   │   │   │   │   │   ├── StockReservation.cs
│   │   │   │   │   │   ├── ProductStatus.cs
│   │   │   │   │   │   └── IProductRepository.cs
│   │   │   │   ├── Events/
│   │   │   │   │   ├── ProductCreatedDomainEvent.cs
│   │   │   │   │   ├── StockChangedDomainEvent.cs
│   │   │   │   │   └── PriceChangedDomainEvent.cs
│   │   │   │   └── Exceptions/
│   │   │   │
│   │   │   ├── Catalog.Infrastructure/
│   │   │   │   ├── Persistence/
│   │   │   │   │   ├── CatalogDbContext.cs
│   │   │   │   │   ├── Configurations/
│   │   │   │   │   ├── Repositories/
│   │   │   │   │   └── Migrations/
│   │   │   │   ├── Services/
│   │   │   │   │   ├── ImageStorageService.cs
│   │   │   │   │   └── SearchService.cs
│   │   │   │   └── DependencyInjection.cs
│   │   │   │
│   │   │   └── Catalog.Tests/
│   │   │
│   │   ├── Cart/                    # Cart Service
│   │   │   ├── Cart.API/
│   │   │   │   ├── Controllers/
│   │   │   │   │   └── CartController.cs
│   │   │   │   ├── IntegrationEvents/
│   │   │   │   │   ├── Events/
│   │   │   │   │   │   ├── CartCreatedIntegrationEvent.cs
│   │   │   │   │   │   ├── CartAbandonedIntegrationEvent.cs
│   │   │   │   │   │   └── CartConvertedIntegrationEvent.cs
│   │   │   │   │   └── Handlers/
│   │   │   │   │       ├── ProductPriceChangedIntegrationEventHandler.cs
│   │   │   │   │       └── UserRegisteredIntegrationEventHandler.cs
│   │   │   │   ├── Grpc/
│   │   │   │   ├── BackgroundServices/
│   │   │   │   │   ├── AbandonedCartCleanupService.cs
│   │   │   │   │   └── CartExpirationService.cs
│   │   │   │   ├── Dockerfile
│   │   │   │   └── Program.cs
│   │   │   │
│   │   │   ├── Cart.Application/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── AddItemToCart/
│   │   │   │   │   ├── RemoveItemFromCart/
│   │   │   │   │   ├── UpdateItemQuantity/
│   │   │   │   │   ├── ApplyCoupon/
│   │   │   │   │   └── MergeCart/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetCart/
│   │   │   │   │   └── GetAbandonedCarts/
│   │   │   │   └── DTOs/
│   │   │   │
│   │   │   ├── Cart.Domain/
│   │   │   │   ├── AggregatesModel/
│   │   │   │   │   └── CartAggregate/
│   │   │   │   │       ├── Cart.cs
│   │   │   │   │       ├── CartItem.cs
│   │   │   │   │       ├── CartStatus.cs
│   │   │   │   │       └── ICartRepository.cs
│   │   │   │   ├── Events/
│   │   │   │   └── Exceptions/
│   │   │   │
│   │   │   ├── Cart.Infrastructure/
│   │   │   │   ├── Persistence/
│   │   │   │   │   ├── CartDbContext.cs
│   │   │   │   │   ├── Configurations/
│   │   │   │   │   ├── Repositories/
│   │   │   │   │   └── Migrations/
│   │   │   │   ├── Redis/
│   │   │   │   │   └── RedisCacheService.cs
│   │   │   │   └── DependencyInjection.cs
│   │   │   │
│   │   │   └── Cart.Tests/
│   │   │
│   │   ├── Order/                   # Order Service
│   │   │   ├── Order.API/
│   │   │   │   ├── Controllers/
│   │   │   │   │   ├── OrdersController.cs
│   │   │   │   │   ├── TrackingController.cs
│   │   │   │   │   └── InvoiceController.cs
│   │   │   │   ├── IntegrationEvents/
│   │   │   │   │   ├── Events/
│   │   │   │   │   │   ├── OrderCreatedIntegrationEvent.cs
│   │   │   │   │   │   ├── OrderPaidIntegrationEvent.cs
│   │   │   │   │   │   ├── OrderShippedIntegrationEvent.cs
│   │   │   │   │   │   ├── OrderDeliveredIntegrationEvent.cs
│   │   │   │   │   │   └── OrderCancelledIntegrationEvent.cs
│   │   │   │   │   └── Handlers/
│   │   │   │   │       ├── PaymentCapturedIntegrationEventHandler.cs
│   │   │   │   │       └── PaymentFailedIntegrationEventHandler.cs
│   │   │   │   ├── Grpc/
│   │   │   │   ├── BackgroundServices/
│   │   │   │   │   ├── OrderStatusUpdateService.cs
│   │   │   │   │   └── PendingOrderTimeoutService.cs
│   │   │   │   ├── Dockerfile
│   │   │   │   └── Program.cs
│   │   │   │
│   │   │   ├── Order.Application/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateOrder/
│   │   │   │   │   ├── ConfirmOrder/
│   │   │   │   │   ├── ShipOrder/
│   │   │   │   │   ├── CancelOrder/
│   │   │   │   │   └── RefundOrder/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetOrder/
│   │   │   │   │   ├── GetUserOrders/
│   │   │   │   │   └── GetOrderTracking/
│   │   │   │   └── DTOs/
│   │   │   │
│   │   │   ├── Order.Domain/
│   │   │   │   ├── AggregatesModel/
│   │   │   │   │   └── OrderAggregate/
│   │   │   │   │       ├── Order.cs
│   │   │   │   │       ├── OrderItem.cs
│   │   │   │   │       ├── OrderStatus.cs
│   │   │   │   │       ├── PaymentMethod.cs
│   │   │   │   │       ├── ShippingMethod.cs
│   │   │   │   │       └── IOrderRepository.cs
│   │   │   │   ├── Events/
│   │   │   │   ├── Exceptions/
│   │   │   │   └── Services/
│   │   │   │       └── OrderingDomainService.cs
│   │   │   │
│   │   │   ├── Order.Infrastructure/
│   │   │   │   ├── Persistence/
│   │   │   │   │   ├── OrderDbContext.cs
│   │   │   │   │   ├── Configurations/
│   │   │   │   │   ├── Repositories/
│   │   │   │   │   └── Migrations/
│   │   │   │   ├── Services/
│   │   │   │   │   ├── ShippingService.cs
│   │   │   │   │   └── InvoiceService.cs
│   │   │   │   └── DependencyInjection.cs
│   │   │   │
│   │   │   └── Order.Tests/
│   │   │
│   │   ├── Payment/                 # Payment Service
│   │   │   ├── Payment.API/
│   │   │   │   ├── Controllers/
│   │   │   │   │   ├── PaymentsController.cs
│   │   │   │   │   ├── WebhooksController.cs
│   │   │   │   │   └── PaymentMethodsController.cs
│   │   │   │   ├── IntegrationEvents/
│   │   │   │   │   ├── Events/
│   │   │   │   │   │   ├── PaymentCreatedIntegrationEvent.cs
│   │   │   │   │   │   ├── PaymentCapturedIntegrationEvent.cs
│   │   │   │   │   │   ├── PaymentFailedIntegrationEvent.cs
│   │   │   │   │   │   └── PaymentRefundedIntegrationEvent.cs
│   │   │   │   │   └── Handlers/
│   │   │   │   │       └── OrderCreatedIntegrationEventHandler.cs
│   │   │   │   ├── Grpc/
│   │   │   │   ├── BackgroundServices/
│   │   │   │   │   └── PaymentExpirationService.cs
│   │   │   │   ├── Dockerfile
│   │   │   │   └── Program.cs
│   │   │   │
│   │   │   ├── Payment.Application/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreatePayment/
│   │   │   │   │   ├── CapturePayment/
│   │   │   │   │   ├── RefundPayment/
│   │   │   │   │   └── ProcessWebhook/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetPayment/
│   │   │   │   │   └── GetUserPaymentMethods/
│   │   │   │   └── DTOs/
│   │   │   │
│   │   │   ├── Payment.Domain/
│   │   │   │   ├── AggregatesModel/
│   │   │   │   │   ├── PaymentAggregate/
│   │   │   │   │   │   ├── Payment.cs
│   │   │   │   │   │   ├── PaymentTransaction.cs
│   │   │   │   │   │   ├── PaymentStatus.cs
│   │   │   │   │   │   ├── PaymentMethod.cs
│   │   │   │   │   │   └── IPaymentRepository.cs
│   │   │   │   │   └── PaymentMethodAggregate/
│   │   │   │   │       ├── UserPaymentMethod.cs
│   │   │   │   │       └── IPaymentMethodRepository.cs
│   │   │   │   ├── Events/
│   │   │   │   ├── Exceptions/
│   │   │   │   └── Services/
│   │   │   │       └── IPaymentGateway.cs
│   │   │   │
│   │   │   ├── Payment.Infrastructure/
│   │   │   │   ├── Persistence/
│   │   │   │   │   ├── PaymentDbContext.cs
│   │   │   │   │   ├── Configurations/
│   │   │   │   │   ├── Repositories/
│   │   │   │   │   └── Migrations/
│   │   │   │   ├── Gateways/
│   │   │   │   │   ├── Stripe/
│   │   │   │   │   │   └── StripePaymentGateway.cs
│   │   │   │   │   ├── PagarMe/
│   │   │   │   │   │   └── PagarMePaymentGateway.cs
│   │   │   │   │   └── MercadoPago/
│   │   │   │   │       └── MercadoPagoPaymentGateway.cs
│   │   │   │   └── DependencyInj