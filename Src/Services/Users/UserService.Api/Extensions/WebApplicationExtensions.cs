using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using UserService.Application.Contracts.Keycloak;

namespace UserService.Api.Extensions;

/// <summary>
/// Extensões para configuração do pipeline de middleware da aplicação B-Commerce User Service.
/// Esta classe centraliza todas as configurações de middleware, facilitando a manutenção
/// e garantindo consistência entre diferentes ambientes (desenvolvimento/produção).
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configura o pipeline de middleware específico para ambiente de desenvolvimento.
    /// Este método habilita ferramentas de debugging e documentação que não devem
    /// estar disponíveis em produção por questões de segurança e performance.
    /// </summary>
    /// <param name="app">Instância da aplicação web</param>
    /// <returns>A mesma instância da aplicação para permitir method chaining</returns>
    public static WebApplication ConfigureDevelopmentPipeline(this WebApplication app)
    {
        // Habilita páginas de exceção detalhadas para debugging
        // Mostra stack traces completos e informações técnicas sobre erros
        // IMPORTANTE: Nunca deve ser usado em produção por expor informações sensíveis
        app.UseDeveloperExceptionPage();
        
        return app;
    }

    /// <summary>
    /// Configura o pipeline de middleware específico para ambiente de produção.
    /// Este método implementa configurações de segurança e tratamento de erros
    /// apropriadas para um ambiente público, sem expor informações sensíveis.
    /// </summary>
    /// <param name="app">Instância da aplicação web</param>
    /// <returns>A mesma instância da aplicação para permitir method chaining</returns>
    public static WebApplication ConfigureProductionPipeline(this WebApplication app)
    {
        // Configura tratamento genérico de exceções para produção
        // Redireciona todas as exceções não tratadas para um endpoint de erro personalizado
        // Evita exposição de detalhes técnicos que poderiam ser explorados maliciosamente
        app.UseExceptionHandler("/error");
        
        // Habilita HTTP Strict Transport Security (HSTS)
        // Força navegadores a sempre usarem HTTPS para acessar a aplicação
        // Protege contra ataques man-in-the-middle e downgrade de protocolo
        app.UseHsts();
        
        return app;
    }

    /// <summary>
    /// Configura cabeçalhos de segurança HTTP essenciais para proteger a aplicação
    /// contra ataques comuns como XSS, clickjacking, MIME sniffing e outros.
    /// Estes cabeçalhos implementam uma camada adicional de segurança no navegador.
    /// </summary>
    /// <param name="app">Instância da aplicação web</param>
    /// <returns>A mesma instância da aplicação para permitir method chaining</returns>
    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            // X-Content-Type-Options: Previne MIME sniffing attacks
             // Força o navegador a respeitar o Content-Type declarado pelo servidor
             context.Response.Headers["X-Content-Type-Options"] = "nosniff";
             
             // X-Frame-Options: Protege contra clickjacking attacks
             // DENY impede que a página seja carregada em qualquer frame/iframe
             context.Response.Headers["X-Frame-Options"] = "DENY";
             
             // X-XSS-Protection: Ativa proteção XSS do navegador (legacy, mas ainda útil)
             // mode=block faz o navegador bloquear a página inteira se detectar XSS
             context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
             
             // Referrer-Policy: Controla quais informações de referência são enviadas
             // strict-origin-when-cross-origin: envia referrer completo apenas para same-origin
             context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
             
             // Content-Security-Policy: Define política de segurança de conteúdo
             // Controla quais recursos podem ser carregados e executados na página
             context.Response.Headers["Content-Security-Policy"] = 
                 "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'";
            
            await next();
        });
        
        return app;
    }

    /// <summary>
    /// Configura o pipeline principal de middleware da aplicação.
    /// A ordem dos middlewares é CRÍTICA - cada middleware processa a requisição
    /// na ordem definida e a resposta na ordem inversa.
    /// </summary>
    /// <param name="app">Instância da aplicação web</param>
    /// <returns>A mesma instância da aplicação para permitir method chaining</returns>
    public static WebApplication ConfigureMiddlewarePipeline(this WebApplication app)
    {
        // 0. Swagger (apenas em desenvolvimento) - deve vir antes de HTTPS redirect
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        // 1. HTTPS Redirection: Redireciona todas as requisições HTTP para HTTPS
        // Deve vir ANTES de outros middlewares para garantir comunicação segura
        app.UseHttpsRedirection();
        
        // 2. CORS: Configura Cross-Origin Resource Sharing
        // Permite que aplicações frontend em outros domínios acessem a API
        // Deve vir ANTES da autenticação para permitir preflight requests
        app.UseCors();
        
        // 3. Authentication: Identifica quem é o usuário (JWT, cookies, etc.)
        // Deve vir ANTES da autorização para que as claims estejam disponíveis
        app.UseAuthentication();
        
        // 4. Authorization: Verifica se o usuário tem permissão para acessar o recurso
        // Deve vir DEPOIS da autenticação e ANTES do mapeamento de controllers
        app.UseAuthorization();
        
        // 5. Health Checks: Mapeia os endpoints de health check
        // Permite monitoramento da saúde da aplicação por load balancers e orquestradores
        app.MapHealthChecks("/health");
        
        // 6. Controller Mapping: Mapeia as rotas para os controllers
        // Deve ser o ÚLTIMO middleware do pipeline para processar as requisições
        app.MapControllers();
        
        return app;
    }

    /// <summary>
    /// Configura tratamento global de exceções para capturar e processar
    /// todas as exceções não tratadas da aplicação de forma centralizada.
    /// Garante que erros sejam logados adequadamente e respostas consistentes sejam retornadas.
    /// </summary>
    /// <param name="app">Instância da aplicação web</param>
    /// <returns>A mesma instância da aplicação para permitir method chaining</returns>
    public static WebApplication UseGlobalExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(appError =>
        {
            appError.Run(async context =>
            {
                // Define status HTTP 500 (Internal Server Error) para todas as exceções não tratadas
                context.Response.StatusCode = 500;
                
                // Define o tipo de conteúdo como JSON para resposta estruturada
                context.Response.ContentType = "application/json";

                // Obtém informações sobre a exceção que ocorreu
                var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (contextFeature != null)
                {
                    // Obtém o logger para registrar a exceção
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    
                    // Registra a exceção completa com stack trace para debugging
                    // IMPORTANTE: Logs detalhados ajudam na investigação de problemas
                    logger.LogError(contextFeature.Error, "Exceção não tratada ocorreu");

                    // Cria resposta padronizada para o cliente
                    // Não expõe detalhes técnicos da exceção por segurança
                    var response = new
                    {
                        StatusCode = context.Response.StatusCode,
                        Message = "Erro Interno do Servidor",
                        Details = "Ocorreu um erro ao processar sua solicitação"
                    };

                    // Serializa a resposta para JSON e envia ao cliente
                    var jsonResponse = JsonSerializer.Serialize(response);
                    await context.Response.WriteAsync(jsonResponse);
                }
            });
        });

        return app;
    }

    /// <summary>
    /// Executa testes de integração básicos durante a inicialização da aplicação.
    /// Estes testes verificam se os componentes essenciais estão funcionando corretamente,
    /// incluindo conectividade com banco de dados e injeção de dependências.
    /// Útil para detectar problemas de configuração antes da aplicação receber requisições.
    /// </summary>
    /// <param name="app">Instância da aplicação web</param>
    /// <returns>A mesma instância da aplicação para permitir method chaining</returns>
    public static async Task<WebApplication> RunIntegrationTestsAsync(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        
        // Teste 1: Verificação de conectividade com Entity Framework/Banco de Dados
        try
        {
            // Tenta estabelecer conexão com o banco de dados
            // Este teste falha se: string de conexão inválida, banco indisponível, credenciais incorretas
            var dbContext = app.Services.GetRequiredService<UserService.Infrastructure.Data.UserServiceDbContext>();
            await dbContext.Database.CanConnectAsync();
            logger.LogInformation("✅ Teste de conexão Entity Framework: SUCESSO");
        }
        catch (Exception ex)
        {
            // Log do erro para debugging - não interrompe a inicialização
            // Em produção, considere implementar retry logic ou circuit breaker
            logger.LogError(ex, "❌ Teste de conexão Entity Framework: FALHOU");
        }

        // Teste 2: Verificação do container de Injeção de Dependências
        try
        {
            // Tenta resolver um serviço crítico da aplicação
            // Este teste falha se: serviço não registrado, dependências circulares, configuração incorreta
            var keycloakService = app.Services.GetRequiredService<IKeycloakService>();
            logger.LogInformation("✅ Teste de Injeção de Dependência: SUCESSO");
        }
        catch (Exception ex)
        {
            // Falha na DI geralmente indica problema grave de configuração
            logger.LogError(ex, "❌ Teste de Injeção de Dependência: FALHOU");
        }

        return app;
    }

    /// <summary>
    /// Configura logging de inicialização da aplicação com informações importantes
    /// sobre o ambiente e configurações críticas. Facilita debugging e monitoramento
    /// durante o startup da aplicação em diferentes ambientes.
    /// </summary>
    /// <param name="app">Instância da aplicação web</param>
    /// <param name="configuration">Configurações da aplicação</param>
    /// <returns>A mesma instância da aplicação para permitir method chaining</returns>
    public static WebApplication ConfigureStartupLogging(this WebApplication app, IConfiguration configuration)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        
        // Log de inicialização - marca o início do processo de startup
        logger.LogInformation("API de Gerenciamento de Usuários B-Commerce iniciando...");
        
        // Log do ambiente atual - crítico para identificar configurações específicas
        // Ajuda a distinguir entre Development, Staging, Production, etc.
        logger.LogInformation("Ambiente: {Environment}", app.Environment.EnvironmentName);
        
        // Log da URL do servidor de autenticação Keycloak
        // Importante para verificar se está apontando para o ambiente correto
        logger.LogInformation("URL do Keycloak: {KeycloakUrl}", configuration["Keycloak:AuthServerUrl"]);
        
        // Log do realm do Keycloak sendo utilizado
        // Cada ambiente pode ter um realm diferente (dev, staging, prod)
        logger.LogInformation("Realm do Keycloak: {Realm}", configuration["Keycloak:Realm"]);
        
        return app;
    }
}