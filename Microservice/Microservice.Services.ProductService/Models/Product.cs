using Microservice.Common.Models;

namespace Microservice.Services.ProductService.Models;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; } = true;
    // Tính năng mới
    public decimal? DiscountPrice { get; set; }
    public DateTime? DiscountStartDate { get; set; }
    public DateTime? DiscountEndDate { get; set; }
    public List<ProductTag> Tags { get; set; } = new();
    public List<ProductReview> Reviews { get; set; } = new();
}

public class ProductReview : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; } // 1-5
    public string Comment { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; } = false;
}

public class ProductTag : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string TagName { get; set; } = string.Empty;
}

