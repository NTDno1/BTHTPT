namespace Microservice.ApiGateway.RabbitMQ.Models;

public class ApiResponse
{
    public int StatusCode { get; set; }
    public object? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

