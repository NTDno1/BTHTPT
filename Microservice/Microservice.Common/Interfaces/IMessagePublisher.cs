namespace Microservice.Common.Interfaces;

public interface IMessagePublisher
{
    void Publish<T>(T message, string routingKey) where T : class;
    Task PublishAsync<T>(T message, string routingKey) where T : class;
}

