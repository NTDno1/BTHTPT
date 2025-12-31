using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microservice.Services.UserService.Data;
using Microservice.Services.UserService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "User Service API",
        Version = "v1",
        Description = "API cho quản lý người dùng trong hệ thống Microservice",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Microservice Team"
        }
    });
});

// Database Configuration - PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? builder.Configuration.GetConnectionString("PostgreSQL")
    ?? "Host=47.130.33.106;Port=5432;Database=userservice_db;Username=postgres;Password=123456";

builder.Services.AddDbContext<UserDbContext>(options =>
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

// JWT Configuration
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] 
    ?? "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Microservice";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MicroserviceUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// Register Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtService, JwtService>();
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API v1");
    c.RoutePrefix = "swagger"; // Swagger UI ở /swagger
    c.DocumentTitle = "User Service API Documentation";
});

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Ensure database exists and all tables are created
        dbContext.Database.EnsureCreated();
        logger.LogInformation("Database ensured created");
        
        // Add missing columns to Users table if they don't exist
        try
        {
            // Check and add Role column
            dbContext.Database.ExecuteSqlRaw(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Users' AND column_name = 'Role'
                    ) THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""Role"" VARCHAR(50) NOT NULL DEFAULT 'Customer';
                    END IF;
                END $$;
            ");
            
            // Check and add AvatarUrl column
            dbContext.Database.ExecuteSqlRaw(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Users' AND column_name = 'AvatarUrl'
                    ) THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""AvatarUrl"" VARCHAR(500);
                    END IF;
                END $$;
            ");
            
            logger.LogInformation("Users table columns ensured");
        }
        catch (Exception colEx)
        {
            logger.LogWarning(colEx, "Users table column update skipped");
        }
        
        // Try to create UserAddresses table if it doesn't exist
        // This handles the case where EnsureCreated() might not create all tables
        try
        {
            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""UserAddresses"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""UserId"" INTEGER NOT NULL,
                    ""FullName"" VARCHAR(100) NOT NULL,
                    ""PhoneNumber"" VARCHAR(20) NOT NULL,
                    ""Street"" VARCHAR(200) NOT NULL,
                    ""City"" VARCHAR(100) NOT NULL,
                    ""State"" VARCHAR(100) NOT NULL,
                    ""PostalCode"" VARCHAR(20) NOT NULL,
                    ""Country"" VARCHAR(100) NOT NULL,
                    ""IsDefault"" BOOLEAN NOT NULL DEFAULT FALSE,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" TIMESTAMP,
                    ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE,
                    CONSTRAINT ""FK_UserAddresses_Users_UserId"" 
                        FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
                );
            ");
            
            // Create index if it doesn't exist
            dbContext.Database.ExecuteSqlRaw(@"
                CREATE INDEX IF NOT EXISTS ""IX_UserAddresses_UserId"" ON ""UserAddresses"" (""UserId"");
            ");
            
            logger.LogInformation("UserAddresses table ensured");
        }
        catch (Exception tableEx)
        {
            // Table might already exist, which is fine
            logger.LogWarning(tableEx, "UserAddresses table creation skipped (might already exist)");
        }
    }
    catch (Exception ex)
    {
        // Log error but don't crash the app
        logger.LogError(ex, "Error creating/migrating database: {Message}", ex.Message);
    }
}

app.Run();
