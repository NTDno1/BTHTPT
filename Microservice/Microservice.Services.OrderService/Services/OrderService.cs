using Microsoft.EntityFrameworkCore;
using Microservice.Services.OrderService.Data;
using Microservice.Services.OrderService.Models;
using Microservice.Services.OrderService.DTOs;
using Microservice.Common.Interfaces;
using Microservice.Common.Models;

namespace Microservice.Services.OrderService.Services;

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

    public OrderService(
        OrderDbContext context,
        IMessagePublisher messagePublisher,
        ILogger<OrderService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _messagePublisher = messagePublisher;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
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
        // Validate products và lấy thông tin từ ProductService
        var orderItems = new List<OrderItem>();
        decimal totalAmount = 0;

        foreach (var item in createOrderDto.OrderItems)
        {
            // Gọi ProductService để lấy thông tin sản phẩm
            // Trong thực tế, nên dùng HttpClient hoặc gRPC
            // Ở đây giả sử đã có thông tin sản phẩm
            var orderItem = new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = $"Product {item.ProductId}", // Sẽ được lấy từ ProductService
                Quantity = item.Quantity,
                UnitPrice = 0, // Sẽ được lấy từ ProductService
                SubTotal = 0
            };
            orderItem.SubTotal = orderItem.UnitPrice * orderItem.Quantity;
            totalAmount += orderItem.SubTotal;
            orderItems.Add(orderItem);
        }

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

        _logger.LogInformation("Order created: {OrderId} - User: {UserId}", order.Id, order.UserId);

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

