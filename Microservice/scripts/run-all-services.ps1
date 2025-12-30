# Script để chạy tất cả Microservices
# Sử dụng: .\run-all-services.ps1

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Microservice Architecture - Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Kiểm tra .NET SDK
Write-Host "Đang kiểm tra .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ .NET SDK chưa được cài đặt!" -ForegroundColor Red
    Write-Host "Vui lòng cài đặt .NET 8.0 SDK từ: https://dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}
Write-Host "✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
Write-Host ""

# Kiểm tra thư mục
if (-not (Test-Path "MicroserviceArchitecture.sln")) {
    Write-Host "❌ Không tìm thấy solution file!" -ForegroundColor Red
    Write-Host "Vui lòng chạy script này trong thư mục Microservice" -ForegroundColor Red
    exit 1
}

# Restore packages
Write-Host "Đang restore packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Lỗi khi restore packages!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Packages đã được restore" -ForegroundColor Green
Write-Host ""

# Build solution
Write-Host "Đang build solution..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Lỗi khi build!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build thành công" -ForegroundColor Green
Write-Host ""

# Thông báo
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Các services sẽ được chạy trong các cửa sổ mới" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Swagger UI sẽ có tại:" -ForegroundColor Yellow
Write-Host "  - API Gateway:    http://localhost:5000/swagger" -ForegroundColor White
Write-Host "  - User Service:    http://localhost:5001/swagger" -ForegroundColor White
Write-Host "  - Product Service: http://localhost:5002/swagger" -ForegroundColor White
Write-Host "  - Order Service:   http://localhost:5003/swagger" -ForegroundColor White
Write-Host ""
Write-Host "Nhấn Ctrl+C trong mỗi cửa sổ để dừng service tương ứng" -ForegroundColor Yellow
Write-Host ""

# Hỏi xác nhận
$confirm = Read-Host "Bạn có muốn chạy tất cả services? (Y/N)"
if ($confirm -ne "Y" -and $confirm -ne "y") {
    Write-Host "Đã hủy." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Đang khởi động các services..." -ForegroundColor Green
Write-Host ""

# Chạy User Service
Write-Host "🚀 Khởi động User Service..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\Microservice.Services.UserService'; dotnet run" -WindowStyle Normal

Start-Sleep -Seconds 3

# Chạy Product Service
Write-Host "🚀 Khởi động Product Service..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\Microservice.Services.ProductService'; dotnet run" -WindowStyle Normal

Start-Sleep -Seconds 3

# Chạy Order Service
Write-Host "🚀 Khởi động Order Service..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\Microservice.Services.OrderService'; dotnet run" -WindowStyle Normal

Start-Sleep -Seconds 3

# Chạy API Gateway
Write-Host "🚀 Khởi động API Gateway..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\Microservice.ApiGateway'; dotnet run" -WindowStyle Normal

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  ✅ Tất cả services đã được khởi động!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Các cửa sổ PowerShell mới đã được mở cho mỗi service." -ForegroundColor Yellow
Write-Host "Đợi vài giây để các services khởi động hoàn toàn..." -ForegroundColor Yellow
Write-Host ""
Write-Host "Sau đó truy cập Swagger UI để test APIs!" -ForegroundColor Cyan
Write-Host ""

