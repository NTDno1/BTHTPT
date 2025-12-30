namespace Microservice.Common.Models;

public class MessageEvent
{
    public string EventType { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public object? Data { get; set; }
    public string? CorrelationId { get; set; }
}

