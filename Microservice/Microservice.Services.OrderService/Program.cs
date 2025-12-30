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
    dbContext.Database.EnsureCreated();
}

app.Run();
