using Microsoft.EntityFrameworkCore;
using Microservice.Services.UserService.Data;
using Microservice.Services.UserService.Models;
using Microservice.Services.UserService.DTOs;

namespace Microservice.Services.UserService.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
    Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
    Task<bool> DeleteUserAsync(int id);
    // Authentication
    Task<User?> ValidateUserAsync(string username, string password);
    // Addresses
    Task<List<UserAddressDto>> GetUserAddressesAsync(int userId);
    Task<UserAddressDto> AddUserAddressAsync(int userId, CreateUserAddressDto dto);
    Task<UserAddressDto?> UpdateUserAddressAsync(int userId, int addressId, UpdateUserAddressDto dto);
    Task<bool> DeleteUserAddressAsync(int userId, int addressId);
}

public class UserService : IUserService
{
    private readonly UserDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(UserDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .Include(u => u.Addresses.Where(a => !a.IsDeleted))
            .Where(u => !u.IsDeleted)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                Role = u.Role,
                AvatarUrl = u.AvatarUrl,
                Addresses = u.Addresses.Where(a => !a.IsDeleted).Select(a => new UserAddressDto
                {
                    Id = a.Id,
                    FullName = a.FullName,
                    PhoneNumber = a.PhoneNumber,
                    Street = a.Street,
                    City = a.City,
                    State = a.State,
                    PostalCode = a.PostalCode,
                    Country = a.Country,
                    IsDefault = a.IsDefault
                }).ToList(),
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return users;
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.Addresses.Where(a => !a.IsDeleted))
            .Where(u => u.Id == id && !u.IsDeleted)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                Role = u.Role,
                AvatarUrl = u.AvatarUrl,
                Addresses = u.Addresses.Where(a => !a.IsDeleted).Select(a => new UserAddressDto
                {
                    Id = a.Id,
                    FullName = a.FullName,
                    PhoneNumber = a.PhoneNumber,
                    Street = a.Street,
                    City = a.City,
                    State = a.State,
                    PostalCode = a.PostalCode,
                    Country = a.Country,
                    IsDefault = a.IsDefault
                }).ToList(),
                CreatedAt = u.CreatedAt
            })
            .FirstOrDefaultAsync();

        return user;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
    {
        // Hash password (trong thực tế nên dùng BCrypt hoặc Identity)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);

        var user = new User
        {
            Username = createUserDto.Username,
            Email = createUserDto.Email,
            PasswordHash = passwordHash,
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            PhoneNumber = createUserDto.PhoneNumber,
            Role = createUserDto.Role ?? "Customer",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User created: {UserId} - {Username}", user.Id, user.Username);

        return new UserDto
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
            Addresses = new List<UserAddressDto>(),
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

        if (user == null)
            return null;

        if (!string.IsNullOrEmpty(updateUserDto.FirstName))
            user.FirstName = updateUserDto.FirstName;

        if (!string.IsNullOrEmpty(updateUserDto.LastName))
            user.LastName = updateUserDto.LastName;

        if (updateUserDto.PhoneNumber != null)
            user.PhoneNumber = updateUserDto.PhoneNumber;

        if (updateUserDto.IsActive.HasValue)
            user.IsActive = updateUserDto.IsActive.Value;

        if (!string.IsNullOrEmpty(updateUserDto.Role))
            user.Role = updateUserDto.Role;

        if (updateUserDto.AvatarUrl != null)
            user.AvatarUrl = updateUserDto.AvatarUrl;

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User updated: {UserId}", user.Id);

        return new UserDto
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
            Addresses = user.Addresses.Where(a => !a.IsDeleted).Select(a => new UserAddressDto
            {
                Id = a.Id,
                FullName = a.FullName,
                PhoneNumber = a.PhoneNumber,
                Street = a.Street,
                City = a.City,
                State = a.State,
                PostalCode = a.PostalCode,
                Country = a.Country,
                IsDefault = a.IsDefault
            }).ToList(),
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

        if (user == null)
            return false;

        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User deleted: {UserId}", user.Id);

        return true;
    }

    public async Task<User?> ValidateUserAsync(string username, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => (u.Username == username || u.Email == username) && !u.IsDeleted && u.IsActive);

        if (user == null)
            return null;

        // Verify password
        var isValidPassword = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        if (!isValidPassword)
            return null;

        return user;
    }

    // Address Management
    public async Task<List<UserAddressDto>> GetUserAddressesAsync(int userId)
    {
        var addresses = await _context.UserAddresses
            .Where(a => a.UserId == userId && !a.IsDeleted)
            .Select(a => new UserAddressDto
            {
                Id = a.Id,
                FullName = a.FullName,
                PhoneNumber = a.PhoneNumber,
                Street = a.Street,
                City = a.City,
                State = a.State,
                PostalCode = a.PostalCode,
                Country = a.Country,
                IsDefault = a.IsDefault
            })
            .ToListAsync();

        return addresses;
    }

    public async Task<UserAddressDto> AddUserAddressAsync(int userId, CreateUserAddressDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null)
            throw new Exception($"User with ID {userId} not found.");

        // Nếu đặt làm default, bỏ default của các address khác
        if (dto.IsDefault)
        {
            var existingAddresses = await _context.UserAddresses
                .Where(a => a.UserId == userId && !a.IsDeleted)
                .ToListAsync();
            foreach (var addr in existingAddresses)
            {
                addr.IsDefault = false;
            }
        }

        var address = new UserAddress
        {
            UserId = userId,
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            Street = dto.Street,
            City = dto.City,
            State = dto.State,
            PostalCode = dto.PostalCode,
            Country = dto.Country,
            IsDefault = dto.IsDefault
        };

        _context.UserAddresses.Add(address);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Address added for user {UserId}", userId);

        return new UserAddressDto
        {
            Id = address.Id,
            FullName = address.FullName,
            PhoneNumber = address.PhoneNumber,
            Street = address.Street,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            Country = address.Country,
            IsDefault = address.IsDefault
        };
    }

    public async Task<UserAddressDto?> UpdateUserAddressAsync(int userId, int addressId, UpdateUserAddressDto dto)
    {
        var address = await _context.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId && !a.IsDeleted);

        if (address == null)
            return null;

        if (!string.IsNullOrEmpty(dto.FullName))
            address.FullName = dto.FullName;

        if (!string.IsNullOrEmpty(dto.PhoneNumber))
            address.PhoneNumber = dto.PhoneNumber;

        if (!string.IsNullOrEmpty(dto.Street))
            address.Street = dto.Street;

        if (!string.IsNullOrEmpty(dto.City))
            address.City = dto.City;

        if (!string.IsNullOrEmpty(dto.State))
            address.State = dto.State;

        if (!string.IsNullOrEmpty(dto.PostalCode))
            address.PostalCode = dto.PostalCode;

        if (!string.IsNullOrEmpty(dto.Country))
            address.Country = dto.Country;

        if (dto.IsDefault.HasValue && dto.IsDefault.Value)
        {
            // Bỏ default của các address khác
            var otherAddresses = await _context.UserAddresses
                .Where(a => a.UserId == userId && a.Id != addressId && !a.IsDeleted)
                .ToListAsync();
            foreach (var addr in otherAddresses)
            {
                addr.IsDefault = false;
            }
            address.IsDefault = true;
        }
        else if (dto.IsDefault.HasValue)
        {
            address.IsDefault = dto.IsDefault.Value;
        }

        address.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Address {AddressId} updated for user {UserId}", addressId, userId);

        return new UserAddressDto
        {
            Id = address.Id,
            FullName = address.FullName,
            PhoneNumber = address.PhoneNumber,
            Street = address.Street,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            Country = address.Country,
            IsDefault = address.IsDefault
        };
    }

    public async Task<bool> DeleteUserAddressAsync(int userId, int addressId)
    {
        var address = await _context.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId && !a.IsDeleted);

        if (address == null)
            return false;

        address.IsDeleted = true;
        address.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Address {AddressId} deleted for user {UserId}", addressId, userId);

        return true;
    }
}

