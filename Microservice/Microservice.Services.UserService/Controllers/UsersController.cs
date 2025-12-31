using Microsoft.AspNetCore.Mvc;
using Microservice.Services.UserService.DTOs;
using Microservice.Services.UserService.Services;

namespace Microservice.Services.UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound($"User with ID {id} not found.");

        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var user = await _userService.CreateUserAsync(createUserDto);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, "An error occurred while creating the user.");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userService.UpdateUserAsync(id, updateUserDto);
        if (user == null)
            return NotFound($"User with ID {id} not found.");

        return Ok(user);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _userService.DeleteUserAsync(id);
        if (!result)
            return NotFound($"User with ID {id} not found.");

        return NoContent();
    }

    // Address Endpoints
    [HttpGet("{userId}/addresses")]
    public async Task<ActionResult<List<UserAddressDto>>> GetUserAddresses(int userId)
    {
        var addresses = await _userService.GetUserAddressesAsync(userId);
        return Ok(addresses);
    }

    [HttpPost("{userId}/addresses")]
    public async Task<ActionResult<UserAddressDto>> AddUserAddress(int userId, [FromBody] CreateUserAddressDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var address = await _userService.AddUserAddressAsync(userId, dto);
            return CreatedAtAction(nameof(GetUserAddresses), new { userId }, address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding address for user {UserId}", userId);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("{userId}/addresses/{addressId}")]
    public async Task<ActionResult<UserAddressDto>> UpdateUserAddress(int userId, int addressId, [FromBody] UpdateUserAddressDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var address = await _userService.UpdateUserAddressAsync(userId, addressId, dto);
        if (address == null)
            return NotFound($"Address with ID {addressId} not found for user {userId}.");

        return Ok(address);
    }

    [HttpDelete("{userId}/addresses/{addressId}")]
    public async Task<IActionResult> DeleteUserAddress(int userId, int addressId)
    {
        var result = await _userService.DeleteUserAddressAsync(userId, addressId);
        if (!result)
            return NotFound($"Address with ID {addressId} not found for user {userId}.");

        return NoContent();
    }
}

