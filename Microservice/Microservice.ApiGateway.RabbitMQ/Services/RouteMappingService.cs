using Microsoft.Extensions.Configuration;

namespace Microservice.ApiGateway.RabbitMQ.Services;

public class RouteMappingService
{
    private readonly Dictionary<string, string> _routeToServiceMap;
    private readonly ILogger<RouteMappingService> _logger;
    private readonly IConfiguration _configuration;

    public RouteMappingService(IConfiguration configuration, ILogger<RouteMappingService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _routeToServiceMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Load route mappings from configuration
        var serviceRoutes = configuration.GetSection("ServiceRoutes").GetChildren();
        foreach (var serviceRoute in serviceRoutes)
        {
            var serviceName = serviceRoute.Key;
            var routePrefix = ExtractRoutePrefix(serviceName);
            if (!string.IsNullOrEmpty(routePrefix))
            {
                _routeToServiceMap[routePrefix] = serviceName;
                _logger.LogInformation("Mapped route '{RoutePrefix}' to service '{ServiceName}'", routePrefix, serviceName);
            }
        }
    }

    public string? GetServiceNameFromPath(string path)
    {
        // Extract the first segment after /api/
        // Example: /api/users/123 -> users
        // Example: /api/products -> products
        if (string.IsNullOrEmpty(path))
            return null;

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || segments[0].ToLower() != "api")
            return null;

        var routePrefix = segments[1].ToLower();
        
        if (_routeToServiceMap.TryGetValue(routePrefix, out var serviceName))
        {
            return serviceName;
        }

        // Try to match by converting route to service name
        // Example: users -> UserService, products -> ProductService
        var potentialServiceName = $"{char.ToUpper(routePrefix[0])}{routePrefix.Substring(1)}Service";
        if (_routeToServiceMap.ContainsValue(potentialServiceName))
        {
            return potentialServiceName;
        }

        return null;
    }

    private string ExtractRoutePrefix(string serviceName)
    {
        // First try to get from configuration
        var routePrefix = _configuration[$"ServiceRoutes:{serviceName}:RoutePrefix"];
        if (!string.IsNullOrEmpty(routePrefix))
        {
            return routePrefix.ToLower();
        }

        // Fallback: Convert "UserService" -> "users"
        // Convert "ProductService" -> "products"
        // Convert "OrderService" -> "orders"
        if (serviceName.EndsWith("Service", StringComparison.OrdinalIgnoreCase))
        {
            var baseName = serviceName.Substring(0, serviceName.Length - 7);
            return baseName.ToLower() + "s"; // Simple pluralization
        }
        return serviceName.ToLower();
    }

    public bool IsRouteMapped(string path)
    {
        return GetServiceNameFromPath(path) != null;
    }
}

