# Documentação Bcommerce.BuildingBlocks.Caching

Este projeto fornece uma abstração unificada para cache, permitindo alternar entre implementações (Redis ou Memória) sem impactar o código de negócio.

## Sumário

1. [Abstração (ICacheService)](#1-abstração-icacheservice)
2. [Implementações](#2-implementações)
3. [Configuração](#3-configuração)

---

## 1. Abstração (ICacheService)

**Problema:** Depender diretamente de `IMemoryCache` ou `IDistributedCache` espalha detalhes de infraestrutura (como serialização JSON para Redis) por toda a aplicação.
**Solução:** `ICacheService` define operações de alto nível, já lidando com tipos genéricos `<T>`.

**Interface:**
```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}
```

**Exemplo de Uso:**

```csharp
public class ProdutoService
{
    private readonly ICacheService _cache;
    private readonly IRepository<Produto> _repo;

    public async Task<Produto> ObterProduto(Guid id)
    {
        string key = $"produto:{id}";
        
        // Tenta pegar do cache (já deserializado)
        var produto = await _cache.GetAsync<Produto>(key);
        if (produto != null) return produto;

        // Se não existir, busca no banco
        produto = await _repo.GetByIdAsync(id);
        
        // Salva no cache por 30 minutos
        if (produto != null)
        {
            await _cache.SetAsync(key, produto, TimeSpan.FromMinutes(30));
        }

        return produto;
    }
}
```

---

## 2. Implementações

### RedisCacheService
Usa `IDistributedCache` (StackExchange.Redis) por baixo dos panos.
*   **Serialização:** Usa `Newtonsoft.Json` automaticamente para converter objetos em strings antes de salvar no Redis.
*   **Tratamento de Erro:** Captura exceções de conexão (logando-as) para que o cache indisponível não derrube a aplicação (Fail-Safe), retornando `default` ou ignorando a escrita.

### MemoryCacheService
Wrapper simples sobre `IMemoryCache`. Ideal para testes locais ou dados que não precisam ser compartilhados entre instâncias de microsserviços.

---

## 3. Configuração

Para trocar de estratégia, basta mudar a Injeção de Dependência no `Program.cs`.

**Configuração Típica:**

```csharp
// Configuração
services.Configure<CacheSettings>(builder.Configuration.GetSection("CacheSettings"));

// Escolha a implementação desejada:

// Opção A: Redis (Produção)
services.AddStackExchangeRedisCache(options => ...);
services.AddScoped<ICacheService, RedisCacheService>();

// Opção B: Memória (Dev/Testes)
services.AddMemoryCache();
services.AddScoped<ICacheService, MemoryCacheService>();
```
