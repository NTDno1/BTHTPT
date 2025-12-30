using Microsoft.AspNetCore.Mvc;
using Microservice.ApiGateway.RabbitMQ.Models;
using Microservice.ApiGateway.RabbitMQ.Services;
using System.Text.Json;

namespace Microservice.ApiGateway.RabbitMQ.Controllers;

[ApiController]
[Route("api/{**path}")]
public class GatewayController : ControllerBase
{
    private readonly RabbitMQGatewayService _gatewayService;
    private readonly RouteMappingService _routeMappingService;
    private readonly ILogger<GatewayController> _logger;

    public GatewayController(
        RabbitMQGatewayService gatewayService,
        RouteMappingService routeMappingService,
        ILogger<GatewayController> logger)
    {
        _gatewayService = gatewayService;
        _routeMappingService = routeMappingService;
        _logger = logger;
    }

    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpPatch]
    [HttpDelete]
    public async Task<IActionResult> RouteRequest(string path)
    {
        // Reconstruct full path
        var fullPath = $"/api/{path}";
        
        // Get service name from path
        var serviceName = _routeMappingService.GetServiceNameFromPath(fullPath);
        if (string.IsNullOrEmpty(serviceName))
        {
            _logger.LogWarning("No service mapping found for path: {Path}", fullPath);
            return NotFound(new { error = $"No service found for path: {fullPath}" });
        }

        _logger.LogInformation("Routing request to {ServiceName}: {Method} {Path}", 
            serviceName, Request.Method, fullPath);

        // Read request body if present
        object? requestBody = null;
        if (Request.ContentLength > 0 && 
            (Request.Method == "POST" || Request.Method == "PUT" || Request.Method == "PATCH"))
        {
            Request.Body.Position = 0; // Reset stream position
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var bodyString = await reader.ReadToEndAsync();
            if (!string.IsNullOrEmpty(bodyString))
            {
                try
                {
                    requestBody = JsonSerializer.Deserialize<object>(bodyString);
                }
                catch
                {
                    requestBody = bodyString;
                }
            }
        }

        // Build query parameters
        var queryParameters = Request.Query.ToDictionary(
            q => q.Key,
            q => q.Value.ToString()
        );

        // Build headers (optional, can be useful for service-to-service communication)
        var headers = Request.Headers.ToDictionary(
            h => h.Key,
            h => h.Value.ToString()
        );

        // Create API request
        var apiRequest = new ApiRequest
        {
            Method = Request.Method,
            Path = fullPath,
            QueryParameters = queryParameters.Any() ? queryParameters : null,
            Headers = headers.Any() ? headers : null,
            Body = requestBody
        };

        try
        {
            // Send request via RabbitMQ
            var response = await _gatewayService.SendRequestAsync(apiRequest, serviceName);
            
            _logger.LogInformation("Received response from {ServiceName}: Status {StatusCode}", 
                serviceName, response.StatusCode);

            return StatusCode(response.StatusCode, response.Data ?? new { error = response.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing request to {ServiceName}", serviceName);
            return StatusCode(500, new { error = $"Internal gateway error: {ex.Message}" });
        }
    }
}

