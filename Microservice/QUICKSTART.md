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
cd Microservice.ApiGateway && dotnet run
```

### Bước 3: Chạy Frontend

```bash
cd Microservice/Frontend
npm install
npm start
```

### Bước 4: Truy Cập

- **Frontend:** http://localhost:4200
- **API Gateway:** http://localhost:5000/swagger
- **User Service:** http://localhost:5001/swagger
- **Product Service:** http://localhost:5002/swagger
- **Order Service:** http://localhost:5003/swagger

---

## 📡 Test API

### Tạo User:
```bash
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{"username":"test","email":"test@example.com","password":"123","firstName":"Test","lastName":"User"}'
```

### Tạo Product:
```bash
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Laptop","description":"High performance","price":15000000,"stock":10,"category":"Electronics"}'
```

### Tạo Order:
```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{"userId":1,"shippingAddress":"123 Main St","orderItems":[{"productId":1,"quantity":2}]}'
```

---

## 🛑 Dừng Services

```powershell
.\stop-all-services.ps1
```

---

## 📝 Ports

| Service | Port |
|---------|------|
| API Gateway | 5000 |
| User Service | 5001 |
| Product Service | 5002 |
| Order Service | 5003 |
| Frontend | 4200 |

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
