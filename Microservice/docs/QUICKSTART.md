# ⚡ Quick Start Guide

Hướng dẫn nhanh để chạy dự án Microservice.

---

## ✅ Yêu Cầu

- .NET 8.0 SDK
- Node.js 18+ (cho Frontend)
- PostgreSQL server: 47.130.33.106:5432
- RabbitMQ server: 47.130.33.106:5672
- MongoDB Atlas (connection string trong appsettings.json)

---

## 🚀 Chạy Nhanh

### Bước 1: Tạo Databases

Kết nối PostgreSQL và tạo 3 databases:

```sql
CREATE DATABASE userservice_db;
CREATE DATABASE productservice_db;
CREATE DATABASE orderservice_db;
```

### Bước 2: Chạy Backend

**Cách 1: Script PowerShell (Khuyến nghị)**
```powershell
cd Microservice
.\run-all-services.ps1
```

**Cách 2: Chạy thủ công**
```bash
# Mở 4 terminals và chạy từng service
cd Microservice.Services.UserService && dotnet run
cd Microservice.Services.ProductService && dotnet run
cd Microservice.Services.OrderService && dotnet run
cd Microservice.ApiGateway.RabbitMQ && dotnet run
```

**Cách 3: Docker Compose (Khuyến nghị cho production)**
```bash
cd Microservice
docker-compose up -d --build
```

### Bước 3: Chạy Frontend

```bash
cd Microservice/Frontend
npm install
npm start
```

### Bước 4: Truy Cập

- **Frontend:** http://localhost:4200
- **API Gateway RabbitMQ (PRIMARY):** http://localhost:5010/swagger
- **User Service Instance 1:** http://localhost:5001/swagger
- **User Service Instance 2:** http://localhost:5004/swagger
- **Product Service Instance 1:** http://localhost:5002/swagger
- **Product Service Instance 2:** http://localhost:5006/swagger
- **Order Service Instance 1:** http://localhost:5003/swagger
- **Order Service Instance 2:** http://localhost:5007/swagger

**Lưu ý:** API Gateway Ocelot (port 5000) đã bị disable, chỉ sử dụng RabbitMQ Gateway (port 5010).

---

## 📡 Test API qua API Gateway RabbitMQ

### Đăng ký User:
```bash
curl -X POST http://localhost:5010/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"test","email":"test@example.com","password":"123456","firstName":"Test","lastName":"User"}'
```

### Đăng nhập:
```bash
curl -X POST http://localhost:5010/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"test","password":"123456"}'
```

### Tạo Product (cần JWT token):
```bash
curl -X POST http://localhost:5010/api/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{"name":"Laptop","description":"High performance","price":15000000,"stock":10,"category":"Electronics"}'
```

### Tạo Order:
```bash
curl -X POST http://localhost:5010/api/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{"userId":1,"shippingAddress":"123 Main St","orderItems":[{"productId":1,"quantity":2}]}'
```

---

## 🛑 Dừng Services

```powershell
.\stop-all-services.ps1
```

---

## 📝 Ports

| Service | Port | Notes |
|---------|------|-------|
| API Gateway RabbitMQ (PRIMARY) | 5010 | Entry point chính |
| User Service Instance 1 | 5001 | Load Balanced |
| User Service Instance 2 | 5004 | Load Balanced |
| Product Service Instance 1 | 5002 | Load Balanced |
| Product Service Instance 2 | 5006 | Load Balanced |
| Order Service Instance 1 | 5003 | Load Balanced |
| Order Service Instance 2 | 5007 | Load Balanced |
| Frontend | 4200 | Angular app |

**Lưu ý:** API Gateway Ocelot (port 5000) đã bị disable.

---

## 🔧 Troubleshooting

**Lỗi kết nối PostgreSQL:**
- Kiểm tra server 47.130.33.106:5432
- Kiểm tra databases đã được tạo

**Lỗi kết nối RabbitMQ:**
- Kiểm tra server 47.130.33.106:5672
- Kiểm tra credentials: guest/guest

**Port đã được sử dụng:**
```bash
netstat -ano | findstr :5001
taskkill /PID <PID> /F
```

---

## 📚 Xem Thêm

- **Hướng dẫn chi tiết:** [HUONG_DAN_CHAY_DU_AN.md](./HUONG_DAN_CHAY_DU_AN.md)
- **Kịch bản demo:** [KICH_BAN_DEMO.md](./KICH_BAN_DEMO.md)
