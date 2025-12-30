# Script để dừng tất cả các processes dotnet đang chạy
# Sử dụng: .\stop-all-services.ps1

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Dừng Tất Cả Microservices" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Tìm tất cả processes dotnet đang chạy
$processes = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue

if ($processes.Count -eq 0) {
    Write-Host "✅ Không có service nào đang chạy" -ForegroundColor Green
    exit 0
}

Write-Host "Tìm thấy $($processes.Count) process(es) dotnet đang chạy" -ForegroundColor Yellow
Write-Host ""

# Hỏi xác nhận
$confirm = Read-Host "Bạn có muốn dừng tất cả? (Y/N)"
if ($confirm -ne "Y" -and $confirm -ne "y") {
    Write-Host "Đã hủy." -ForegroundColor Yellow
    exit 0
}

# Dừng tất cả processes
Write-Host "Đang dừng các services..." -ForegroundColor Yellow
foreach ($process in $processes) {
    try {
        Stop-Process -Id $process.Id -Force
        Write-Host "✅ Đã dừng process: $($process.Id)" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Không thể dừng process: $($process.Id)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  ✅ Đã dừng tất cả services!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

