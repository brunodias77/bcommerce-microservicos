# Documentação Bcommerce.BuildingBlocks.Infrastructure

Este projeto implementa a infraestrutura base para persistência de dados e mensageria confiável, utilizando **Entity Framework Core** e o padrão **Transactional Outbox/Inbox**.

## Sumário

1. [Persistência de Dados (Repository & UnitOfWork)](#1-persistência-de-dados)
2. [Transactional Outbox Pattern](#2-transactional-outbox-pattern)
3. [Transactional Inbox Pattern](#3-transactional-inbox-pattern)
4. [Audit Log (Interceptor)](#4-audit-log)

---

## 1. Persistência de Dados

### BaseDbContext & EntityConfigurations

**Problema:** Configurar mapeamentos ORM manualmente em cada DbContext é repetitivo e propenso a erros. Além disso, precisamos garantir a publicação de eventos de domínio ao salvar.
**Solução:** 
*   `BaseDbContext`: Intercepta o `SaveChanges` para despachar Domain Events (através do MediatR) *antes* ou *durante* a transação (dependendo da estratégia).
*   `EntityBaseConfiguration<T>`: Padroniza configurações comuns como chaves primárias e campos obrigatórios (`CreatedAt`).

**Exemplo Prático (Criando um DbContext Específico):**

```csharp
public class CatalogDbContext : BaseDbContext
{
    public DbSet<Produto> Produtos { get; set; }

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options, IPublisher publisher) 
        : base(options, publisher) { }
}

// Configuração da Entidade
public class ProdutoConfiguration : AggregateRootConfiguration<Produto>
{
    public override void Configure(EntityTypeBuilder<Produto> builder)
    {
        base.Configure(builder);
        builder.Property(p => p.Nome).HasMaxLength(100).IsRequired();
    }
}
```

### Repository & UnitOfWork

**Problema:** O acesso direto ao DbContext espalha lógica de consulta por toda a aplicação. Salvar mudanças individuais pode causar inconsistência se múltiplas operações pertencerem à mesma transação lógica.
**Solução:**
*   `IRepository<T>`: Abstrai o acesso a dados para um Agregado específico.
*   `IUnitOfWork`: Garante que todas as alterações feitas através de múltiplos repositórios sejam persistidas atomicamente (`CommitAsync`).

**Exemplo Prático:**

```csharp
// Injeção de Dependência
public class CriarPedidoHandler : ICommandHandler<CriarPedidoCommand>
{
    private readonly IRepository<Pedido> _pedidoRepository;

    public CriarPedidoHandler(IRepository<Pedido> pedidoRepository)
    {
        _pedidoRepository = pedidoRepository;
    }

    public async Task<Unit> Handle(CriarPedidoCommand request, CancellationToken token)
    {
        var pedido = new Pedido();
        // ... lógica ...
        
        await _pedidoRepository.AddAsync(pedido, token);
        
        // Persiste tudo no banco (incluindo eventos de domínio que podem ter gerado mensagens na Outbox)
        await _pedidoRepository.UnitOfWork.CommitAsync(token);
        
        return Unit.Value;
    }
}
```

---

## 2. Transactional Outbox Pattern

**Problema:** Ao salvar uma entidade no banco e publicar um evento no RabbitMQ, se o banco salvar mas a publicação falhar (ou vice-versa), o sistema fica inconsistente.
**Solução:** Salvar o evento **na mesma transação do banco de dados** em uma tabela `OutboxMessage`. Um processo em segundo plano lê essa tabela e publica no broker.

*   `OutboxMessage`: A representação do evento salva no banco.
*   `IOutboxPublisher`: Interface usada para "publicar" (salvar) o evento na tabela.

**Exemplo Prático (Configuração):**
Normalmente, um `IDomainEvent` é convertido em `OutboxMessage` por um NotificationHandler interno ou pelo próprio `BaseDbContext` se configurado para tal.

```csharp
// Exemplo de NotificationHandler que intercepta eventos e salva na Outbox
public class OutboxNotificationHandler : INotificationHandler<DomainEvent>
{
    private readonly IOutboxMessageRepository _outboxRepo;

    public async Task Handle(DomainEvent notification, CancellationToken token)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = notification.GetType().FullName,
            Content = JsonConvert.SerializeObject(notification),
            CreatedOnUtc = DateTime.UtcNow
        };
        
        await _outboxRepo.AddAsync(message);
        // O Commit do UnitOfWork original salvará isso junto com a entidade
    }
}
```

---

## 3. Transactional Inbox Pattern

**Problema:** Garantir que uma mensagem recebida do RabbitMQ seja processada exatamente uma vez e que o processamento seja atômico (Idempotência).
**Solução:** Ao receber uma mensagem, salve-a primeiro na tabela `InboxMessage`. Processe-a localmente e marque como processada.

*   `InboxMessage`: Registro da mensagem recebida.
*   `InboxProcessor`: Lê mensagens não processadas e invoca os handlers correspondentes.

**Exemplo Prático (Worker):**

```csharp
public class InboxWorker : BackgroundService
{
    private readonly InboxProcessor _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _processor.ProcessMessagesAsync(stoppingToken);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

---

## 4. Audit Log

**Problema:** Rastrear "quem fez o que e quando" em todas as entidades do sistema para conformidade e segurança.
**Solução:** `AuditLogInterceptor` intercepta o `SaveChanges` do EF Core, compara valores antigos e novos (`OriginalValues` vs `CurrentValues`) e salva registros na tabela `AuditLog`.

**Uso:**
Basta registrar o interceptor no setup do DbContext. Ele funcionará automaticamente para todas as entidades que herdam de `Entity<T>`.

```csharp
// No Program.cs ou DependencyInjection.cs
services.AddDbContext<CatalogDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<AuditLogInterceptor>();
    options.UseNpgsql("...")
           .AddInterceptors(interceptor);
});
```
