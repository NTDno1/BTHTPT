using Microservice.Common.Models;

namespace Microservice.Services.OrderService.Models;

public class Order : BaseEntity
{
    public int UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Cancelled
    public string ShippingAddress { get; set; } = string.Empty;
    public List<OrderItem> OrderItems { get; set; } = new();
    // Tính năng mới
    public string? PaymentMethod { get; set; } // CreditCard, PayPal, CashOnDelivery
    public string? PaymentStatus { get; set; } // Pending, Paid, Failed, Refunded
    public string? PaymentTransactionId { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? ShippingCarrier { get; set; } // DHL, FedEx, UPS, etc.
    public string? TrackingNumber { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public string? Notes { get; set; }
    public List<OrderStatusHistory> StatusHistory { get; set; } = new();
}

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

public class OrderStatusHistory : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? ChangedBy { get; set; } // UserId hoặc System
}

