using Microsoft.AspNetCore.Mvc;

namespace Microservice.ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", service = "ApiGateway", timestamp = DateTime.UtcNow });
    }
}

