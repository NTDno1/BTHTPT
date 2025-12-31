using Microsoft.AspNetCore.Mvc;
using Microservice.Services.UserService.DTOs;
using Microservice.Services.UserService.Services;
using Microservice.Services.UserService.Models;

namespace Microservice.Services.UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserService userService, 
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _userService = userService;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { error = "Username and password are required" });
        }

        try
        {
            // Validate user credentials
            var user = await _userService.ValidateUserAsync(request.Username, request.Password);
            
            if (user == null)
            {
                _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
                return Unauthorized(new { error = "Invalid username or password" });
            }

            // Generate JWT token
            var token = _jwtService.GenerateToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

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

            var response = new LoginResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                User = userDto,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60) // Default 60 minutes
            };

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
            return StatusCode(500, new { error = "An error occurred during login" });
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponseDto>> Register([FromBody] CreateUserDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Create user
            var userDto = await _userService.CreateUserAsync(request);

            // Get the created user to generate token
            var user = await _userService.ValidateUserAsync(request.Username, request.Password);
            
            if (user == null)
            {
                return StatusCode(500, new { error = "User created but login failed" });
            }

            // Generate JWT token
            var token = _jwtService.GenerateToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            var response = new LoginResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                User = userDto,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            };

            _logger.LogInformation("User {UserId} registered and logged in successfully", userDto.Id);

            return CreatedAtAction(nameof(Login), response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for username: {Username}", request.Username);
            return StatusCode(500, new { error = "An error occurred during registration" });
        }
    }
}

