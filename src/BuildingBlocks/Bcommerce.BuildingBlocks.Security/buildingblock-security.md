# Documentação Bcommerce.BuildingBlocks.Security

Este projeto centraliza a lógica de segurança, focando principalmente na geração de tokens JWT e facilitação do acesso aos dados do usuário autenticado.

## Sumário

1. [Autenticação (JWT)](#1-autenticação-jwt)
2. [Autorização (Politicas)](#2-autorização-politicas)
3. [Extensões (ClaimsPrincipal)](#3-extensões-claimsprincipal)

---

## 1. Autenticação (JWT)

**Problema:** A lógica de geração de tokens JWT envolve lidar com chaves, algoritmos de assinatura e claims padrão, o que pode ser repetitivo e sensível a erros se duplicado.
**Solução:** `ITokenGenerator` encapsula essa complexidade.

### JwtSettings
Classe de configuração simples para mapear a seção `JwtSettings` do `appsettings.json`.

```json
"JwtSettings": {
  "Secret": "minha-chave-secreta-super-segura-e-longa",
  "ExpiryMinutes": 60,
  "Issuer": "Bcommerce",
  "Audience": "Bcommerce"
}
```

### JwtTokenGenerator
Implementação que gera o token assinado.

**Exemplo de Uso (no Serviço de Identity):**

```csharp
public class AuthService
{
    private readonly ITokenGenerator _tokenGenerator;

    public AuthService(ITokenGenerator tokenGenerator)
    {
        _tokenGenerator = tokenGenerator;
    }

    public string Login(User user)
    {
        // Gera o token com ID, Email e Roles
        var token = _tokenGenerator.GenerateToken(
            userId: user.Id, 
            email: user.Email, 
            roles: new[] { "Customer" }
        );
        
        return token;
    }
}
```

---

## 2. Autorização (Políticas)

**Problema:** O uso de "Magic Strings" para nomes de políticas e roles espalhados pelos Controllers torna a refatoração difícil.
**Solução:** `PolicyNames` centraliza essas constantes.

**Exemplo de Uso:**

```csharp
[Authorize(Policy = PolicyNames.AdminPolicy)]
[HttpPost]
public IActionResult CriarProduto() { ... }
```

---

## 3. Extensões (ClaimsPrincipal)

**Problema:** Para obter o ID do usuário logado em um Controller, é necessário acessar `User.Claims`, filtrar pelo tipo certo e converter para Guid, repetidamente.
**Solução:** Métodos de extensão em `ClaimsPrincipalExtensions`.

**Exemplo de Uso (no Controller):**

```csharp
[ApiController]
[Route("api/pedidos")]
public class PedidosController : ControllerBase
{
    [HttpPost]
    public IActionResult CriarPedido()
    {
        // Obtém o ID do usuário diretamente do token de forma tipada e segura
        Guid userId = User.GetUserId();
        string email = User.GetEmail();

        _service.CriarPedido(userId);
        
        return Ok();
    }
}
```

**Benefícios:**
*   Lança exceções claras (`UnauthorizedAccessException`) se o claim não existir.
*   Centraliza a lógica de parsing (string -> Guid).
