using Microsoft.EntityFrameworkCore;
using Microservice.Services.ProductService.Data;
using Microservice.Services.ProductService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Product Service API",
        Version = "v1",
        Description = "API cho quản lý sản phẩm trong hệ thống Microservice",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Microservice Team"
        }
    });
});

// Database Configuration - PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? builder.Configuration.GetConnectionString("PostgreSQL")
    ?? "Host=47.130.33.106;Port=5432;Database=productservice_db;Username=postgres;Password=123456";

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(connectionString));

// MongoDB Configuration
var mongoConnectionString = builder.Configuration["MongoDb:ConnectionString"];
var mongoDatabase = builder.Configuration["MongoDb:Database"];
if (!string.IsNullOrEmpty(mongoConnectionString) && !string.IsNullOrEmpty(mongoDatabase))
{
    builder.Services.AddSingleton<MongoDB.Driver.IMongoDatabase>(sp =>
    {
        var client = new MongoDB.Driver.MongoClient(mongoConnectionString);
        return client.GetDatabase(mongoDatabase);
    });
}

// Register Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddHostedService<RabbitMQConsumerService>();

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Swagger luôn được bật (không chỉ trong Development)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Service API v1");
    c.RoutePrefix = "swagger"; // Swagger UI ở /swagger
    c.DocumentTitle = "Product Service API Documentation";
});

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Ensure database exists and all tables are created
        dbContext.Database.EnsureCreated();
        logger.LogInformation("Database ensured created");
        
        // Add missing columns to Products table if they don't exist
        try
        {
            // Check and add DiscountPrice column
            dbContext.Database.ExecuteSqlRaw(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Products' AND column_name = 'DiscountPrice'
                    ) THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""DiscountPrice"" NUMERIC(18,2);
                    END IF;
                END $$;
            ");
            
            // Check and add DiscountStartDate column
            dbContext.Database.ExecuteSqlRaw(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Products' AND column_name = 'DiscountStartDate'
                    ) THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""DiscountStartDate"" TIMESTAMP;
                    END IF;
                END $$;
            ");
            
            // Check and add DiscountEndDate column
            dbContext.Database.ExecuteSqlRaw(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Products' AND column_name = 'DiscountEndDate'
                    ) THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""DiscountEndDate"" TIMESTAMP;
                    END IF;
                END $$;
            ");
            
            logger.LogInformation("Products table columns ensured");
        }
        catch (Exception colEx)
        {
            logger.LogWarning(colEx, "Products table column update skipped");
        }
        
        // Try to create ProductReviews table if it doesn't exist
        try
        {
            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""ProductReviews"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""ProductId"" INTEGER NOT NULL,
                    ""UserId"" INTEGER NOT NULL,
                    ""UserName"" VARCHAR(100) NOT NULL,
                    ""Rating"" INTEGER NOT NULL CHECK (""Rating"" >= 1 AND ""Rating"" <= 5),
                    ""Comment"" VARCHAR(1000),
                    ""IsVerifiedPurchase"" BOOLEAN NOT NULL DEFAULT FALSE,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" TIMESTAMP,
                    ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""FK_ProductReviews_Products_ProductId"" 
                        FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE CASCADE
                );
            ");
            
            // Create indexes
            dbContext.Database.ExecuteSqlRaw(@"
                CREATE INDEX IF NOT EXISTS ""IX_ProductReviews_ProductId"" ON ""ProductReviews"" (""ProductId"");
                CREATE INDEX IF NOT EXISTS ""IX_ProductReviews_UserId"" ON ""ProductReviews"" (""UserId"");
            ");
            
            logger.LogInformation("ProductReviews table ensured");
        }
        catch (Exception tableEx)
        {
            logger.LogWarning(tableEx, "ProductReviews table creation skipped (might already exist)");
        }
        
        // Try to create ProductTags table if it doesn't exist
        try
        {
            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""ProductTags"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""ProductId"" INTEGER NOT NULL,
                    ""TagName"" VARCHAR(50) NOT NULL,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" TIMESTAMP,
                    ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""FK_ProductTags_Products_ProductId"" 
                        FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE CASCADE
                );
            ");
            
            // Create indexes
            dbContext.Database.ExecuteSqlRaw(@"
                CREATE INDEX IF NOT EXISTS ""IX_ProductTags_ProductId"" ON ""ProductTags"" (""ProductId"");
                CREATE INDEX IF NOT EXISTS ""IX_ProductTags_TagName"" ON ""ProductTags"" (""TagName"");
            ");
            
            logger.LogInformation("ProductTags table ensured");
        }
        catch (Exception tableEx)
        {
            logger.LogWarning(tableEx, "ProductTags table creation skipped (might already exist)");
        }
    }
    catch (Exception ex)
    {
        // Log error but don't crash the app
        logger.LogError(ex, "Error creating/migrating database: {Message}", ex.Message);
    }
}

app.Run();
