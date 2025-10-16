using System.Security.Claims;
using BuildingBlocks.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using UserService.Application.Commands.Users.Create;
using UserService.Infrastructure.Services.Keycloak;

namespace UserService.Api.Configurations;

public static class ApplicationDependencyInjection
{
    public static void AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        AddMediator(services);
        AddSwagger(services);
        AddCors(services, configuration);
        AddAuthentication(services, configuration);
        AddAuthorization(services);
        AddHealthChecks(services, configuration);
    }
    
    /// <summary>
    /// Configura o padrão Mediator para desacoplar controllers dos handlers.
    /// </summary>
    private static void AddMediator(IServiceCollection services)
    {
        services.AddMediator(typeof(CreateUserCommandHandler).Assembly);
    }
    
    /// <summary>
    /// Configura documentação Swagger/OpenAPI com autenticação JWT.
    /// </summary>
    private static void AddSwagger(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "B-Commerce User Management API",
                Version = "v1",
                Description = "API for user authentication and management using Keycloak"
            });
            
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }
    
    /// <summary>
    /// Configura CORS para permitir acesso de aplicações frontend.
    /// </summary>
    private static void AddCors(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                                   ?? new[] { 
                                       "http://localhost:3000",
                                       "http://localhost:5173",
                                       "http://localhost:4200"
                                   };
                
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }
    
    /// <summary>
    /// Configura autenticação JWT integrada com Keycloak.
    /// </summary>
    private static void AddAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var keycloakSettings = configuration.GetSection(KeycloakSettings.SectionName).Get<KeycloakSettings>()
                              ?? throw new InvalidOperationException("Configurações do Keycloak não encontradas");
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                ConfigureJwtOptions(options, keycloakSettings, configuration);
                ConfigureJwtEvents(options);
            });
    }

    private static void ConfigureJwtOptions(JwtBearerOptions options, KeycloakSettings keycloakSettings, IConfiguration configuration)
    {
        options.Authority = $"{keycloakSettings.Url}/realms/{keycloakSettings.Realm}";
        options.Audience = keycloakSettings.BackendClientId;
        options.RequireHttpsMetadata = configuration.GetValue<bool>("Security:RequireHttps");
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{keycloakSettings.Url}/realms/{keycloakSettings.Realm}",
            ValidateAudience = true,
            ValidAudience = keycloakSettings.BackendClientId,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = "preferred_username"
        };
    }

    private static void ConfigureJwtEvents(JwtBearerOptions options)
    {
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var userId = context.Principal?.FindFirst("sub")?.Value;
                logger.LogDebug("Token validated for user: {UserId}", userId);
                return Task.CompletedTask;
            },
            
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT authentication failed: {Exception}", context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    }
    
    /// <summary>
    /// Configura políticas de autorização baseadas em roles.
    /// </summary>
    private static void AddAuthorization(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("admin", "realm-admin");
            });
            
            options.AddPolicy("UserPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("user", "admin", "realm-admin");
            });
            
            options.AddPolicy("ManagerPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("manager", "admin", "realm-admin");
            });
        });
    }

    /// <summary>
    /// Configura Health Checks para monitoramento da aplicação.
    /// </summary>
    private static void AddHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck("database", () =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                return !string.IsNullOrEmpty(connectionString) 
                    ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database connection string configured")
                    : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database connection string not configured");
            });
    }
}