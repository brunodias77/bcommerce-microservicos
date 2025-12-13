using EventBus.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EventBus.RabbitMQ;

/// <summary>
/// Extensões para configurar RabbitMQ no DI Container
/// </summary>
public static class RabbitMQExtensions
{
    /// <summary>
    /// Adiciona o EventBus com RabbitMQ ao container de DI
    /// </summary>
    public static IServiceCollection AddRabbitMQEventBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(RabbitMQSettings.SectionName).Get<RabbitMQSettings>()
                       ?? new RabbitMQSettings();

        // Connection Factory
        services.AddSingleton<IConnectionFactory>(sp =>
        {
            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password,
                VirtualHost = settings.VirtualHost,
                DispatchConsumersAsync = true
            };
            return factory;
        });

        // RabbitMQ Connection
        services.AddSingleton<IRabbitMQConnection>(sp =>
        {
            var factory = sp.GetRequiredService<IConnectionFactory>();
            var logger = sp.GetRequiredService<ILogger<RabbitMQConnection>>();
            return new RabbitMQConnection(factory, logger, settings.RetryCount);
        });

        // Event Bus Subscriptions Manager
        services.AddSingleton<IEventBusSubscriptionsManager, EventBusSubscriptionsManager>();

        // Event Bus
        services.AddSingleton<IEventBus>(sp =>
        {
            var connection = sp.GetRequiredService<IRabbitMQConnection>();
            var subsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();
            var logger = sp.GetRequiredService<ILogger<RabbitMQEventBus>>();

            return new RabbitMQEventBus(
                connection,
                subsManager,
                sp,
                logger,
                settings.ExchangeName,
                settings.QueueName ?? string.Empty);
        });

        return services;
    }

    /// <summary>
    /// Registra um handler de evento de integração
    /// </summary>
    public static IServiceCollection AddIntegrationEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        services.AddTransient<THandler>();
        return services;
    }
}
