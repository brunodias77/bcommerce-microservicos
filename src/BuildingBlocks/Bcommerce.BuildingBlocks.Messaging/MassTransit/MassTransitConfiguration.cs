using Bcommerce.BuildingBlocks.Messaging.Abstractions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Bcommerce.BuildingBlocks.Messaging.MassTransit;

public static class MassTransitConfiguration
{
    public static IServiceCollection AddMessageBus(this IServiceCollection services, IConfiguration configuration, Assembly? assembly = null)
    {
        services.AddMassTransit(config =>
        {
            config.SetKebabCaseEndpointNameFormatter();

            if (assembly != null)
            {
                config.AddConsumers(assembly);
            }

            config.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration["MessageBroker:Host"] ?? "localhost", "/", h =>
                {
                    h.Username(configuration["MessageBroker:Username"] ?? "guest");
                    h.Password(configuration["MessageBroker:Password"] ?? "guest");
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IEventBus, MassTransitEventBus>();
        services.AddScoped<IMessagePublisher, MassTransitEventBus>();

        return services;
    }
}
