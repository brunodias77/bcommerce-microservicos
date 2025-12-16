using Bcommerce.BuildingBlocks.Web.Filters;
using Bcommerce.BuildingBlocks.Web.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Bcommerce.BuildingBlocks.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
            options.Filters.Add<ApiExceptionFilter>();
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            // Disable default ModelState validation because ValidationFilter handles it
            options.SuppressModelStateInvalidFilter = true;
        })
        .AddNewtonsoftJson();

        return services;
    }

    public static IServiceCollection AddCustomMiddleware(this IServiceCollection services)
    {
        services.AddScoped<ExceptionHandlingMiddleware>();
        services.AddScoped<RequestLoggingMiddleware>();
        services.AddScoped<CorrelationIdMiddleware>();
        return services;
    }
}
