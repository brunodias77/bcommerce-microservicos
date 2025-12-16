# Estrutura de Pastas - E-commerce Microservices

```
Bcommerce.Solution/
│
├── src/
│   │
│   ├── BuildingBlocks/
│   │   │
│   │   ├── Bcommerce.BuildingBlocks.Core/
│   │   │   ├── Domain/
│   │   │   │   ├── Entity.cs
│   │   │   │   ├── AggregateRoot.cs
│   │   │   │   ├── ValueObject.cs
│   │   │   │   ├── IEntity.cs
│   │   │   │   ├── IAggregateRoot.cs
│   │   │   │   ├── IDomainEvent.cs
│   │   │   │   └── DomainEvent.cs
│   │   │   │
│   │   │   ├── Application/
│   │   │   │   ├── ICommand.cs
│   │   │   │   ├── ICommandHandler.cs
│   │   │   │   ├── IQuery.cs
│   │   │   │   ├── IQueryHandler.cs
│   │   │   │   ├── Result.cs
│   │   │   │   ├── ValidationError.cs
│   │   │   │   └── PaginatedList.cs
│   │   │   │
│   │   │   ├── Exceptions/
│   │   │   │   ├── DomainException.cs
│   │   │   │   ├── NotFoundException.cs
│   │   │   │   ├── ValidationException.cs
│   │   │   │   ├── ConcurrencyException.cs
│   │   │   │   └── BusinessRuleException.cs
│   │   │   │
│   │   │   └── Guards/
│   │   │       ├── Guard.cs
│   │   │       └── GuardExtensions.cs
│   │   │
│   │   ├── Bcommerce.BuildingBlocks.Infrastructure/
│   │   │   ├── Data/
│   │   │   │   ├── BaseDbContext.cs
│   │   │   │   ├── IUnitOfWork.cs
│   │   │   │   ├── UnitOfWork.cs
│   │   │   │   ├── IRepository.cs
│   │   │   │   ├── Repository.cs
│   │   │   │   └── EntityConfigurations/
│   │   │   │       ├── EntityBaseConfiguration.cs
│   │   │   │       └── AggregateRootConfiguration.cs
│   │   │   │
│   │   │   ├── Outbox/
│   │   │   │   ├── OutboxMessage.cs
│   │   │   │   ├── IOutboxMessageRepository.cs
│   │   │   │   ├── OutboxMessageRepository.cs
│   │   │   │   ├── OutboxProcessor.cs
│   │   │   │   └── IOutboxPublisher.cs
│   │   │   │
│   │   │   ├── Inbox/
│   │   │   │   ├── InboxMessage.cs
│   │   │   │   ├── IInboxMessageRepository.cs
│   │   │   │   ├── InboxMessageRepository.cs
│   │   │   │   └── InboxProcessor.cs
│   │   │   │
│   │   │   └── AuditLog/
│   │   │       ├── AuditLog.cs
│   │   │       ├── IAuditLogRepository.cs
│   │   │       ├── AuditLogRepository.cs
│   │   │       └── AuditLogInterceptor.cs
│   │   │
│   │   ├── Bcommerce.BuildingBlocks.Messaging/
│   │   │   ├── Abstractions/
│   │   │   │   ├── IIntegrationEvent.cs
│   │   │   │   ├── IntegrationEvent.cs
│   │   │   │   ├── IEventBus.cs
│   │   │   │   └── IMessagePublisher.cs
│   │   │   │
│   │   │   ├── MassTransit/
│   │   │   │   ├── MassTransitConfiguration.cs
│   │   │   │   ├── MassTransitEventBus.cs
│   │   │   │   ├── OutboxPublisher.cs
│   │   │   │   └── Filters/
│   │   │   │       ├── IdempotencyFilter.cs
│   │   │   │       ├── LoggingFilter.cs
│   │   │   │       └── RetryFilter.cs
│   │   │   │
│   │   │   └── Events/
│   │   │       └── README.md (shared event contracts)
│   │   │
│   │   ├── Bcommerce.BuildingBlocks.Web/
│   │   │   ├── Middleware/
│   │   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   │   ├── RequestLoggingMiddleware.cs
│   │   │   │   └── CorrelationIdMiddleware.cs
│   │   │   │
│   │   │   ├── Filters/
│   │   │   │   ├── ValidationFilter.cs
│   │   │   │   └── ApiExceptionFilter.cs
│   │   │   │
│   │   │   ├── Extensions/
│   │   │   │   ├── ServiceCollectionExtensions.cs
│   │   │   │   └── ApplicationBuilderExtensions.cs
│   │   │   │
│   │   │   └── Models/
│   │   │       ├── ApiResponse.cs
│   │   │       ├── PaginatedResponse.cs
│   │   │       └── ErrorResponse.cs
│   │   │
│   │   ├── Bcommerce.BuildingBlocks.Security/
│   │   │   ├── Authentication/
│   │   │   │   ├── JwtSettings.cs
│   │   │   │   ├── JwtTokenGenerator.cs
│   │   │   │   └── ITokenGenerator.cs
│   │   │   │
│   │   │   ├── Authorization/
│   │   │   │   ├── Policies/
│   │   │   │   │   ├── PolicyNames.cs
│   │   │   │   │   └── Requirements/
│   │   │   │   └── Handlers/
│   │   │   │
│   │   │   └── Extensions/
│   │   │       └── ClaimsPrincipalExtensions.cs
│   │   │
│   │   ├── Bcommerce.BuildingBlocks.Caching/
│   │   │   ├── ICacheService.cs
│   │   │   ├── RedisCacheService.cs
│   │   │   ├── MemoryCacheService.cs
│   │   │   └── CacheSettings.cs
│   │   │
│   │   └── Bcommerce.BuildingBlocks.Observability/
│   │       ├── Logging/
│   │       │   ├── LoggingConfiguration.cs
│   │       │   └── SerilogEnrichers/
│   │       │
│   │       ├── Metrics/
│   │       │   ├── MetricsConfiguration.cs
│   │       │   └── CustomMetrics.cs
│   │       │
│   │       └── Tracing/
│   │           ├── TracingConfiguration.cs
│   │           └── ActivityExtensions.cs
│   │
│   ├── Services/
│   │   │
│   │   ├── User/
│   │   │   ├── ECommerce.User.API/
│   │   │   │   ├── Controllers/
│   │   │   │   │   ├── AuthController.cs
│   │   │   │   │   ├── UsersController.cs
│   │   │   │   │   ├── ProfilesController.cs
│   │   │   │   │   ├── AddressesController.cs
│   │   │   │   │   └── NotificationsController.cs
│   │   │   │   │
│   │   │   │   ├── Extensions/
│   │   │   │   │   └── ServiceCollectionExtensions.cs
│   │   │   │   │
│   │   │   │   ├── Program.cs
│   │   │   │   ├── appsettings.json
│   │   │   │   └── appsettings.Development.json
│   │   │   │
│   │   │   ├── ECommerce.User.Application/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── Auth/
│   │   │   │   │   │   ├── RegisterUser/
│   │   │   │   │   │   │   ├── RegisterUserCommand.cs
│   │   │   │   │   │   │   ├── RegisterUserCommandHandler.cs
│   │   │   │   │   │   │   └── RegisterUserCommandValidator.cs
│   │   │   │   │   │   ├── Login/
│   │   │   │   │   │   ├── ConfirmEmail/
│   │   │   │   │   │   ├── ResetPassword/
│   │   │   │   │   │   └── RefreshToken/
│   │   │   │   │   │
│   │   │   │   │   ├── Profile/
│   │   │   │   │   │   ├── UpdateProfile/
│   │   │   │   │   │   └── UploadAvatar/
│   │   │   │   │   │
│   │   │   │   │   └── Address/
│   │   │   │   │       ├── AddAddress/
│   │   │   │   │       ├── UpdateAddress/
│   │   │   │   │       ├── DeleteAddress/
│   │   │   │   │       └── SetDefaultAddress/
│   │   │   │   │
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetUserById/
│   │   │   │   │   ├── GetUserProfile/
│   │   │   │   │   ├── GetUserAddresses/
│   │   │   │   │   └── GetNotifications/
│   │   │   │   │
│   │   │   │   ├── EventHandlers/
│   │   │   │   │   ├── OrderPlaced/
│   │   │   │   │   │   └── OrderPlacedEventHandler.cs
│   │   │   │   │   └── PaymentCompleted/
│   │   │   │   │       └── PaymentCompletedEventHandler.cs
│   │   │   │   │
│   │   │   │   ├── DTOs/
│   │   │   │   │   ├── UserDto.cs
│   │   │   │   │   ├── ProfileDto.cs
│   │   │   │   │   ├── AddressDto.cs
│   │   │   │   │   └── NotificationDto.cs
│   │   │   │   │
│   │   │   │   ├── Mappings/
│   │   │   │   │   └── UserMappingProfile.cs
│   │   │   │   │
│   │   │   │   └── Validators/
│   │   │   │       └── (FluentValidation validators)
│   │   │   │
│   │   │   ├── ECommerce.User.Domain/
│   │   │   │   ├── Users/
│   │   │   │   │   ├── User.cs (ApplicationUser : IdentityUser)
│   │   │   │   │   ├── UserProfile.cs
│   │   │   │   │   ├── Address.cs
│   │   │   │   │   ├── UserSession.cs
│   │   │   │   │   ├── UserNotification.cs
│   │   │   │   │   └── Events/
│   │   │   │   │       ├── UserRegisteredEvent.cs
│   │   │   │   │       ├── ProfileUpdatedEvent.cs
│   │   │   │   │       └── AddressAddedEvent.cs
│   │   │   │   │
│   │   │   │   ├── ValueObjects/
│   │   │   │   │   ├── Cpf.cs
│   │   │   │   │   └── PostalCode.cs
│   │   │   │   │
│   │   │   │   └── Repositories/
│   │   │   │       ├── IUserRepository.cs
│   │   │   │       ├── IAddressRepository.cs
│   │   │   │       └── INotificationRepository.cs
│   │   │   │
│   │   │   └── ECommerce.User.Infrastructure/
│   │   │       ├── Data/
│   │   │       │   ├── UserDbContext.cs
│   │   │       │   ├── Migrations/
│   │   │       │   └── Configurations/
│   │   │       │       ├── UserConfiguration.cs
│   │   │       │       ├── ProfileConfiguration.cs
│   │   │       │       └── AddressConfiguration.cs
│   │   │       │
│   │   │       ├── Repositories/
│   │   │       │   ├── UserRepository.cs
│   │   │       │   ├── AddressRepository.cs
│   │   │       │   └── NotificationRepository.cs
│   │   │       │
│   │   │       ├── Identity/
│   │   │       │   ├── IdentityConfiguration.cs
│   │   │       │   └── CustomUserStore.cs
│   │   │       │
│   │   │       └── Extensions/
│   │   │           └── ServiceCollectionExtensions.cs
│   │   │
│   │   ├── Catalog/
│   │   │   ├── ECommerce.Catalog.API/
│   │   │   │   ├── Controllers/
│   │   │   │   │   ├── CategoriesController.cs
│   │   │   │   │   ├── ProductsController.cs
│   │   │   │   │   ├── ReviewsController.cs
│   │   │   │   │   └── StockController.cs
│   │   │   │   │
│   │   │   │   ├── Program.cs
│   │   │   │   └── appsettings.json
│   │   │   │
│   │   │   ├── ECommerce.Catalog.Application/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── Products/
│   │   │   │   │   │   ├── CreateProduct/
│   │   │   │   │   │   ├── UpdateProduct/
│   │   │   │   │   │   ├── DeleteProduct/
│   │   │   │   │   │   └── UpdateStock/
│   │   │   │   │   │
│   │   │   │   │   ├── Categories/
│   │   │   │   │   │   ├── CreateCategory/
│   │   │   │   │   │   ├── UpdateCategory/
│   │   │   │   │   │   └── DeleteCategory/
│   │   │   │   │   │
│   │   │   │   │   └── Reviews/
│   │   │   │   │       ├── CreateReview/
│   │   │   │   │       └── ApproveReview/
│   │   │   │   │
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetProducts/
│   │   │   │   │   ├── GetProductById/
│   │   │   │   │   ├── SearchProducts/
│   │   │   │   │   ├── GetCategories/
│   │   │   │   │   └── GetProductReviews/
│   │   │   │   │
│   │   │   │   ├── EventHandlers/
│   │   │   │   │   ├── OrderCreated/
│   │   │   │   │   │   └── ReserveStockHandler.cs
│   │   │   │   │   └── OrderCancelled/
│   │   │   │   │       └── ReleaseStockHandler.cs
│   │   │   │   │
│   │   │   │   └── DTOs/
│   │   │   │       ├── ProductDto.cs
│   │   │   │       ├── CategoryDto.cs
│   │   │   │       └── ReviewDto.cs
│   │   │   │
│   │   │   ├── ECommerce.Catalog.Domain/
│   │   │   │   ├── Products/
│   │   │   │   │   ├── Product.cs
│   │   │   │   │   ├── ProductImage.cs
│   │   │   │   │   ├── ProductReview.cs
│   │   │   │   │   ├── StockMovement.cs
│   │   │   │   │   ├── StockReservation.cs
│   │   │   │   │   └── Events/
│   │   │   │   │       ├── ProductCreatedEvent.cs
│   │   │   │   │       ├── StockUpdatedEvent.cs
│   │   │   │   │       └── PriceChangedEvent.cs
│   │   │   │   │
│   │   │   │   ├── Categories/
│   │   │   │   │   ├── Category.cs
│   │   │   │   │   └── Events/
│   │   │   │   │
│   │   │   │   ├── ValueObjects/
│   │   │   │   │   ├── Money.cs
│   │   │   │   │   ├── Sku.cs
│   │   │   │   │   └── Dimensions.cs
│   │   │   │   │
│   │   │   │   └── Repositories/
│   │   │   │       ├── IProductRepository.cs
│   │   │   │       ├── ICategoryRepository.cs
│   │   │   │       └── IStockRepository.cs
│   │   │   │
│   │   │   └── ECommerce.Catalog.Infrastructure/
│   │   │       ├── Data/
│   │   │       │   ├── CatalogDbContext.cs
│   │   │       │   ├── Migrations/
│   │   │       │   └── Configurations/
│   │   │       │
│   │   │       └── Repositories/
│   │   │           ├── ProductRepository.cs
│   │   │           ├── CategoryRepository.cs
│   │   │           └── StockRepository.cs
│   │   │
│   │   ├── Cart/
│   │   │   ├── ECommerce.Cart.API/
│   │   │   │   ├── Controllers/
│   │   │   │   │   └── CartsController.cs
│   │   │   │   │
│   │   │   │   ├── Program.cs
│   │   │   │   └── appsettings.json
│   │   │   │
│   │   │   ├── ECommerce.Cart.Application/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── AddItem/
│   │   │   │   │   ├── RemoveItem/
│   │   │   │   │   ├── UpdateQuantity/
│   │   │   │   │   ├── ApplyCoupon/
│   │   │   │   │   └── ClearCart/
│   │   │   │   │
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetCart/
│   │   │   │   │   └── GetCartSummary/
│   │   │   │   │
│   │   │   │   ├── EventHandlers/
│   │   │   │   │   ├── ProductPriceChanged/
│   │   │   │   │   │   └── UpdateCartPricesHandler.cs
│   │   │   │   │   └── ProductStockChanged/
│   │   │   │   │       └── ValidateCartStockHandler.cs
│   │   │   │   │
│   │   │   │   └── DTOs/
│   │   │   │       ├── CartDto.cs
│   │   │   │       └── CartItemDto.cs
│   │   │   │
│   │   │   ├── ECommerce.Cart.Domain/
│   │   │   │   ├── Carts/
│   │   │   │   │   ├── Cart.cs
│   │   │   │   │   ├── CartItem.cs
│   │   │   │   │   └── Events/
│   │   │   │   │       ├── CartCreatedEvent.cs
│   │   │   │   │       ├── ItemAddedEvent.cs
│   │   │   │   │       └── CartAbandonedEvent.cs
│   │   │   │   │
│   │   │   │   └── Repositories/
│   │   │   │       └── ICartRepository.cs
│   │   │   │
│   │   │   └── ECommerce.Cart.Infrastructure/
│   │   │       ├── Data/
│   │   │       │   ├── CartDbContext.cs
│   │   │       │   └── Configurations/
│   │   │       │
│   │   │       └── Repositories/
│   │   │           └── CartRepository.cs
│   │   │
│   │   ├── Order/
│   │   │   ├── ECommerce.Order.API/
│   │   │   │   ├── Controllers/
│   │   │   │   │   ├── OrdersController.cs
│   │   │   │   │   └── TrackingController.cs
│   │   │   │   │
│   │   │   │   ├── Program.cs
│   │   │   │   └── appsettings.json
│   │   │   │
│   │   │   ├── ECommerce.Order.Application/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateOrder/
│   │   │   │   │   ├── UpdateOrderStatus/
│   │   │   │   │   ├── CancelOrder/
│   │   │   │   │   └── AddTrackingInfo/
│   │   │   │   │
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetOrder/
│   │   │   │   │   ├── GetUserOrders/
│   │   │   │   │   └── GetOrderTracking/
│   │   │   │   │
│   │   │   │   ├── EventHandlers/
│   │   │   │   │   ├── PaymentCompleted/
│   │   │   │   │   │   └── ConfirmOrderHandler.cs
│   │   │   │   │   └── PaymentFailed/
│   │   │   │   │       └── CancelOrderHandler.cs
│   │   │   │   │
│   │   │   │   └── DTOs/
│   │   │   │       ├── OrderDto.cs
│   │   │   │       └── OrderItemDto.cs
│   │   │   │
│   │   │   ├── ECommerce.Order.Domain/
│   │   │   │   ├── Orders/
│   │   │   │   │   ├── Order.cs
│   │   │   │   │   ├── OrderItem.cs
│   │   │   │   │   ├── OrderStatusHistory.cs
│   │   │   │   │   ├── TrackingEvent.cs
│   │   │   │   │   └── Events/
│   │   │   │   │       ├── OrderCreatedEvent.cs
│   │   │   │   │       ├── OrderPaidEvent.cs
│   │   │   │   │       └── OrderShippedEvent.cs
│   │   │   │   │
│   │   │   │   ├── ValueObjects/
│   │   │   │   │   ├── OrderNumber.cs
│   │   │   │   │   └── ShippingAddress.cs
│   │   │   │   │
│   │   │   │   └── Repositories/
│   │   │   │       └── IOrderRepository.cs
│   │   │   │
│   │   │   └── ECommerce.Order.Infrastructure/
│   │   │       ├── Data/
│   │   │       │   ├── OrderDbContext.cs
│   │   │       │   └── Configurations/
│   │   │       │
│   │   │       └── Repositories/
│   │   │           └── OrderRepository.cs
│   │   │
│   │   ├── Payment/
│   │   │   ├── ECommerce.Payment.API/
│   │   │   │   ├── Controllers/
│   │   │   │   │   ├── PaymentsController.cs
│   │   │   │   │   ├── PaymentMethodsController.cs
│   │   │   │   │   └── WebhooksController.cs
│   │   │   │   │
│   │   │   │   ├── Program.cs
│   │   │   │   └── appsettings.json
│   │   │   │
│   │   │   ├── ECommerce.Payment.Application/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── ProcessPayment/
│   │   │   │   │   ├── CapturePayment/
│   │   │   │   │   ├── RefundPayment/
│   │   │   │   │   └── SavePaymentMethod/
│   │   │   │   │
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetPayment/
│   │   │   │   │   └── GetPaymentMethods/
│   │   │   │   │
│   │   │   │   ├── EventHandlers/
│   │   │   │   │   └── OrderCreated/
│   │   │   │   │       └── CreatePaymentHandler.cs
│   │   │   │   │
│   │   │   │   ├── Services/
│   │   │   │   │   ├── IPaymentGateway.cs
│   │   │   │   │   ├── StripeGateway.cs
│   │   │   │   │   └── PagarMeGateway.cs
│   │   │   │   │
│   │   │   │   └── DTOs/
│   │   │   │       └── PaymentDto.cs
│   │   │   │
│   │   │   ├── ECommerce.Payment.Domain/
│   │   │   │   ├── Payments/
│   │   │   │   │   ├── Payment.cs
│   │   │   │   │   ├── PaymentTransaction.cs
│   │   │   │   │   ├── PaymentMethod.cs
│   │   │   │   │   └── Events/
│   │   │   │   │       ├── PaymentCreatedEvent.cs
│   │   │   │   │       ├── PaymentCompletedEvent.cs
│   │   │   │   │       └── PaymentFailedEvent.cs
│   │   │   │   │
│   │   │   │   └── Repositories/
│   │   │   │       └── IPaymentRepository.cs
│   │   │   │
│   │   │   └── ECommerce.Payment.Infrastructure/
│   │   │       ├── Data/
│   │   │       │   ├── PaymentDbContext.cs
│   │   │       │   └── Configurations/
│   │   │       │
│   │   │       ├── Repositories/
│   │   │       │   └── PaymentRepository.cs
│   │   │       │
│   │   │       └── Gateways/
│   │   │           ├── StripeGatewayImplementation.cs
│   │   │           └── PagarMeGatewayImplementation.cs
│   │   │
│   │   └── Coupon/
│   │       ├── ECommerce.Coupon.API/
│   │       │   ├── Controllers/
│   │       │   │   └── CouponsController.cs
│   │       │   │
│   │       │   ├── Program.cs
│   │       │   └── appsettings.json
│   │       │
│   │       ├── ECommerce.Coupon.Application/
│   │       │   ├── Commands/
│   │       │   │   ├── CreateCoupon/
│   │       │   │   ├── UpdateCoupon/
│   │       │   │   ├── ValidateCoupon/
│   │       │   │   └── ApplyCoupon/
│   │       │   │
│   │       │   ├── Queries/
│   │       │   │   ├── GetCoupon/
│   │       │   │   └── GetActiveCoupons/
│   │       │   │
│   │       │   ├── EventHandlers/
│   │       │   │   └── OrderCreated/
│   │       │   │       └── RecordCouponUsageHandler.cs
│   │       │   │
│   │       │   └── DTOs/
│   │       │       └── CouponDto.cs
│   │       │
│   │       ├── ECommerce.Coupon.Domain/
│   │       │   ├── Coupons/
│   │       │   │   ├── Coupon.cs
│   │       │   │   ├── CouponUsage.cs
│   │       │   │   ├── CouponReservation.cs
│   │       │   │   └── Events/
│   │       │   │       ├── CouponCreatedEvent.cs
│   │       │   │       └── CouponUsedEvent.cs
│   │       │   │
│   │       │   ├── ValueObjects/
│   │       │   │   └── CouponCode.cs
│   │       │   │
│   │       │   └── Repositories/
│   │       │       └── ICouponRepository.cs
│   │       │
│   │       └── ECommerce.Coupon.Infrastructure/
│   │           ├── Data/
│   │           │   ├── CouponDbContext.cs
│   │           │   └── Configurations/
│   │           │
│   │           └── Repositories/
│   │               └── CouponRepository.cs
│   │
│   ├── ApiGateway/
│   │   └── ECommerce.Gateway/
│   │       ├── Configurations/
│   │       │   └── ocelot.json
│   │       │
│   │       ├── Program.cs
│   │       └── appsettings.json
│   │
│   └── Contracts/
│       └── ECommerce.Contracts/
│           ├── User/
│           │   ├── Events/
│           │   │   ├── UserRegisteredEvent.cs
│           │   │   ├── ProfileUpdatedEvent.cs
│           │   │   └── AddressAddedEvent.cs
│           │   │
│           │   └── Requests/
│           │       └── ValidateUserRequest.cs
│           │
│           ├── Catalog/
│           │   ├── Events/
│           │   │   ├── ProductCreatedEvent.cs
│           │   │   ├── StockUpdatedEvent.cs
│           │   │   └── PriceChangedEvent.cs
│           │   │
│           │   └── Requests/
│           │       ├── ReserveStockRequest.cs
│           │       └── GetProductRequest.cs
│           │
│           ├── Cart/
│           │   ├── Events/
│           │   │   ├── CartCreatedEvent.cs
│           │   │   ├── ItemAddedEvent.cs
│           │   │   └── CartAbandonedEvent.cs
│           │   │
│           │   └── Requests/
│           │       └── ValidateCartRequest.cs
│           │
│           ├── Order/
│           │   ├── Events/
│           │   │   ├── OrderCreatedEvent.cs
│           │   │   ├── OrderPaidEvent.cs
│           │   │   ├── OrderShippedEvent.cs
│           │   │   ├── OrderDeliveredEvent.cs
│           │   │   └── OrderCancelledEvent.cs
│           │   │
│           │   └── Requests/
│           │       └── CreateOrderRequest.cs
│           │
│           ├── Payment/
│           │   ├── Events/
│           │   │   ├── PaymentCreatedEvent.cs
│           │   │   ├── PaymentCompletedEvent.cs
│           │   │   ├── PaymentFailedEvent.cs
│           │   │   └── PaymentRefundedEvent.cs
│           │   │
│           │   └── Requests/
│           │       └── ProcessPaymentRequest.cs
│           │
│           └── Coupon/
│               ├── Events/
│               │   ├── CouponCreatedEvent.cs
│               │   └── CouponUsedEvent.cs
│               │
│               └── Requests/
│                   └── ValidateCouponRequest.cs
│
├── tests/
│   ├── BuildingBlocks/
│   │   ├── Bcommerce.BuildingBlocks.Core.Tests/
│   │   ├── Bcommerce.BuildingBlocks.Infrastructure.Tests/
│   │   └── Bcommerce.BuildingBlocks.Messaging.Tests/
│   │
│   ├── Services/
│   │   ├── User/
│   │   │   ├── ECommerce.User.Domain.Tests/
│   │   │   ├── ECommerce.User.Application.Tests/
│   │   │   └── ECommerce.User.API.Tests/
│   │   │
│   │   ├── Catalog/
│   │   │   ├── ECommerce.Catalog.Domain.Tests/
│   │   │   ├── ECommerce.Catalog.Application.Tests/
│   │   │   └── ECommerce.Catalog.API.Tests/
│   │   │
│   │   ├── Cart/
│   │   │   ├── ECommerce.Cart.Domain.Tests/
│   │   │   ├── ECommerce.Cart.Application.Tests/
│   │   │   └── ECommerce.Cart.API.Tests/
│   │   │
│   │   ├── Order/
│   │   │   ├── ECommerce.Order.Domain.Tests/
│   │   │   ├── ECommerce.Order.Application.Tests/
│   │   │   └── ECommerce.Order.API.Tests/
│   │   │
│   │   ├── Payment/
│   │   │   ├── ECommerce.Payment.Domain.Tests/
│   │   │   ├── ECommerce.Payment.Application.Tests/
│   │   │   └── ECommerce.Payment.API.Tests/
│   │   │
│   │   └── Coupon/
│   │       ├── ECommerce.Coupon.Domain.Tests/
│   │       ├── ECommerce.Coupon.Application.Tests/
│   │       └── ECommerce.Coupon.API.Tests/
│   │
│   └── Integration/
│       ├── ECommerce.IntegrationTests/
│       │   ├── Scenarios/
│       │   │   ├── CheckoutFlowTests.cs
│       │   │   ├── OrderPlacementTests.cs
│       │   │   └── PaymentProcessingTests.cs
│       │   │
│       │   └── Fixtures/
│       │       └── WebApplicationFactoryFixture.cs
│       │
│       └── ECommerce.LoadTests/
│           └── (K6 ou Artillery scripts)
│
├── tools/
│   ├── scripts/
│   │   ├── setup-databases.sh
│   │   ├── run-migrations.sh
│   │   ├── seed-data.sh
│   │   └── docker-compose-local.sh
│   │
│   └── docker/
│       ├── postgres/
│       │   └── init-scripts/
│       │
│       ├── rabbitmq/
│       │   └── rabbitmq.conf
│       │
│       └── redis/
│           └── redis.conf
│
├── docs/
│   ├── architecture/
│   │   ├── overview.md
│   │   ├── microservices.md
│   │   ├── messaging.md
│   │   └── data-consistency.md
│   │
│   ├── api/
│   │   ├── user-api.md
│   │   ├── catalog-api.md
│   │   ├── cart-api.md
│   │   ├── order-api.md
│   │   ├── payment-api.md
│   │   └── coupon-api.md
│   │
│   └── db/
│       ├── 00_shared_infrastructure.sql
│       ├── 01_user_service.sql
│       ├── 02_catalog_service.sql
│       ├── 03_cart_service.sql
│       ├── 04_order_service.sql
│       ├── 05_payment_service.sql
│       └── 06_coupon_service.sql
│
├── docker-compose.yml
├── docker-compose.override.yml
├── .dockerignore
├── .gitignore
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── nuget.config
├── ECommerce.Solution.sln
└── README.md
```

