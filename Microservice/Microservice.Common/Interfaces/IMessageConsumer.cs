namespace Microservice.Common.Interfaces;

public interface IMessageConsumer
{
    void Consume<T>(string queueName, Action<T> handler) where T : class;
    Task ConsumeAsync<T>(string queueName, Func<T, Task> handler) where T : class;
}

