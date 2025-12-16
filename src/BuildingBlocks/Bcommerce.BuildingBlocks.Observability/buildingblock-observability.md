# Documentação Bcommerce.BuildingBlocks.Observability

Este projeto padroniza a coleta de telemetria da plataforma, integrando Logs, Métricas e Tracing Distribuído.

## Sumário

1. [Logging (Serilog)](#1-logging-serilog)
2. [Métricas (OpenTelemetry + Prometheus)](#2-métricas-opentelemetry--prometheus)
3. [Tracing (OpenTelemetry + Jaeger)](#3-tracing-opentelemetry--jaeger)

---

## 1. Logging (Serilog)

**Problema:** Logs inconsistentes entre serviços dificultam a depuração centralizada (ex: Elastic Stack ou Seq).
**Solução:** `LoggingConfiguration` configura o Serilog com enriquecedores padrão (Environment, ApplicationName) e saídas (Console, File).

**Configuração (Program.cs):**
```csharp
builder.Host.UseCustomSerilog();
```

**Uso:**
Use `ILogger<T>` normalmente. O Serilog intercepta automaticamente.
```csharp
_logger.LogInformation("Processando pedido {PedidoId}", id);
```

---

## 2. Métricas (OpenTelemetry + Prometheus)

**Problema:** Falta de visibilidade sobre performance (RPS, Latência) e métricas de negócio.
**Solução:** `MetricsConfiguration` expõe o endpoint `/metrics` para scraping do Prometheus. `CustomMetrics` facilita a criação de contadores de negócio.

**Configuração (Program.cs):**
```csharp
// Adiciona serviços
builder.AddCustomMetrics();

// ... build ...

// Adiciona endpoint /metrics
app.UseCustomMetrics();
```

**Uso (Métricas de Negócio):**
```csharp
var contadorVendas = CustomMetrics.CriarContador("vendas_total", "Total de vendas realizadas");
contadorVendas.Add(1);
```

---

## 3. Tracing (OpenTelemetry + Jaeger)

**Problema:** Perder a rastreabilidade de uma requisição que passa por múltiplos microsserviços via HTTP ou RabbitMQ.
**Solução:** `TracingConfiguration` injeta instrumentação automática para ASP.NET Core e HttpClient.

**Configuração (Program.cs):**
```csharp
builder.AddCustomTracing();
```

**Uso:**
A instrumentação é automática. Se precisar criar Spans manuais para blocos de código internos:
```csharp
using var activity = new ActivitySource("MinhaAtividade").StartActivity("ProcessamentoComplexo");
activity?.AdicionarTag("usuario_id", userId);
// código...
```
