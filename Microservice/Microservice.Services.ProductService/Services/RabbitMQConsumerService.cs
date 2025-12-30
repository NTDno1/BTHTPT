using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microservice.Services.ProductService.DTOs;
using Microservice.Services.ProductService.Services;
using Microservice.Services.ProductService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace Microservice.Services.ProductService.Services;

public class RabbitMQConsumerService : IHostedService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQConsumerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _requestQueueName = "api.product.request";
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
            _logger.LogInformation("Connecting to RabbitMQ at {HostName}:{Port}...", 
                factory.HostName, factory.Port);
            
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

            _logger.LogInformation("Request queue declared: {QueueName}", _requestQueueName);

            // Declare response queue
            _channel.QueueDeclare(
                queue: _responseQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("Response queue declared: {QueueName}", _responseQueueName);
            _logger.LogInformation("RabbitMQ Consumer Service initialized successfully. Ready to listen on queue: {QueueName}", _requestQueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ Consumer Service. Error: {Message}", ex.Message);
            throw;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting RabbitMQ Consumer Service...");
            
            if (_channel == null || _channel.IsClosed)
            {
                _logger.LogError("RabbitMQ channel is null or closed. Cannot start consumer.");
                throw new InvalidOperationException("RabbitMQ channel is not available");
            }
            
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
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                    
                    response = await ProcessRequestAsync(request, productService);
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

            _logger.LogInformation("Successfully started consuming messages from queue: {QueueName}. Consumer is ready to receive requests.", _requestQueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RabbitMQ Consumer Service. Error: {Message}", ex.Message);
            throw;
        }
        
        return Task.CompletedTask;
    }

    private async Task<ApiResponse> ProcessRequestAsync(ApiRequest request, IProductService productService)
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

            if (segments.Length < 2 || segments[0].ToLower() != "api" || segments[1].ToLower() != "products")
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
                        // GET /api/products
                        var products = await productService.GetAllProductsAsync();
                        response.StatusCode = 200;
                        response.Data = products;
                    }
                    else if (segments.Length == 3)
                    {
                        // GET /api/products/{id} or GET /api/products/category/{category}
                        if (segments[2].ToLower() == "category" && segments.Length == 4)
                        {
                            // GET /api/products/category/{category}
                            var categoryProducts = await productService.GetProductsByCategoryAsync(segments[3]);
                            response.StatusCode = 200;
                            response.Data = categoryProducts;
                        }
                        else if (int.TryParse(segments[2], out var productId))
                        {
                            // GET /api/products/{id}
                            var product = await productService.GetProductByIdAsync(productId);
                            if (product == null)
                            {
                                response.StatusCode = 404;
                                response.ErrorMessage = $"Product with ID {productId} not found";
                            }
                            else
                            {
                                response.StatusCode = 200;
                                response.Data = product;
                            }
                        }
                        else
                        {
                            response.StatusCode = 400;
                            response.ErrorMessage = "Invalid product ID";
                        }
                    }
                    else if (segments.Length == 4 && segments[2].ToLower() == "category")
                    {
                        // GET /api/products/category/{category}
                        var categoryProducts = await productService.GetProductsByCategoryAsync(segments[3]);
                        response.StatusCode = 200;
                        response.Data = categoryProducts;
                    }
                    break;

                case "POST":
                    // POST /api/products
                    if (request.Body != null)
                    {
                        var createProductJson = JsonSerializer.Serialize(request.Body, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                        var createProductDto = JsonSerializer.Deserialize<CreateProductDto>(createProductJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                        if (createProductDto == null)
                        {
                            response.StatusCode = 400;
                            response.ErrorMessage = "Invalid request body";
                        }
                        else
                        {
                            var product = await productService.CreateProductAsync(createProductDto);
                            response.StatusCode = 201;
                            response.Data = product;
                        }
                    }
                    else
                    {
                        response.StatusCode = 400;
                        response.ErrorMessage = "Request body is required";
                    }
                    break;

                case "PUT":
                    // PUT /api/products/{id}
                    if (segments.Length == 3)
                    {
                        if (int.TryParse(segments[2], out var updateProductId) && request.Body != null)
                        {
                            var updateJson = JsonSerializer.Serialize(request.Body, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                            var updateDto = JsonSerializer.Deserialize<UpdateProductDto>(updateJson, new JsonSerializerOptions
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
                                var product = await productService.UpdateProductAsync(updateProductId, updateDto);
                                if (product == null)
                                {
                                    response.StatusCode = 404;
                                    response.ErrorMessage = $"Product with ID {updateProductId} not found";
                                }
                                else
                                {
                                    response.StatusCode = 200;
                                    response.Data = product;
                                }
                            }
                        }
                        else
                        {
                            response.StatusCode = 400;
                            response.ErrorMessage = "Invalid product ID or request body";
                        }
                    }
                    else
                    {
                        response.StatusCode = 400;
                        response.ErrorMessage = "Invalid path for PUT request";
                    }
                    break;

                case "PATCH":
                    // PATCH /api/products/{id}/stock
                    if (segments.Length == 4 && segments[3].ToLower() == "stock")
                    {
                        if (int.TryParse(segments[2], out var stockProductId) && request.Body != null)
                        {
                            // Body should be an integer for stock quantity
                            var stockJson = JsonSerializer.Serialize(request.Body, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                            if (int.TryParse(stockJson, out var quantity))
                            {
                                var result = await productService.UpdateStockAsync(stockProductId, quantity);
                                if (result)
                                {
                                    response.StatusCode = 200;
                                    response.Data = new { message = "Stock updated successfully" };
                                }
                                else
                                {
                                    response.StatusCode = 404;
                                    response.ErrorMessage = $"Product with ID {stockProductId} not found";
                                }
                            }
                            else
                            {
                                response.StatusCode = 400;
                                response.ErrorMessage = "Invalid stock quantity";
                            }
                        }
                        else
                        {
                            response.StatusCode = 400;
                            response.ErrorMessage = "Invalid product ID or request body";
                        }
                    }
                    else
                    {
                        response.StatusCode = 400;
                        response.ErrorMessage = "Invalid path for PATCH request";
                    }
                    break;

                case "DELETE":
                    // DELETE /api/products/{id}
                    if (segments.Length == 3)
                    {
                        if (int.TryParse(segments[2], out var deleteProductId))
                        {
                            var result = await productService.DeleteProductAsync(deleteProductId);
                            if (result)
                            {
                                response.StatusCode = 204;
                            }
                            else
                            {
                                response.StatusCode = 404;
                                response.ErrorMessage = $"Product with ID {deleteProductId} not found";
                            }
                        }
                        else
                        {
                            response.StatusCode = 400;
                            response.ErrorMessage = "Invalid product ID";
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

