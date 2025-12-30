# Script để kiểm tra các services đang chạy
Write-Host "=== Checking Microservices Status ===" -ForegroundColor Cyan

# Kiểm tra UserService
Write-Host "`n[UserService]" -ForegroundColor Yellow
$userService = Get-Process -Name "Microservice.Services.UserService" -ErrorAction SilentlyContinue
if ($userService) {
    Write-Host "✓ UserService is running (PID: $($userService.Id))" -ForegroundColor Green
} else {
    Write-Host "✗ UserService is NOT running" -ForegroundColor Red
    Write-Host "  Please start it with: cd Microservice.Services.UserService && dotnet run" -ForegroundColor Yellow
}

# Kiểm tra OrderService
Write-Host "`n[OrderService]" -ForegroundColor Yellow
$orderService = Get-Process -Name "Microservice.Services.OrderService" -ErrorAction SilentlyContinue
if ($orderService) {
    Write-Host "✓ OrderService is running (PID: $($orderService.Id))" -ForegroundColor Green
} else {
    Write-Host "✗ OrderService is NOT running" -ForegroundColor Red
    Write-Host "  Please start it with: cd Microservice.Services.OrderService && dotnet run" -ForegroundColor Yellow
}

# Kiểm tra ProductService
Write-Host "`n[ProductService]" -ForegroundColor Yellow
$productService = Get-Process -Name "Microservice.Services.ProductService" -ErrorAction SilentlyContinue
if ($productService) {
    Write-Host "✓ ProductService is running (PID: $($productService.Id))" -ForegroundColor Green
} else {
    Write-Host "✗ ProductService is NOT running" -ForegroundColor Red
    Write-Host "  Please start it with: cd Microservice.Services.ProductService && dotnet run" -ForegroundColor Yellow
}

# Kiểm tra API Gateway RabbitMQ
Write-Host "`n[API Gateway RabbitMQ]" -ForegroundColor Yellow
$gateway = Get-Process -Name "Microservice.ApiGateway.RabbitMQ" -ErrorAction SilentlyContinue
if ($gateway) {
    Write-Host "✓ API Gateway RabbitMQ is running (PID: $($gateway.Id))" -ForegroundColor Green
} else {
    Write-Host "✗ API Gateway RabbitMQ is NOT running" -ForegroundColor Red
    Write-Host "  Please start it with: cd Microservice.ApiGateway.RabbitMQ && dotnet run" -ForegroundColor Yellow
}

Write-Host "`n=== Check Complete ===" -ForegroundColor Cyan
Write-Host "`nIMPORTANT: After adding RabbitMQ Consumer Service, you MUST restart the service!" -ForegroundColor Red
Write-Host "The consumer service starts automatically when the service starts." -ForegroundColor Yellow

