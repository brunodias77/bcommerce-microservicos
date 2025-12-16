# Documentação Bcommerce.BuildingBlocks.Web

Este projeto padroniza a camada de apresentação (API) dos microsserviços, garantindo respostas uniformes, tratamento de erros centralizado e logging consistente.

## Sumário

1. [Modelos de Resposta Padronizados](#1-modelos-de-resposta-padronizados)
2. [Middlewares (Pipeline)](#2-middlewares-pipeline)
3. [Filtros (MVC)](#3-filtros-mvc)
4. [Configuração Simplificada](#4-configuração-simplificada)

---

## 1. Modelos de Resposta Padronizados

**Problema:** Cada endpoint retornar estruturas diferentes (às vezes só o objeto, às vezes um envelope, erros com formatos variados) dificulta o consumo pelo frontend.
**Solução:** `ApiResponse<T>` e `ErrorResponse` garantem que toda resposta siga o mesmo formato.

**Estrutura da Resposta:**
```json
{
  "success": true,
  "data": { ... },
  "error": null
}
```
ou em caso de erro:
```json
{
  "success": false,
  "data": null,
  "error": {
    "message": "Mensagem amigável",
    "code": "ERROR_CODE",
    "validationErrors": [ ... ]
  }
}
```

**Uso no Controller:**
Você não precisa retornar `ApiResponse` manualmente se usar os Filtros ou Middleware, mas pode usar os helpers:
```csharp
return Ok(ApiResponse<Produto>.Ok(produto));
// ou
return BadRequest(ApiResponse<object>.Fail("Estoque insuficiente"));
```

---

## 2. Middlewares (Pipeline)

Interceptam todas as requisições HTTP antes de chegarem aos Controllers.

### ExceptionHandlingMiddleware
Captura **qualquer** exceção não tratada na aplicação.
*   Converte `DomainException` -> 400 Bad Request
*   Converte `NotFoundException` -> 404 Not Found
*   Outras -> 500 Internal Server Error
*   Retorna sempre um JSON padronizado (`ApiResponse`).

### RequestLoggingMiddleware
Mede o tempo de execução de cada requisição e loga o método, caminho e status code.
**Exemplo de Log:** `Requisição finalizada: GET /api/produtos - Status: 200 - Tempo: 45ms`

### CorrelationIdMiddleware
Gerencia o header `X-Correlation-Id`.
*   Se a requisição já tem o header, usa ele.
*   Se não tem, gera um novo `Guid`.
*   Adiciona o ID no `TraceIdentifier` para que todos os logs dessa requisição compartilhem o mesmo ID rastreável.

---

## 3. Filtros (MVC)

Atuam especificamente no contexto dos Controllers.

### ValidationFilter
Substitui a validação padrão do ASP.NET Core (`ModelState`).
Se um DTO (InputModel) for inválido (FluenteValidation ou DataAnnotations), este filtro intercepta a requisição **antes** de entrar na Action e retorna um 400 com a lista de erros padronizada em `ErrorResponse`.

### ApiExceptionFilter
Uma alternativa/complemento ao Middleware para capturar exceções que ocorrem durante a execução da Action, permitindo manipulação mais fina do `IActionResult` se necessário. Atualmente configurado globalmente.

---

## 4. Configuração Simplificada

Utilize as extensões para configurar tudo com poucas linhas no `Program.cs`.

**Exemplo de Configuração:**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Adiciona Controllers com Filtros Globais e NewtonsoftJson
builder.Services.AddCustomControllers();

// Adiciona Middlewares ao DI
builder.Services.AddCustomMiddleware();

var app = builder.Build();

// Configura o Pipeline na ordem correta
app.UseCustomMiddleware();

app.MapControllers();
app.Run();
```
