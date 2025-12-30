using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microservice.Services.OrderService.DTOs;
using Microservice.Services.OrderService.Services;
using Microservice.Services.OrderService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace Microservice.Services.OrderService.Services;

public class RabbitMQConsumerService : IHostedService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQConsumerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _requestQueueName = "api.order.request";
    private readonly string _responseQueueName = "api.gateway.response";

    public RabbitMQConsumerService(
        IConfiguration configuration,
        ILogger<RabbitMQConsumerService> logger,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;

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

            _logger.LogInformation("RabbitMQ connection established successfully");

            // Configure QoS (Quality of Service) for fair dispatch
            // Prefetch count = 1: Mỗi consumer chỉ nhận 1 message chưa ack tại một thời điểm
            // Đảm bảo phân phối đều giữa các consumers (load balancing)
            _channel.BasicQos(
                prefetchSize: 0,      // Không giới hạn size
                prefetchCount: 1,     // Chỉ nhận 1 message chưa ack
                global: false         // Áp dụng cho consumer này
            );

            _logger.LogInformation("QoS configured: prefetchCount=1 for fair dispatch");

            // Declare request queue
            _channel.QueueDeclare(
                queue: _requestQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Declare response queue
            _channel.QueueDeclare(
                queue: _responseQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("RabbitMQ Consumer Service initialized. Listening on queue: {QueueName}", _requestQueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ Consumer Service");
            throw;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var response = new ApiResponse
            {
                CorrelationId = ea.BasicProperties.CorrelationId
            };

            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var request = JsonSerializer.Deserialize<ApiRequest>(message, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (request == null)
                {
                    response.StatusCode = 400;
                    response.ErrorMessage = "Invalid request format";
                    SendResponse(response, ea.BasicProperties.ReplyTo);
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                _logger.LogInformation("Received request: {Method} {Path}, CorrelationId: {CorrelationId}",
                    request.Method, request.Path, request.CorrelationId);

                // Process request
                using var scope = _serviceProvider.CreateScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                
                response = await ProcessRequestAsync(request, orderService);
                response.CorrelationId = request.CorrelationId;

                SendResponse(response, ea.BasicProperties.ReplyTo);
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request. CorrelationId: {CorrelationId}", response.CorrelationId);
                response.StatusCode = 500;
                response.ErrorMessage = $"Internal server error: {ex.Message}";
                SendResponse(response, ea.BasicProperties.ReplyTo);
                _channel.BasicAck(ea.DeliveryTag, false);
            }
        };

        _channel.BasicConsume(
            queue: _requestQueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Started consuming messages from queue: {QueueName}", _requestQueueName);
        return Task.CompletedTask;
    }

    private async Task<ApiResponse> ProcessRequestAsync(ApiRequest request, IOrderService orderService)
    {
        var response = new ApiResponse
        {
            CorrelationId = request.CorrelationId
        };

        try
        {
            // Parse path to determine action
            var path = request.Path.TrimStart('/');
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length < 2 || segments[0].ToLower() != "api" || segments[1].ToLower() != "orders")
            {
                response.StatusCode = 404;
                response.ErrorMessage = "Invalid path";
                return response;
            }

            switch (request.Method.ToUpper())
            {
                case "GET":
                    if (segments.Length == 2)
                    {
                        // GET /api/orders
                        var orders = await orderService.GetAllOrdersAsync();
                        response.StatusCode = 200;
                        response.Data = orders;
                    }
                    else if (segments.Length == 4 && segments[2].ToLower() == "user")
                    {
                        // GET /api/orders/user/{userId}
                        if (int.TryParse(segments[3], out var userId))
                        {
                            var userOrders = await orderService.GetOrdersByUserIdAsync(userId);
                            response.StatusCode = 200;
                            response.Data = userOrders;
                        }
                        else
                        {
                            response.StatusCode = 400;
                            response.ErrorMessage = "Invalid user ID";
                        }
                    }
                    else if (segments.Length == 3)
                    {
                        // GET /api/orders/{id}
                        if (int.TryParse(segments[2], out var orderId))
                        {
                            var order = await orderService.GetOrderByIdAsync(orderId);
                            if (order == null)
                            {
                                response.StatusCode = 404;
                                response.ErrorMessage = $"Order with ID {orderId} not found";
                            }
                            else
                            {
                                response.StatusCode = 200;
                                response.Data = order;
                            }
                        }
                        else
                        {
                            response.StatusCode = 400;
                            response.ErrorMessage = "Invalid order ID";
                        }
                    }
                    break;

                case "POST":
                    // POST /api/orders
                    if (request.Body != null)
                    {
                        var createOrderJson = JsonSerializer.Serialize(request.Body, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                        var createOrderDto = JsonSerializer.Deserialize<CreateOrderDto>(createOrderJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                        if (createOrderDto == null)
                        {
                            response.StatusCode = 400;
                            response.ErrorMessage = "Invalid request body";
                        }
                        else
                        {
                            var order = await orderService.CreateOrderAsync(createOrderDto);
                            response.StatusCode = 201;
                            response.Data = order;
                        }
                    }
                    else
                    {
                        response.StatusCode = 400;
                        response.ErrorMessage = "Request body is required";
                    }
                    break;

                case "PUT":
                    // PUT /api/orders/{id}/status
                    if (segments.Length == 4 && segments[3].ToLower() == "status")
                    {
                        if (int.TryParse(segments[2], out var updateOrderId) && request.Body != null)
                        {
                            var updateJson = JsonSerializer.Serialize(request.Body, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                            var updateDto = JsonSerializer.Deserialize<UpdateOrderStatusDto>(updateJson, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });

                            if (updateDto == null)
                            {
                                response.StatusCode = 400;
                                response.ErrorMessage = "Invalid request body";
                            }
                            else
                            {
                                var order = await orderService.UpdateOrderStatusAsync(updateOrderId, updateDto);
                                if (order == null)
                                {
                                    response.StatusCode = 404;
                                    response.ErrorMessage = $"Order with ID {updateOrderId} not found";
                                }
                                else
                                {
                                    response.StatusCode = 200;
                                    response.Data = order;
                                }
                            }
                        }
                        else
                        {
                            response.StatusCode = 400;
                            response.ErrorMessage = "Invalid order ID or request body";
                        }
                    }
                    else
                    {
                        response.StatusCode = 400;
                        response.ErrorMessage = "Invalid path for PUT request";
                    }
                    break;

                case "DELETE":
                    // DELETE /api/orders/{id}
                    if (segments.Length == 3)
                    {
                        if (int.TryParse(segments[2], out var deleteOrderId))
                        {
                            var result = await orderService.DeleteOrderAsync(deleteOrderId);
                            if (result)
                            {
                                response.StatusCode = 204;
                            }
                            else
                            {
                                response.StatusCode = 404;
                                response.ErrorMessage = $"Order with ID {deleteOrderId} not found";
                            }
                        }
                        else
                        {
                            response.StatusCode = 400;
                            response.ErrorMessage = "Invalid order ID";
                        }
                    }
                    else
                    {
                        response.StatusCode = 400;
                        response.ErrorMessage = "Invalid path for DELETE request";
                    }
                    break;

                default:
                    response.StatusCode = 405;
                    response.ErrorMessage = $"Method {request.Method} not allowed";
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request");
            response.StatusCode = 500;
            response.ErrorMessage = $"Internal server error: {ex.Message}";
        }

        return response;
    }

    private void SendResponse(ApiResponse response, string? replyTo)
    {
        if (string.IsNullOrEmpty(replyTo))
        {
            replyTo = _responseQueueName;
        }

        try
        {
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.CorrelationId = response.CorrelationId;
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: "",
                routingKey: replyTo,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Response sent. CorrelationId: {CorrelationId}, StatusCode: {StatusCode}",
                response.CorrelationId, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending response. CorrelationId: {CorrelationId}", response.CorrelationId);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQ Consumer Service");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}

