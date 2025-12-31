using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microservice.Services.UserService.DTOs;
using Microservice.Services.UserService.Services;
using Microservice.Services.UserService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace Microservice.Services.UserService.Services;

public class RabbitMQConsumerService : IHostedService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQConsumerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _requestQueueName = "api.user.request";
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
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                
                response = await ProcessRequestAsync(request, userService);
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

    private async Task<ApiResponse> ProcessRequestAsync(ApiRequest request, IUserService userService)
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

            if (segments.Length < 2 || segments[0].ToLower() != "api")
            {
                response.StatusCode = 404;
                response.ErrorMessage = "Invalid path";
                return response;
            }

            var routeSegment = segments[1].ToLower();
            
            // Handle auth routes
            if (routeSegment == "auth")
            {
                return await ProcessAuthRequestAsync(request, userService);
            }

            // Handle users routes
            if (routeSegment != "users")
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
                        // GET /api/users
                        var users = await userService.GetAllUsersAsync();
                        response.StatusCode = 200;
                        response.Data = users;
                    }
                    else if (segments.Length == 3)
                    {
                        // GET /api/users/{id}
                        if (int.TryParse(segments[2], out var userId))
                        {
                            var user = await userService.GetUserByIdAsync(userId);
                            if (user == null)
                            {
                                response.StatusCode = 404;
                                response.ErrorMessage = $"User with ID {userId} not found";
                            }
                            else
                            {
                                response.StatusCode = 200;
                                response.Data = user;
                            }
                        }
                        else
                        {
                            response.StatusCode = 400;
                            response.ErrorMessage = "Invalid user ID";
                        }
                    }
                    break;

                case "POST":
                    // POST /api/users
                    if (request.Body != null)
                    {
                        var createUserJson = JsonSerializer.Serialize(request.Body, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                        var createUserDto = JsonSerializer.Deserialize<CreateUserDto>(createUserJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                        if (createUserDto == null)
                        {
                            response.StatusCode = 400;
                            response.ErrorMessage = "Invalid request body";
                        }
                        else
                        {
                            var user = await userService.CreateUserAsync(createUserDto);
                            response.StatusCode = 201;
                            response.Data = user;
                        }
                    }
                    else
                    {
                        response.StatusCode = 400;
                        response.ErrorMessage = "Request body is required";
                    }
                    break;

                case "PUT":
                    // PUT /api/users/{id}
                    if (segments.Length == 3)
                    {
                        if (int.TryParse(segments[2], out var updateUserId) && request.Body != null)
                        {
                            var updateJson = JsonSerializer.Serialize(request.Body, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                            var updateDto = JsonSerializer.Deserialize<UpdateUserDto>(updateJson, new JsonSerializerOptions
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
                                var user = await userService.UpdateUserAsync(updateUserId, updateDto);
                                if (user == null)
                                {
                                    response.StatusCode = 404;
                                    response.ErrorMessage = $"User with ID {updateUserId} not found";
                                }
                                else
                                {
                                    response.StatusCode = 200;
                                    response.Data = user;
                                }
                            }
                        }
                        else
                        {
                            response.StatusCode = 400;
                            response.ErrorMessage = "Invalid user ID or request body";
                        }
                    }
                    else
                    {
                        response.StatusCode = 400;
                        response.ErrorMessage = "Invalid path for PUT request";
                    }
                    break;

                case "DELETE":
                    // DELETE /api/users/{id}
                    if (segments.Length == 3)
                    {
                        if (int.TryParse(segments[2], out var deleteUserId))
                        {
                            var result = await userService.DeleteUserAsync(deleteUserId);
                            if (result)
                            {
                                response.StatusCode = 204;
                            }
                            else
                            {
                                response.StatusCode = 404;
                                response.ErrorMessage = $"User with ID {deleteUserId} not found";
                            }
                        }
                        else
                        {
                            response.StatusCode = 400;
                            response.ErrorMessage = "Invalid user ID";
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

    private async Task<ApiResponse> ProcessAuthRequestAsync(ApiRequest request, IUserService userService)
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

            if (segments.Length < 3 || segments[0].ToLower() != "api" || segments[1].ToLower() != "auth")
            {
                response.StatusCode = 404;
                response.ErrorMessage = "Invalid auth path";
                return response;
            }

            var action = segments[2].ToLower();

            // Get JwtService from scope
            using var scope = _serviceProvider.CreateScope();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            if (request.Method.ToUpper() == "POST")
            {
                if (action == "login")
                {
                    // POST /api/auth/login
                    if (request.Body == null)
                    {
                        response.StatusCode = 400;
                        response.ErrorMessage = "Request body is required";
                        return response;
                    }

                    var loginJson = JsonSerializer.Serialize(request.Body, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    var loginRequest = JsonSerializer.Deserialize<LoginRequestDto>(loginJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Username) || string.IsNullOrEmpty(loginRequest.Password))
                    {
                        response.StatusCode = 400;
                        response.ErrorMessage = "Username and password are required";
                        return response;
                    }

                    // Validate user credentials
                    var user = await userService.ValidateUserAsync(loginRequest.Username, loginRequest.Password);
                    
                    if (user == null)
                    {
                        _logger.LogWarning("Failed login attempt for username: {Username}", loginRequest.Username);
                        response.StatusCode = 401;
                        response.ErrorMessage = "Invalid username or password";
                        return response;
                    }

                    // Generate JWT token
                    var token = jwtService.GenerateToken(user);
                    var refreshToken = jwtService.GenerateRefreshToken();

                    // Map user to DTO
                    var userDto = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        PhoneNumber = user.PhoneNumber,
                        IsActive = user.IsActive,
                        Role = user.Role,
                        AvatarUrl = user.AvatarUrl,
                        CreatedAt = user.CreatedAt
                    };

                    var loginResponse = new LoginResponseDto
                    {
                        Token = token,
                        RefreshToken = refreshToken,
                        User = userDto,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(60)
                    };

                    _logger.LogInformation("User {UserId} logged in successfully", user.Id);
                    response.StatusCode = 200;
                    response.Data = loginResponse;
                }
                else if (action == "register")
                {
                    // POST /api/auth/register
                    if (request.Body == null)
                    {
                        response.StatusCode = 400;
                        response.ErrorMessage = "Request body is required";
                        return response;
                    }

                    var registerJson = JsonSerializer.Serialize(request.Body, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    var createUserDto = JsonSerializer.Deserialize<CreateUserDto>(registerJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (createUserDto == null)
                    {
                        response.StatusCode = 400;
                        response.ErrorMessage = "Invalid request body";
                        return response;
                    }

                    // Create user
                    var userDto = await userService.CreateUserAsync(createUserDto);

                    // Get the created user to generate token
                    var user = await userService.ValidateUserAsync(createUserDto.Username, createUserDto.Password);
                    
                    if (user == null)
                    {
                        response.StatusCode = 500;
                        response.ErrorMessage = "User created but login failed";
                        return response;
                    }

                    // Generate JWT token
                    var token = jwtService.GenerateToken(user);
                    var refreshToken = jwtService.GenerateRefreshToken();

                    var loginResponse = new LoginResponseDto
                    {
                        Token = token,
                        RefreshToken = refreshToken,
                        User = userDto,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(60)
                    };

                    _logger.LogInformation("User {UserId} registered and logged in successfully", userDto.Id);
                    response.StatusCode = 201;
                    response.Data = loginResponse;
                }
                else
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = $"Auth action '{action}' not found";
                }
            }
            else
            {
                response.StatusCode = 405;
                response.ErrorMessage = $"Method {request.Method} not allowed for auth endpoints";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing auth request");
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

