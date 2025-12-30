using Microsoft.AspNetCore.Mvc;

namespace Microservice.ApiGateway.RabbitMQ.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            service = "API Gateway RabbitMQ",
            timestamp = DateTime.UtcNow
        });
    }
}

