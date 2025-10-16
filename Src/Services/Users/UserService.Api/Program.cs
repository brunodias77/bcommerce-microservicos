// ============================================================================
// B-Commerce User Management API - Program.cs
// ============================================================================
// Este arquivo é o ponto de entrada principal da aplicação ASP.NET Core.
// Utiliza o padrão Minimal API do .NET 6+ que simplifica a configuração
// e inicialização da aplicação, eliminando a necessidade das classes
// Startup.cs e Program.cs separadas do .NET Framework.
// ============================================================================

using UserService.Api.Configurations;
using UserService.Api.Extensions;
using UserService.Api.Middlewares;

// ============================================================================
// FASE 1: CRIAÇÃO DO BUILDER
// ============================================================================
// O WebApplicationBuilder é responsável por configurar todos os serviços
// que serão utilizados pela aplicação através do container de Injeção de Dependências.
// Aqui definimos TODOS os serviços antes de construir a aplicação.
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Registra serviços da camada de infraestrutura (Infrastructure Layer)
// Inclui: Entity Framework, repositórios, conexão com banco de dados,
// configurações de Keycloak, email, logging, migrations, etc.
builder.Services.AddInfrastructure(builder.Configuration);

// Registra serviços da camada de aplicação (Application Layer)
// Inclui: serviços de negócio, validações, DTOs, mapeamentos,
// handlers de comandos/queries, políticas de autorização, etc.
builder.Services.AddApplication(builder.Configuration);

// ============================================================================
// FASE 3: CONSTRUÇÃO DA APLICAÇÃO
// ============================================================================
// Após configurar todos os serviços, construímos a instância da aplicação.
// A partir deste ponto, o container de DI está "selado" e não pode ser modificado.
var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// ============================================================================
// FASE 4: EXECUÇÃO DE MIGRATIONS AUTOMÁTICAS
// ============================================================================
// Executa automaticamente as migrations do banco de dados na inicialização.
// IMPORTANTE: Em produção, considere executar migrations separadamente
// para ter maior controle sobre o processo de deploy e rollback.
InfraDependencyInjection.RunMigrations(app.Services);

// ============================================================================
// FASE 5: CONFIGURAÇÃO DO PIPELINE DE MIDDLEWARE
// ============================================================================
// O pipeline de middleware processa TODAS as requisições HTTP.
// A ORDEM é CRÍTICA - cada middleware processa na ordem definida (request)
// e na ordem INVERSA (response). Configurações diferentes por ambiente.

// AMBIENTE DE DESENVOLVIMENTO
// Configurações específicas para debugging, testes e desenvolvimento local
if (app.Environment.IsDevelopment())
{
    // Habilita Swagger UI, páginas de erro detalhadas e ferramentas de debug
    // NUNCA deve ser usado em produção por questões de segurança
    app.ConfigureDevelopmentPipeline();
    
    // Executa testes de integração básicos durante a inicialização
    // Verifica conectividade com banco de dados e injeção de dependências
    // Ajuda a detectar problemas de configuração antes de receber requisições
    await app.RunIntegrationTestsAsync();
}
else
{
    // AMBIENTE DE PRODUÇÃO
    // Configurações otimizadas para performance, segurança e estabilidade
    // Inclui tratamento genérico de erros e HSTS para forçar HTTPS
    app.ConfigureProductionPipeline();
}

// ============================================================================
// FASE 6: CONFIGURAÇÃO DE SEGURANÇA E MIDDLEWARE PRINCIPAL
// ============================================================================
// Esta seção utiliza METHOD CHAINING (encadeamento de métodos) para
// configurar o pipeline principal de middleware de forma fluente e legível.
// Cada método retorna a instância da aplicação, permitindo o encadeamento.

app.UseSecurityHeaders()                    // 1º: Cabeçalhos de segurança HTTP (XSS, CSRF, etc.)
   .ConfigureMiddlewarePipeline()           // 2º: Pipeline principal (HTTPS, CORS, Auth, etc.)
   .ConfigureStartupLogging(builder.Configuration); // 4º: Logging de inicialização

// ============================================================================
// FASE 7: INICIALIZAÇÃO E TRATAMENTO DE EXCEÇÕES
// ============================================================================
// Inicia o servidor web e fica escutando requisições HTTP.
// O try-catch captura erros CRÍTICOS de inicialização que impediriam
// a aplicação de iniciar (porta ocupada, configurações inválidas, etc.)

try
{
    // Inicia o servidor web e bloqueia a thread principal
    // A aplicação ficará rodando até receber um sinal de parada (Ctrl+C, SIGTERM, etc.)
    app.Run();
}
catch (Exception ex)
{
    // Captura erros críticos de inicialização
    // Registra o erro e re-lança para que o processo termine com código de erro
    // Isso é essencial para containers Docker e orquestradores como Kubernetes
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "Falha crítica na inicialização da aplicação B-Commerce User API");
    
    // Re-lança a exceção para que o processo termine com exit code != 0
    // Isso sinaliza para o ambiente de execução que houve uma falha
    throw;
}















//
// // ============================================================================
// // B-Commerce User Management API - Program.cs
// // ============================================================================
// // Este arquivo é o ponto de entrada principal da aplicação ASP.NET Core.
// // Utiliza o padrão Minimal API do .NET 6+ que simplifica a configuração
// // e inicialização da aplicação, eliminando a necessidade das classes
// // Startup.cs e Program.cs separadas do .NET Framework.
// // ============================================================================
//
// using UserService.Api.Configurations;
// using UserService.Api.Middlewares;
//
// // ============================================================================
// // FASE 1: CRIAÇÃO DO BUILDER
// // ============================================================================
// // O WebApplicationBuilder é responsável por configurar todos os serviços
// // que serão utilizados pela aplicação através do container de Injeção de Dependências.
// // Aqui definimos TODOS os serviços antes de construir a aplicação.
// var builder = WebApplication.CreateBuilder(args);
//
// // Add services to the container.
//
// builder.Services.AddControllers();
// // Registra serviços da camada de infraestrutura (Infrastructure Layer)
// // Inclui: Entity Framework, repositórios, conexão com banco de dados,
// // configurações de Keycloak, email, logging, migrations, etc.
// builder.Services.AddInfrastructure(builder.Configuration);
//
// // Registra serviços da camada de aplicação (Application Layer)
// // Inclui: serviços de negócio, validações, DTOs, mapeamentos,
// // handlers de comandos/queries, políticas de autorização, etc.
// builder.Services.AddApplication(builder.Configuration);
//
//
// // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();
//
// var app = builder.Build();
//
// app.UseMiddleware<ExceptionHandlingMiddleware>();
//
// // Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }
//
// app.UseHttpsRedirection();
//
// app.UseAuthorization();
//
// app.MapControllers();
//
// app.Run();