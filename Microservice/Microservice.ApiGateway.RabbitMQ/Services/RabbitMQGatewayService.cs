using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using Microservice.ApiGateway.RabbitMQ.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microservice.ApiGateway.RabbitMQ.Services;

public class RabbitMQGatewayService : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQGatewayService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<ApiResponse>> _pendingRequests;
    private readonly string _responseQueueName;

    public RabbitMQGatewayService(IConfiguration configuration, ILogger<RabbitMQGatewayService> logger)
    {
        _logger = logger;
        _configuration = configuration;
        _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<ApiResponse>>();

        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672")
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            // Tạo response queue cho gateway
            _responseQueueName = "api.gateway.response";
            _channel.QueueDeclare(
                queue: _responseQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Setup consumer để nhận responses
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var response = JsonSerializer.Deserialize<ApiResponse>(message);

                    if (response != null && _pendingRequests.TryRemove(response.CorrelationId, out var tcs))
                    {
                        tcs.SetResult(response);
                        _logger.LogInformation("Received response for correlation ID: {CorrelationId}", response.CorrelationId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing response message");
                }
                finally
                {
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
            };

            _channel.BasicConsume(
                queue: _responseQueueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("RabbitMQ Gateway Service initialized. Response queue: {QueueName}", _responseQueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ Gateway Service");
            throw;
        }
    }

    public async Task<ApiResponse> SendRequestAsync(ApiRequest request, string serviceName, CancellationToken cancellationToken = default)
    {
        var routeConfig = _configuration.GetSection($"ServiceRoutes:{serviceName}");
        var requestQueue = routeConfig["Queue"] ?? throw new InvalidOperationException($"No queue configured for service: {serviceName}");

        // Đảm bảo request queue tồn tại
        _channel.QueueDeclare(
            queue: requestQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Tạo TaskCompletionSource để đợi response
        var tcs = new TaskCompletionSource<ApiResponse>();
        _pendingRequests[request.CorrelationId] = tcs;

        try
        {
            // Serialize request
            var json = JsonSerializer.Serialize(request);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.CorrelationId = request.CorrelationId;
            properties.ReplyTo = _responseQueueName;
            properties.Persistent = true;

            // Gửi request
            _channel.BasicPublish(
                exchange: "",
                routingKey: requestQueue,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Request sent to queue: {QueueName}, CorrelationId: {CorrelationId}", 
                requestQueue, request.CorrelationId);

            // Đợi response với timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 giây timeout

            var response = await tcs.Task.WaitAsync(cts.Token);
            return response;
        }
        catch (OperationCanceledException)
        {
            _pendingRequests.TryRemove(request.CorrelationId, out _);
            _logger.LogWarning("Request timeout for CorrelationId: {CorrelationId}", request.CorrelationId);
            return new ApiResponse
            {
                StatusCode = 504,
                ErrorMessage = "Gateway timeout: No response from service",
                CorrelationId = request.CorrelationId
            };
        }
        catch (Exception ex)
        {
            _pendingRequests.TryRemove(request.CorrelationId, out _);
            _logger.LogError(ex, "Error sending request for CorrelationId: {CorrelationId}", request.CorrelationId);
            return new ApiResponse
            {
                StatusCode = 500,
                ErrorMessage = $"Internal gateway error: {ex.Message}",
                CorrelationId = request.CorrelationId
            };
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}

