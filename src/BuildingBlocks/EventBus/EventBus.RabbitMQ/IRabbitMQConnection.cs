using RabbitMQ.Client;

namespace EventBus.RabbitMQ;

public interface IRabbitMQConnection : IDisposable
{
    bool IsConnected { get; }
    bool TryConnect();
    IChannel CreateModel();
}