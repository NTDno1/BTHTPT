using Microservice.ApiGateway.RabbitMQ.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API Gateway RabbitMQ",
        Version = "v1",
        Description = "API Gateway sử dụng RabbitMQ để điều hướng requests đến các microservices thông qua message queue. Đây là điểm vào thứ hai cho các API requests sử dụng message-based communication.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Microservice Team"
        }
    });
});

// Register RabbitMQ Gateway Service as Singleton
builder.Services.AddSingleton<RabbitMQGatewayService>();

// Register Route Mapping Service
builder.Services.AddSingleton<RouteMappingService>();

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
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway RabbitMQ v1");
    c.RoutePrefix = "swagger"; // Swagger UI ở /swagger
    c.DocumentTitle = "API Gateway RabbitMQ Documentation";
});

app.UseCors("AllowAll");
app.UseHttpsRedirection();

// Enable request buffering to allow reading request body
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.UseAuthorization();
app.MapControllers();

app.Run();

