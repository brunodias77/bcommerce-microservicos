using Common.Application.Behaviors;
using Common.Application.Interfaces;
using Common.Infrastructure.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Threading.RateLimiting;
using WebHost.Customization.Filters;

namespace WebHost.Customization.Extensions;

/// <summary>
/// Extensões para configuração de serviços
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adiciona serviços comuns da aplicação
    /// </summary>
    public static IServiceCollection AddCommonServices(
        this IServiceCollection services,
        Assembly applicationAssembly)
    {
        // HttpContext
        services.AddHttpContextAccessor();

        // Common Services
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddSingleton<IDateTime, DateTimeService>();

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));

        // MediatR Behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        // FluentValidation
        services.AddValidatorsFromAssembly(applicationAssembly);

        // AutoMapper
        services.AddAutoMapper(cfg => cfg.AddMaps(applicationAssembly));

        return services;
    }

    /// <summary>
    /// Adiciona controladores com configurações customizadas
    /// </summary>
    public static IServiceCollection AddCustomControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<HttpGlobalExceptionFilter>();
            options.Filters.Add<ValidateModelStateFilter>();
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

        return services;
    }

    /// <summary>
    /// Adiciona Swagger/OpenAPI com configurações customizadas
    /// </summary>
    public static IServiceCollection AddCustomSwagger(
        this IServiceCollection services,
        string title,
        string version = "v1",
        string? description = null)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(version, new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = title,
                Version = version,
                Description = description
            });

            // JWT Authentication
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Include XML comments if available
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    /// <summary>
    /// Adiciona Health Checks customizados
    /// </summary>
    public static IServiceCollection AddCustomHealthChecks(
        this IServiceCollection services,
        string? connectionString = null)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        if (!string.IsNullOrEmpty(connectionString))
        {
            healthChecksBuilder.AddNpgSql(
                connectionString,
                name: "database",
                tags: new[] { "db", "sql", "postgresql" });
        }

        return services;
    }

    /// <summary>
    /// Adiciona CORS com política permissiva para desenvolvimento
    /// </summary>
    public static IServiceCollection AddCustomCors(
        this IServiceCollection services,
        string policyName = "AllowAll")
    {
        services.AddCors(options =>
        {
            options.AddPolicy(policyName, builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }

    /// <summary>
    /// Adiciona autenticação JWT
    /// </summary>
    public static IServiceCollection AddCustomAuthentication(
        this IServiceCollection services,
        string issuer,
        string audience,
        string secretKey)
    {
        services.AddAuthentication("Bearer")
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(secretKey))
                };

                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    /// <summary>
    /// Adiciona cache distribuído com Redis
    /// </summary>
    public static IServiceCollection AddCustomRedisCache(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = "ECommerce_";
        });

        return services;
    }

    /// <summary>
    /// Adiciona rate limiting
    /// </summary>
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var userId = context.User.Identity?.IsAuthenticated == true
                    ? context.User.Identity.Name
                    : context.Connection.RemoteIpAddress?.ToString();

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.RejectionStatusCode = 429;
        });

        return services;
    }
}
