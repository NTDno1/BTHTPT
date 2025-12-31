namespace Microservice.Services.ProductService.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public bool HasDiscount => DiscountPrice.HasValue && 
        (!DiscountStartDate.HasValue || DiscountStartDate <= DateTime.UtcNow) &&
        (!DiscountEndDate.HasValue || DiscountEndDate >= DateTime.UtcNow);
    public DateTime? DiscountStartDate { get; set; }
    public DateTime? DiscountEndDate { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public List<string> Tags { get; set; } = new();
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public List<string>? Tags { get; set; }
}

public class UpdateProductDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int? Stock { get; set; }
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public bool? IsAvailable { get; set; }
    public List<string>? Tags { get; set; }
}

public class CreateProductReviewDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; } = false;
}

public class UpdateProductReviewDto
{
    public int? Rating { get; set; }
    public string? Comment { get; set; }
}

public class ProductDiscountDto
{
    public decimal? DiscountPrice { get; set; }
    public DateTime? DiscountStartDate { get; set; }
    public DateTime? DiscountEndDate { get; set; }
}

public class ProductSearchDto
{
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public List<string>? Tags { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinRating { get; set; }
    public bool? InStock { get; set; }
    public bool? HasDiscount { get; set; }
    public string? SortBy { get; set; } // "price", "rating", "name", "created"
    public string? SortOrder { get; set; } // "asc", "desc"
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
