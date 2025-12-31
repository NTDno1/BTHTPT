using Microsoft.EntityFrameworkCore;
using Microservice.Services.OrderService.Data;
using Microservice.Services.OrderService.Services;
using Microservice.Common.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Order Service API",
        Version = "v1",
        Description = "API cho quản lý đơn hàng trong hệ thống Microservice. Service này tích hợp với RabbitMQ để giao tiếp bất đồng bộ.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Microservice Team"
        }
    });
});

// Database Configuration - PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? builder.Configuration.GetConnectionString("PostgreSQL")
    ?? "Host=47.130.33.106;Port=5432;Database=orderservice_db;Username=postgres;Password=123456";

builder.Services.AddDbContext<OrderDbContext>(options =>
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

// HttpClient for calling other services
builder.Services.AddHttpClient();

// Register Services
builder.Services.AddSingleton<IMessagePublisher, RabbitMQService>();
builder.Services.AddScoped<IOrderService, OrderService>();
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service API v1");
    c.RoutePrefix = "swagger"; // Swagger UI ở /swagger
    c.DocumentTitle = "Order Service API Documentation";
});

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Ensure database exists and all tables are created
        dbContext.Database.EnsureCreated();
        logger.LogInformation("Database ensured created");
        
        // Add missing columns to Orders table if they don't exist
        try
        {
            // PaymentMethod
            dbContext.Database.ExecuteSqlRaw(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Orders' AND column_name = 'PaymentMethod'
                    ) THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""PaymentMethod"" VARCHAR(50);
                    END IF;
                END $$;
            ");
            
            // PaymentStatus
            dbContext.Database.ExecuteSqlRaw(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Orders' AND column_name = 'PaymentStatus'
                    ) THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""PaymentStatus"" VARCHAR(50);
                    END IF;
                END $$;
            ");
            
            // PaymentTransactionId
            dbContext.Database.ExecuteSqlRaw(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Orders' AND column_name = 'PaymentTransactionId'
                    ) THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""PaymentTransactionId"" VARCHAR(200);
                    END IF;
                END $$;
            ");
            
            // PaymentDate
            dbContext.Database.ExecuteSqlRaw(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Orders' AND column_name = 'PaymentDate'
                    ) THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""PaymentDate"" TIMESTAMP;
                    END IF;
                END $$;
            ");
            
            // ShippingCarrier
            dbContext.Database.ExecuteSqlRaw(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Orders' AND column_name = 'ShippingCarrier'
                    ) THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""ShippingCarrier"" VARCHAR(100);
                    END IF;
                END $$;
            ");
            
            // TrackingNumber
            dbContext.Database.ExecuteSqlRaw(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Orders' AND column_name = 'TrackingNumber'
                    ) THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""TrackingNumber"" VARCHAR(100);
                    END IF;
                END $$;
            ");
            
            // ShippedDate
            dbContext.Database.ExecuteSqlRaw(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Orders' AND column_name = 'ShippedDate'
                    ) THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""ShippedDate"" TIMESTAMP;
                    END IF;
                END $$;
            ");
            
            // DeliveredDate
            dbContext.Database.ExecuteSqlRaw(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Orders' AND column_name = 'DeliveredDate'
                    ) THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""DeliveredDate"" TIMESTAMP;
                    END IF;
                END $$;
            ");
            
            // Notes
            dbContext.Database.ExecuteSqlRaw(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Orders' AND column_name = 'Notes'
                    ) THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""Notes"" VARCHAR(1000);
                    END IF;
                END $$;
            ");
            
            logger.LogInformation("Orders table columns ensured");
        }
        catch (Exception colEx)
        {
            logger.LogWarning(colEx, "Orders table column update skipped");
        }
        
        // Try to create OrderItems table if it doesn't exist
        try
        {
            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""OrderItems"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""OrderId"" INTEGER NOT NULL,
                    ""ProductId"" INTEGER NOT NULL,
                    ""ProductName"" VARCHAR(200) NOT NULL,
                    ""Quantity"" INTEGER NOT NULL,
                    ""UnitPrice"" NUMERIC(18,2) NOT NULL,
                    ""SubTotal"" NUMERIC(18,2) NOT NULL,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" TIMESTAMP,
                    ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""FK_OrderItems_Orders_OrderId"" 
                        FOREIGN KEY (""OrderId"") REFERENCES ""Orders"" (""Id"") ON DELETE CASCADE
                );
            ");
            
            // Create index
            dbContext.Database.ExecuteSqlRaw(@"
                CREATE INDEX IF NOT EXISTS ""IX_OrderItems_OrderId"" ON ""OrderItems"" (""OrderId"");
            ");
            
            logger.LogInformation("OrderItems table ensured");
        }
        catch (Exception tableEx)
        {
            logger.LogWarning(tableEx, "OrderItems table creation skipped (might already exist)");
        }
        
        // Try to create OrderStatusHistory table if it doesn't exist
        try
        {
            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""OrderStatusHistory"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""OrderId"" INTEGER NOT NULL,
                    ""Status"" VARCHAR(50) NOT NULL,
                    ""Notes"" VARCHAR(500),
                    ""ChangedBy"" VARCHAR(100),
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" TIMESTAMP,
                    ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""FK_OrderStatusHistory_Orders_OrderId"" 
                        FOREIGN KEY (""OrderId"") REFERENCES ""Orders"" (""Id"") ON DELETE CASCADE
                );
            ");
            
            // Create index
            dbContext.Database.ExecuteSqlRaw(@"
                CREATE INDEX IF NOT EXISTS ""IX_OrderStatusHistory_OrderId"" ON ""OrderStatusHistory"" (""OrderId"");
            ");
            
            logger.LogInformation("OrderStatusHistory table ensured");
        }
        catch (Exception tableEx)
        {
            logger.LogWarning(tableEx, "OrderStatusHistory table creation skipped (might already exist)");
        }
    }
    catch (Exception ex)
    {
        // Log error but don't crash the app
        logger.LogError(ex, "Error creating/migrating database: {Message}", ex.Message);
    }
}

app.Run();
