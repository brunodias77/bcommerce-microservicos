using System.Security.Claims;
using BuildingBlocks.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using UserService.Application.Commands.Users.Create;
using UserService.Infrastructure.Services.Keycloak; // Validação de tokens JWT

namespace UserService.Api.Configurations;

public static class ApplicationDependencyInjection
{
    public static void AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        AddMediator(services);
    }
    
    /// <summary>
    /// Configura o padrão Mediator do BuildingBlocks.
    /// O Mediator desacopla os controllers dos handlers, permitindo que as requisições
    /// sejam processadas de forma organizada através de commands, queries e handlers.
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    private static void AddMediator(IServiceCollection services)
    {
        // Registra automaticamente todos os handlers do assembly da aplicação
        // usando o CreateUserCommandHandler como referência para localizar o assembly
        services.AddMediator(typeof(CreateUserCommandHandler).Assembly);
    }
    
    /// <summary>
    /// Configura a documentação automática da API usando Swagger/OpenAPI.
    /// O Swagger gera uma interface web interativa para testar e documentar os endpoints da API.
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    private static void AddSwagger(IServiceCollection services)
    {
        // Habilita a descoberta automática de endpoints para documentação
        services.AddEndpointsApiExplorer();
        
        services.AddSwaggerGen(c =>
        {
            // Define as informações básicas que aparecem na documentação da API
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "B-Commerce User Management API",
                Version = "v1",
                Description = "API for user authentication and management using Keycloak"
            });
            
            // Configura o esquema de autenticação JWT no Swagger UI
            // Isso permite que os usuários insiram tokens JWT diretamente na interface
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",                    // Nome do header HTTP
                In = ParameterLocation.Header,            // Localização do token (header)
                Type = SecuritySchemeType.ApiKey,         // Tipo de esquema de segurança
                Scheme = "Bearer"                         // Esquema Bearer para JWT
            });
            
            // Aplica o esquema de segurança JWT a todos os endpoints documentados
            // Isso faz com que todos os endpoints mostrem o campo de autenticação
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"                     // Referencia o esquema definido acima
                        }
                    },
                    Array.Empty<string>()                    // Não requer escopos específicos
                }
            });
        });
    }
    
     /// <summary>
    /// Configura CORS (Cross-Origin Resource Sharing) para permitir que aplicações frontend
    /// em diferentes domínios/portas possam fazer requisições para esta API.
    /// Essencial para SPAs (Single Page Applications) que rodam em portas diferentes.
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    /// <param name="configuration">Configuração para obter origens permitidas</param>
    private static void AddCors(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
            {
                // Obtém as origens permitidas da configuração (appsettings.json)
                // Se não estiver configurado, usa valores padrão para desenvolvimento local
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                                   ?? new[] { 
                                       "http://localhost:3000",    // React (Create React App)
                                       "http://localhost:5173",    // Vite (Vue/React)
                                       "http://localhost:4200"     // Angular CLI
                                   };
                
                policy.WithOrigins(allowedOrigins)        // Define quais origens podem acessar a API
                    .AllowAnyHeader()                     // Permite qualquer cabeçalho HTTP nas requisições
                    .AllowAnyMethod()                     // Permite qualquer método HTTP (GET, POST, PUT, DELETE, etc.)
                    .AllowCredentials();                  // Permite envio de cookies e credenciais de autenticação
            });
        });
    }
    
    /// <summary>
    /// Configura autenticação JWT (JSON Web Token) integrada com Keycloak.
    /// O Keycloak é um servidor de identidade que gerencia usuários, autenticação e autorização.
    /// Os tokens JWT contêm informações do usuário e são validados a cada requisição.
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    /// <param name="configuration">Configuração para obter settings do Keycloak</param>
    private static void AddAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        // Carrega as configurações do Keycloak do appsettings.json
        // Essas configurações incluem URL do servidor, realm, client IDs, etc.
        var keycloakSettings = configuration.GetSection(KeycloakSettings.SectionName).Get<KeycloakSettings>()
                              ?? throw new InvalidOperationException("Configurações do Keycloak não encontradas");
        
        // Configura o ASP.NET Core para usar autenticação JWT Bearer como padrão
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Define a autoridade que emite os tokens JWT (servidor Keycloak)
                // Esta URL é usada para descobrir as chaves públicas de validação
                options.Authority = $"{keycloakSettings.Url}/realms/{keycloakSettings.Realm}";
                
                // Define o público-alvo esperado nos tokens (audience claim)
                // Garante que o token foi emitido especificamente para esta API
                options.Audience = keycloakSettings.BackendClientId;
                
                // Configura se HTTPS é obrigatório para comunicação com o Keycloak
                // Geralmente desabilitado em desenvolvimento local
                options.RequireHttpsMetadata = configuration.GetValue<bool>("Security:RequireHttps");
                
                // Parâmetros detalhados de validação do token JWT
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Valida se o token foi emitido pelo Keycloak correto (issuer claim)
                    ValidateIssuer = true,
                    ValidIssuer = $"{keycloakSettings.Url}/realms/{keycloakSettings.Realm}",
                    
                    // Valida se o token é destinado a esta API (audience claim)
                    ValidateAudience = true,
                    ValidAudience = keycloakSettings.BackendClientId,
                    
                    // Verifica se o token não expirou (exp claim)
                    ValidateLifetime = true,
                    
                    // Valida a assinatura digital do token usando chaves públicas do Keycloak
                    ValidateIssuerSigningKey = true,
                    
                    // Tolerância para diferenças de relógio entre servidores (5 minutos)
                    ClockSkew = TimeSpan.FromMinutes(5),
                    
                    // Define qual claim do JWT contém as roles do usuário
                    RoleClaimType = ClaimTypes.Role,
                    
                    // Define qual claim do JWT contém o nome de usuário
                    NameClaimType = "preferred_username"
                };

                // Eventos de autenticação para logging, debugging e auditoria
                options.Events = new JwtBearerEvents
                {
                    // Executado quando um token JWT é validado com sucesso
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        var userId = context.Principal?.FindFirst("sub")?.Value;  // 'sub' = subject (ID do usuário)
                        logger.LogDebug("Token validated for user: {UserId}", userId);
                        return Task.CompletedTask;
                    },
                    
                    // Executado quando a autenticação JWT falha
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("JWT authentication failed: {Exception}", context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });
    }
    
    /// <summary>
    /// Configura políticas de autorização baseadas em roles (funções) do usuário.
    /// As roles são definidas no Keycloak e incluídas nos tokens JWT.
    /// Cada política define quais roles têm acesso a determinados recursos.
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    private static void AddAuthorization(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Política para administradores do sistema - acesso total
            // Usuários com roles 'admin' ou 'realm-admin' podem acessar recursos administrativos
            options.AddPolicy("AdminPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();           // Usuário deve estar autenticado
                policy.RequireRole("admin", "realm-admin");  // Deve ter uma dessas roles
            });
            
            // Política para usuários comuns - acesso básico
            // Usuários com roles 'user', 'admin' ou 'realm-admin' podem acessar recursos básicos
            options.AddPolicy("UserPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();                        // Usuário deve estar autenticado
                policy.RequireRole("user", "admin", "realm-admin");       // Deve ter uma dessas roles
            });
            
            // Política para gerentes - acesso intermediário
            // Usuários com roles 'manager', 'admin' ou 'realm-admin' podem acessar recursos de gestão
            options.AddPolicy("ManagerPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();                        // Usuário deve estar autenticado
                policy.RequireRole("manager", "admin", "realm-admin");    // Deve ter uma dessas roles
            });
        });
    }

    
    /// <summary>
    /// Configura Health Checks para monitoramento da saúde da aplicação.
    /// Health Checks permitem verificar se a aplicação e suas dependências estão funcionando corretamente.
    /// Útil para load balancers, orquestradores (Kubernetes) e monitoramento.
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    /// <param name="configuration">Configurações da aplicação</param>
    private static void AddHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            // Adiciona verificação básica de saúde da aplicação
            // Para usar verificações específicas do PostgreSQL, seria necessário instalar o pacote AspNetCore.HealthChecks.Npgsql
            .AddCheck("database", () =>
            {
                // Health check básico - verifica se a connection string está configurada
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                return !string.IsNullOrEmpty(connectionString) 
                    ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database connection string configured")
                    : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database connection string not configured");
            });
    }

}