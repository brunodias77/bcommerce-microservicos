# Documentação Bcommerce.BuildingBlocks.Core

Este pacote contém os blocos de construção fundamentais (Kernel Compartilhado) utilizados por todos os microsserviços do BCommerce. Ele padroniza a implementação de padrões táticos do DDD, CQRS, tratamento de erros e utilitários comuns.

## Sumário

1. [Domain Patterns (DDD)](#1-domain-patterns-ddd)
2. [Application Patterns (CQRS)](#2-application-patterns-cqrs)
3. [Result Pattern](#3-result-pattern)
4. [Exceptions & Error Handling](#4-exceptions--error-handling)
5. [Guards (Defensive Programming)](#5-guards-defensive-programming)

---

## 1. Domain Patterns (DDD)

### Entity & AggregateRoot

**Problema:** Em DDD, entidades precisam ter identidade única, auditoria básica (CreatedAt/UpdatedAt) e capacidade de registrar eventos de domínio que ocorreram durante uma transação.
**Solução:** As classes base `Entity<T>` e `AggregateRoot<T>` fornecem essa infraestrutura, implementando `IEntity` e `IAggregateRoot`.

*   **Entity**: Objeto com identidade.
*   **AggregateRoot**: A entidade principal que controla o acesso e consistência de um grupo de entidades. Repositórios só devem existir para AggregateRoots.

**Exemplo Prático:**

```csharp
using Bcommerce.BuildingBlocks.Core.Domain;

// AggregateRoot com ID do tipo Guid
public class Pedido : AggregateRoot<Guid>
{
    private readonly List<ItemPedido> _itens = new();
    public IReadOnlyCollection<ItemPedido> Itens => _itens.AsReadOnly();

    public Pedido()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public void AdicionarItem(Guid produtoId, int quantidade)
    {
        // Regra de negócio...
        var item = new ItemPedido(produtoId, quantidade);
        _itens.Add(item);

        // Registra evento de domínio para efeitos colaterais (ex: notificar outro serviço/contexto)
        AddDomainEvent(new ItemAdicionadoAoPedidoEvent(Id, produtoId));
    }
}
```

### ValueObject

**Problema:** Objetos que são definidos por seus atributos e não por sua identidade (ex: Endereço, Dinheiro, CPF) precisam de implementação correta de igualdade (`Equals`, `GetHashCode`, `==`, `!=`).
**Solução:** A classe base `ValueObject` automatiza a verificação de igualdade baseada nos componentes retornados.

**Exemplo Prático:**

```csharp
using Bcommerce.BuildingBlocks.Core.Domain;

public class Endereco : ValueObject
{
    public string Rua { get; }
    public string Numero { get; }
    public string Cep { get; }

    public Endereco(string rua, string numero, string cep)
    {
        Rua = rua;
        Numero = numero;
        Cep = cep;
    }

    // Define quais propriedades compõem a igualdade/unicidade do objeto
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Rua;
        yield return Numero;
        yield return Cep;
    }
}

// Uso:
var end1 = new Endereco("Rua A", "123", "00000-000");
var end2 = new Endereco("Rua A", "123", "00000-000");
bool saoIguais = (end1 == end2); // true, pois os valores são iguais
```

### DomainEvent

**Problema:** Notificar outras partes do mesmo agregado ou microsserviço que algo importante aconteceu.
**Solução:** `IDomainEvent` e a base `DomainEvent` servem como contratos para eventos intra-processo (MediatR INotification).

**Exemplo Prático:**

```csharp
public record PedidoCriadoEvent(Guid PedidoId) : DomainEvent;
```

---

## 2. Application Patterns (CQRS)

As interfaces nesta camada simplificam o uso da biblioteca **MediatR** para implementar o padrão CQRS (Command Query Responsibility Segregation).

### ICommand & ICommandHandler

**Problema:** Operações que alteram estado (Escrita) devem ser desacopladas de quem as invoca (Controllers) e ter intenção explícita.
**Solução:** Use `ICommand<TResponse>` para definir a intenção e `ICommandHandler<TCommand, TResponse>` para executar a lógica.

**Exemplo Prático:**

```csharp
// 1. Definição do Comando (Intenção)
public record CriarProdutoCommand(string Nome, decimal Preco) : ICommand<Result<Guid>>;

// 2. Handler (Execução)
public class CriarProdutoHandler : ICommandHandler<CriarProdutoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CriarProdutoCommand request, CancellationToken token)
    {
        var produto = new Produto(request.Nome, request.Preco);
        // ... salvar no banco ...
        return Result.Ok(produto.Id);
    }
}
```

### IQuery & IQueryHandler

**Problema:** Operações de leitura não devem ter efeitos colaterais e podem ter modelos de retorno otimizados (DTOs) diferentes das entidades de domínio.
**Solução:** Use `IQuery<TResponse>` para consultas.

**Exemplo Prático:**

```csharp
public record GetProdutoPorIdQuery(Guid Id) : IQuery<Result<ProdutoDto>>;
```

### PaginatedList

**Problema:** Padronizar respostas de APIs que retornam listas grandes.
**Solução:** Classe utilitária que encapsula metadados de paginação (PageIndex, TotalPages, Items).

---

## 3. Result Pattern

**Problema:** O uso excessivo de `try-catch` para fluxo de controle (ex: validação falhou, recurso não encontrado) é custoso em performance e torna o código menos legível ("GoTo" moderno).
**Solução:** O padrão `Result` permite retornar sucesso ou falha de forma explícita, forçando quem chama a tratar o erro.

**Exemplo Prático:**

```csharp
public Result<double> CalcularDesconto(Cupom cupom)
{
    if (cupom.Expirado)
    {
        // Retorna falha sem lançar Exception
        return Result.Fail<double>("Cupom expirado.");
    }

    if (!cupom.Ativo)
    {
         return Result.Fail<double>("Cupom inativo.");
    }

    return Result.Ok(10.0); // Sucesso
}

// Consumo:
var resultado = CalcularDesconto(cupom);
if (resultado.IsFailure)
{
    Console.WriteLine(resultado.Error);
    return;
}
Console.WriteLine($"Desconto: {resultado.Value}");
```

---

## 4. Exceptions & Error Handling

Embora usemos o `Result` pattern para validações comuns, Exceptions ainda são necessárias para situações excepcionais ou erros de infraestrutura.

*   **`DomainException`**: Classe base para erros originados nas regras de negócio profundas.
*   **`NotFoundException`**: Padroniza o erro 404. Mensagem automática: `"Entity {name} ({key}) was not found."`
*   **`ValidationException`**: Integração com FluentValidation. Contém um dicionário de erros (`IDictionary<string, string[]>`) para retornar detalhes campo a campo (Erro 400).
*   **`ConcurrencyException`**: Para tratar erros de concorrência otimista (quando o `version` do registro no banco mudou antes do update).

---

## 5. Guards (Defensive Programming)

**Problema:** Validar pré-condições (ex: argumentos nulos) no início de métodos/construtores gera muito código repetitivo (`if (x == null) throw...`).
**Solução:** A classe estática `GuardExtensions` (ou `Guard`) permite validar condições de forma fluente e lançar exceções apropriadas imediatamente.

**Exemplo Prático:**

```csharp
public class Cliente
{
    public string Nome { get; }

    public Cliente(string nome)
    {
        // Lança ArgumentException automaticamente se vazio/nulo
        // A mensagem já foi traduzida para PT-BR no Core
        GuardExtensions.NullOrEmpty(nome, nameof(nome));
        
        Nome = nome;
    }
}
```
