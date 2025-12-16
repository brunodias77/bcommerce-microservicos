using Bcommerce.BuildingBlocks.Web.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Bcommerce.BuildingBlocks.Web.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCustomMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        return app;
    }
}
