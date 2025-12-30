using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microservice.Services.OrderService.Data;
using Microservice.Services.OrderService.Models;
using Microservice.Services.OrderService.DTOs;
using Microservice.Common.Interfaces;
using Microservice.Common.Models;
using System.Text.Json;
using System.Net.Http.Json;

namespace Microservice.Services.OrderService.Services;

// DTO để nhận thông tin sản phẩm từ ProductService
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface IOrderService
{
    Task<List<OrderDto>> GetAllOrdersAsync();
    Task<List<OrderDto>> GetOrdersByUserIdAsync(int userId);
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<OrderDto> CreateOrderAsync(CreateOrderDto createOrderDto);
    Task<OrderDto?> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto updateOrderStatusDto);
    Task<bool> DeleteOrderAsync(int id);
}

public class OrderService : IOrderService
{
    private readonly OrderDbContext _context;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<OrderService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OrderService(
        OrderDbContext context,
        IMessagePublisher messagePublisher,
        ILogger<OrderService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _context = context;
        _messagePublisher = messagePublisher;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _configuration = configuration;
    }

    public async Task<List<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _context.Orders
            .Where(o => !o.IsDeleted)
            .Include(o => o.OrderItems)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                ShippingAddress = o.ShippingAddress,
                OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    SubTotal = oi.SubTotal
                }).ToList(),
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return orders;
    }

    public async Task<List<OrderDto>> GetOrdersByUserIdAsync(int userId)
    {
        var orders = await _context.Orders
            .Where(o => o.UserId == userId && !o.IsDeleted)
            .Include(o => o.OrderItems)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                ShippingAddress = o.ShippingAddress,
                OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    SubTotal = oi.SubTotal
                }).ToList(),
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return orders;
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        var order = await _context.Orders
            .Where(o => o.Id == id && !o.IsDeleted)
            .Include(o => o.OrderItems)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                ShippingAddress = o.ShippingAddress,
                OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    SubTotal = oi.SubTotal
                }).ToList(),
                CreatedAt = o.CreatedAt
            })
            .FirstOrDefaultAsync();

        return order;
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto createOrderDto)
    {
        var productServiceUrl = _configuration["ServiceUrls:ProductService"] ?? "http://localhost:5002";
        var orderItems = new List<OrderItem>();
        decimal totalAmount = 0;
        var productsToUpdate = new List<(int ProductId, int Quantity)>();

        // Validate products và lấy thông tin từ ProductService
        foreach (var item in createOrderDto.OrderItems)
        {
            try
            {
                // Gọi ProductService để lấy thông tin sản phẩm
                var response = await _httpClient.GetAsync($"{productServiceUrl}/api/products/{item.ProductId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("ProductService returned error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    throw new Exception($"Không tìm thấy sản phẩm với ID {item.ProductId}. Status: {response.StatusCode}");
                }

                // Thử deserialize trực tiếp từ response
                var product = await response.Content.ReadFromJsonAsync<ProductDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (product == null)
                {
                    // Nếu ReadFromJsonAsync trả về null, thử đọc string và deserialize thủ công
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("ReadFromJsonAsync returned null. Trying manual deserialize. Response: {Content}", jsonContent);
                    
                    product = JsonSerializer.Deserialize<ProductDto>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (product == null)
                    {
                        _logger.LogError("Failed to deserialize product {ProductId}. Response: {Content}", item.ProductId, jsonContent);
                        throw new Exception($"Không thể đọc thông tin sản phẩm với ID {item.ProductId}");
                    }
                }

                _logger.LogInformation("Product loaded: ID={Id}, Name={Name}, Price={Price}, Stock={Stock}", 
                    product.Id, product.Name, product.Price, product.Stock);

                // Kiểm tra tồn kho
                if (!product.IsAvailable || product.Stock < item.Quantity)
                {
                    throw new Exception($"Sản phẩm '{product.Name}' không đủ tồn kho. Tồn kho hiện tại: {product.Stock}, yêu cầu: {item.Quantity}");
                }

                // Tạo order item với thông tin đầy đủ
                var orderItem = new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price,
                    SubTotal = product.Price * item.Quantity
                };

                _logger.LogInformation("Order item created: ProductId={ProductId}, UnitPrice={UnitPrice}, Quantity={Quantity}, SubTotal={SubTotal}",
                    orderItem.ProductId, orderItem.UnitPrice, orderItem.Quantity, orderItem.SubTotal);
                
                orderItems.Add(orderItem);
                totalAmount += orderItem.SubTotal;
                productsToUpdate.Add((item.ProductId, -item.Quantity)); // Lưu để trừ tồn kho sau
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling ProductService for product {ProductId}", item.ProductId);
                throw new Exception($"Không thể kết nối đến ProductService: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing product {ProductId}", item.ProductId);
                throw;
            }
        }

        // Tạo đơn hàng
        var order = new Order
        {
            UserId = createOrderDto.UserId,
            TotalAmount = totalAmount,
            Status = "Pending",
            ShippingAddress = createOrderDto.ShippingAddress,
            OrderItems = orderItems
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Trừ tồn kho sau khi tạo đơn hàng thành công
        foreach (var (productId, quantity) in productsToUpdate)
        {
            try
            {
                var updateResponse = await _httpClient.PatchAsync(
                    $"{productServiceUrl}/api/products/{productId}/stock",
                    new StringContent(JsonSerializer.Serialize(quantity), System.Text.Encoding.UTF8, "application/json"));

                if (!updateResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to update stock for product {ProductId}", productId);
                    // Không throw exception ở đây để không rollback đơn hàng đã tạo
                    // Có thể implement compensation pattern sau
                }
                else
                {
                    _logger.LogInformation("Stock updated for product {ProductId}: {Quantity}", productId, quantity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product {ProductId}", productId);
                // Log error nhưng không throw để không rollback đơn hàng
            }
        }

        // Publish event to RabbitMQ
        var orderCreatedEvent = new MessageEvent
        {
            EventType = "OrderCreated",
            ServiceName = "OrderService",
            Data = new
            {
                OrderId = order.Id,
                UserId = order.UserId,
                TotalAmount = order.TotalAmount,
                OrderItems = order.OrderItems.Select(oi => new
                {
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity
                })
            }
        };

        _messagePublisher.Publish(orderCreatedEvent, "order.created");

        _logger.LogInformation("Order created: {OrderId} - User: {UserId} - Total: {TotalAmount}", 
            order.Id, order.UserId, order.TotalAmount);

        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            ShippingAddress = order.ShippingAddress,
            OrderItems = order.OrderItems.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductName = oi.ProductName,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                SubTotal = oi.SubTotal
            }).ToList(),
            CreatedAt = order.CreatedAt
        };
    }

    public async Task<OrderDto?> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto updateOrderStatusDto)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (order == null)
            return null;

        order.Status = updateOrderStatusDto.Status;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Publish status update event
        var statusUpdatedEvent = new MessageEvent
        {
            EventType = "OrderStatusUpdated",
            ServiceName = "OrderService",
            Data = new
            {
                OrderId = order.Id,
                OldStatus = order.Status,
                NewStatus = updateOrderStatusDto.Status
            }
        };

        _messagePublisher.Publish(statusUpdatedEvent, "order.status.updated");

        _logger.LogInformation("Order status updated: {OrderId} - Status: {Status}", order.Id, order.Status);

        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            ShippingAddress = order.ShippingAddress,
            OrderItems = order.OrderItems.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductName = oi.ProductName,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                SubTotal = oi.SubTotal
            }).ToList(),
            CreatedAt = order.CreatedAt
        };
    }

    public async Task<bool> DeleteOrderAsync(int id)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (order == null)
            return false;

        order.IsDeleted = true;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Order deleted: {OrderId}", order.Id);

        return true;
    }
}

