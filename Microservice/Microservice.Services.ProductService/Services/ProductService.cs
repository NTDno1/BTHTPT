using Microsoft.EntityFrameworkCore;
using Microservice.Services.ProductService.Data;
using Microservice.Services.ProductService.Models;
using Microservice.Services.ProductService.DTOs;

namespace Microservice.Services.ProductService.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetAllProductsAsync();
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<List<ProductDto>> GetProductsByCategoryAsync(string category);
    Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto);
    Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto);
    Task<bool> DeleteProductAsync(int id);
    Task<bool> UpdateStockAsync(int id, int quantity);
}

public class ProductService : IProductService
{
    private readonly ProductDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(ProductDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ProductDto>> GetAllProductsAsync()
    {
        var products = await _context.Products
            .Where(p => !p.IsDeleted)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                Category = p.Category,
                ImageUrl = p.ImageUrl,
                IsAvailable = p.IsAvailable,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return products;
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var product = await _context.Products
            .Where(p => p.Id == id && !p.IsDeleted)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                Category = p.Category,
                ImageUrl = p.ImageUrl,
                IsAvailable = p.IsAvailable,
                CreatedAt = p.CreatedAt
            })
            .FirstOrDefaultAsync();

        return product;
    }

    public async Task<List<ProductDto>> GetProductsByCategoryAsync(string category)
    {
        var products = await _context.Products
            .Where(p => !p.IsDeleted && p.Category == category)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                Category = p.Category,
                ImageUrl = p.ImageUrl,
                IsAvailable = p.IsAvailable,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return products;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto)
    {
        var product = new Product
        {
            Name = createProductDto.Name,
            Description = createProductDto.Description,
            Price = createProductDto.Price,
            Stock = createProductDto.Stock,
            Category = createProductDto.Category,
            ImageUrl = createProductDto.ImageUrl,
            IsAvailable = createProductDto.Stock > 0
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product created: {ProductId} - {ProductName}", product.Id, product.Name);

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            Category = product.Category,
            ImageUrl = product.ImageUrl,
            IsAvailable = product.IsAvailable,
            CreatedAt = product.CreatedAt
        };
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (product == null)
            return null;

        if (!string.IsNullOrEmpty(updateProductDto.Name))
            product.Name = updateProductDto.Name;

        if (!string.IsNullOrEmpty(updateProductDto.Description))
            product.Description = updateProductDto.Description;

        if (updateProductDto.Price.HasValue)
            product.Price = updateProductDto.Price.Value;

        if (updateProductDto.Stock.HasValue)
            product.Stock = updateProductDto.Stock.Value;

        if (!string.IsNullOrEmpty(updateProductDto.Category))
            product.Category = updateProductDto.Category;

        if (updateProductDto.ImageUrl != null)
            product.ImageUrl = updateProductDto.ImageUrl;

        if (updateProductDto.IsAvailable.HasValue)
            product.IsAvailable = updateProductDto.IsAvailable.Value;

        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Product updated: {ProductId}", product.Id);

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            Category = product.Category,
            ImageUrl = product.ImageUrl,
            IsAvailable = product.IsAvailable,
            CreatedAt = product.CreatedAt
        };
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (product == null)
            return false;

        product.IsDeleted = true;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Product deleted: {ProductId}", product.Id);

        return true;
    }

    public async Task<bool> UpdateStockAsync(int id, int quantity)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (product == null)
            return false;

        product.Stock += quantity;
        product.IsAvailable = product.Stock > 0;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Product stock updated: {ProductId} - New stock: {Stock}", product.Id, product.Stock);

        return true;
    }
}