## Estrutura de Pacotes NuGet Recomendados

### BuildingBlocks.Core
```xml
<PackageReference Include="FluentValidation" />
<PackageReference Include="Ardalis.GuardClauses" />
<PackageReference Include="MediatR" />
```

### BuildingBlocks.Infrastructure
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
<PackageReference Include="EFCore.NamingConventions" />
<PackageReference Include="Dapper" />
<PackageReference Include="Polly" />
```

### BuildingBlocks.Messaging
```xml
<PackageReference Include="MassTransit" />
<PackageReference Include="MassTransit.RabbitMQ" />
<PackageReference Include="MassTransit.EntityFrameworkCore" />
```

### BuildingBlocks.Web
```xml
<PackageReference Include="Swashbuckle.AspNetCore" />
<PackageReference Include="FluentValidation.AspNetCore" />
<PackageReference Include="Hellang.Middleware.ProblemDetails" />
```

### BuildingBlocks.Security
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />
```

### BuildingBlocks.Caching
```xml
<PackageReference Include="StackExchange.Redis" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" />
```

### BuildingBlocks.Observability
```xml
<PackageReference Include="Serilog.AspNetCore" />
<PackageReference Include="Serilog.Sinks.Seq" />
<PackageReference Include="OpenTelemetry" />
<PackageReference Include="OpenTelemetry.Exporter.Jaeger" />
```

## Características Principais

✅ **Clean Architecture** em cada microsserviço
✅ **CQRS** com MediatR
✅ **Event-Driven** com MassTransit + RabbitMQ
✅ **Outbox/Inbox Pattern** para garantia de entrega
✅ **Domain-Driven Design** com agregados bem definidos
✅ **Optimistic Locking** via version control
✅ **Audit Log** em todos os serviços
✅ **Cross-Service Communication** via eventos
✅ **API Gateway** com Ocelot
✅ **Shared Contracts** para type safety
✅ **Observability** completa (logs, metrics, traces)