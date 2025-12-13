using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;


namespace Common.Infrastructure.Resilience;

public static class ResilienceExtensions
{
    /// <summary>
    /// Adiciona todas as políticas de resiliência ao registry
    /// </summary>
    public static IServiceCollection AddResiliencePolicies(this IServiceCollection services)
    {
        return services;
    }

    /// <summary>
    /// Configura HttpClient com políticas de resiliência
    /// </summary>
    public static IHttpClientBuilder AddResilientHttpClient(
        this IServiceCollection services,
        string name,
        Action<HttpClient> configureClient)
    {
        var builder = services.AddHttpClient(name, configureClient);
        builder.AddStandardResilienceHandler();
        return builder;
    }

    /// <summary>
    /// Configura HttpClient para comunicação entre microserviços
    /// </summary>
    public static IHttpClientBuilder AddMicroserviceHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        string baseAddress)
        where TClient : class
        where TImplementation : class, TClient
    {
        var builder = services.AddHttpClient<TClient, TImplementation>(client =>
        {
            client.BaseAddress = new Uri(baseAddress);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        builder.AddStandardResilienceHandler();
        return builder;
}
}
