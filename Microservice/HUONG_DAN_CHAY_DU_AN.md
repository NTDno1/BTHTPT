# 📖 Hướng Dẫn Chạy Dự Án

Hướng dẫn chi tiết từng bước để chạy dự án Microservice.

---

## 📋 Mục Lục

1. [Yêu Cầu Hệ Thống](#yêu-cầu-hệ-thống)
2. [Chuẩn Bị Databases](#chuẩn-bị-databases)
3. [Cách 1: Chạy Local](#cách-1-chạy-local)
4. [Cách 2: Chạy Bằng Docker](#cách-2-chạy-bằng-docker)
5. [Chạy Frontend](#chạy-frontend)
6. [Kiểm Tra và Test](#kiểm-tra-và-test)
7. [Troubleshooting](#troubleshooting)

---

## ✅ Yêu Cầu Hệ Thống

### Phần Mềm:
- **.NET 8.0 SDK** - https://dotnet.microsoft.com/download/dotnet/8.0
- **Node.js 18+** (cho Frontend)
- **Docker Desktop** (nếu chạy bằng Docker)

### Kết Nối:
- ✅ PostgreSQL: `47.130.33.106:5432`
- ✅ RabbitMQ: `47.130.33.106:5672`
- ✅ MongoDB Atlas (internet)

---

## 🗄️ Chuẩn Bị Databases

### 1. Tạo PostgreSQL Databases

Kết nối PostgreSQL và tạo 3 databases:

```sql
CREATE DATABASE userservice_db;
CREATE DATABASE productservice_db;
CREATE DATABASE orderservice_db;
```

**Thông tin kết nối:**
- Server: 47.130.33.106
- Port: 5432
- Username: postgres
- Password: 123456

### 2. Kiểm Tra MongoDB

MongoDB đã được cấu hình trong `appsettings.json`. Đảm bảo connection string đúng.

### 3. Kiểm Tra RabbitMQ

- Server: 47.130.33.106:5672
- Username: guest
- Password: guest

---

## 🚀 Cách 1: Chạy Local

### ⚡ Sử Dụng Script (Khuyến nghị)

```powershell
cd Microservice
.\run-all-services.ps1
```

Script sẽ tự động chạy tất cả services.

### 📝 Chạy Thủ Công

**Terminal 1 - User Service:**
```bash
cd Microservice/Microservice.Services.UserService
dotnet run
```
**Kết quả:** http://localhost:5001/swagger

**Terminal 2 - Product Service:**
```bash
cd Microservice/Microservice.Services.ProductService
dotnet run
```
**Kết quả:** http://localhost:5002/swagger

**Terminal 3 - Order Service:**
```bash
cd Microservice/Microservice.Services.OrderService
dotnet run
```
**Kết quả:** http://localhost:5003/swagger

**Terminal 4 - API Gateway:**
```bash
cd Microservice/Microservice.ApiGateway
dotnet run
```
**Kết quả:** http://localhost:5000/swagger

### ⚠️ Lưu Ý

- **Thứ tự:** Chạy services trước, sau đó mới chạy API Gateway
- **Ports:** Đảm bảo ports 5000-5003 không bị chiếm

---

## 🐳 Cách 2: Chạy Bằng Docker

### Bước 1: Build và Chạy

```bash
cd Microservice
docker-compose up -d --build
```

### Bước 2: Kiểm Tra

```bash
docker-compose ps
```

### Bước 3: Xem Logs

```bash
docker-compose logs -f
```

### Dừng Services

```bash
docker-compose down
```

---

## 🎨 Chạy Frontend

```bash
cd Microservice/Frontend
npm install
npm start
```

**Truy cập:** http://localhost:4200

---

## ✅ Kiểm Tra và Test

### 1. Kiểm Tra Services

Truy cập Swagger UI:
- API Gateway: http://localhost:5000/swagger
- User Service: http://localhost:5001/swagger
- Product Service: http://localhost:5002/swagger
- Order Service: http://localhost:5003/swagger

### 2. Test API

**Tạo User:**
```bash
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{"username":"test","email":"test@example.com","password":"123","firstName":"Test","lastName":"User"}'
```

**Tạo Product:**
```bash
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Laptop","description":"High performance","price":15000000,"stock":10,"category":"Electronics"}'
```

**Tạo Order:**
```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{"userId":1,"shippingAddress":"123 Main St","orderItems":[{"productId":1,"quantity":2}]}'
```

---

## 🔧 Troubleshooting

### Lỗi Kết Nối PostgreSQL

- Kiểm tra server `47.130.33.106:5432` có thể truy cập
- Kiểm tra databases đã được tạo
- Kiểm tra username/password: `postgres/123456`

### Lỗi Kết Nối MongoDB

- Kiểm tra connection string trong appsettings.json
- Kiểm tra MongoDB Atlas cluster đang hoạt động
- Kiểm tra network access (whitelist IP)

### Lỗi Kết Nối RabbitMQ

- Kiểm tra server `47.130.33.106:5672`
- Kiểm tra credentials: `guest/guest`
- Kiểm tra firewall/network

### Port Đã Được Sử Dụng

**Windows:**
```powershell
netstat -ano | findstr :5001
taskkill /PID <PID> /F
```

**Linux/Mac:**
```bash
lsof -ti:5001 | xargs kill -9
```

### API Gateway Không Route Được

- Đảm bảo các services đã chạy trước
- Kiểm tra file `ocelot.json`
- Kiểm tra ports trong ocelot.json khớp với services

---

## 📚 Xem Thêm

- **Quick Start:** [QUICKSTART.md](./QUICKSTART.md)
- **Kịch bản demo:** [KICH_BAN_DEMO.md](./KICH_BAN_DEMO.md)
- **Kiến trúc:** [ARCHITECTURE.md](./ARCHITECTURE.md)
