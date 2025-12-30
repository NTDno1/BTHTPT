# 🏗️ Microservice Architecture - E-Commerce Backend

Dự án triển khai hệ thống **E-Commerce Backend** theo kiến trúc **Microservice** sử dụng .NET 8.0, dựa trên giáo trình "Các Hệ Thống Phân Tán" và best practices thực tế.

---

## 📚 Tài Liệu

| File | Mô Tả |
|------|-------|
| [TONG_QUAN_DU_AN.md](./TONG_QUAN_DU_AN.md) | Tổng quan tính năng và mục đích dự án |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Kiến trúc chi tiết và thiết kế |
| [HUONG_DAN_CHAY_DU_AN.md](./HUONG_DAN_CHAY_DU_AN.md) | Hướng dẫn chạy dự án từng bước |
| [QUICKSTART.md](./QUICKSTART.md) | Hướng dẫn nhanh để bắt đầu |
| [KICH_BAN_DEMO.md](./KICH_BAN_DEMO.md) | Kịch bản demo chi tiết |
| [GIAI_THICH_KIEN_TRUC.md](./GIAI_THICH_KIEN_TRUC.md) | Giải thích về kiến trúc |
| [Frontend/README.md](./Frontend/README.md) | Hướng dẫn Frontend Angular |

---

## 🎯 Tổng Quan Dự Án

Hệ thống bao gồm **4 microservices** độc lập:

- **👥 User Service** (Port 5001) - Quản lý người dùng
- **📦 Product Service** (Port 5002) - Quản lý sản phẩm  
- **🛒 Order Service** (Port 5003) - Quản lý đơn hàng với RabbitMQ
- **🚪 API Gateway** (Port 5000) - Điều hướng requests (Ocelot)

---

## 🏗️ Kiến Trúc Hệ Thống

```
┌─────────────┐
│   Frontend  │
│  (Angular)  │
│  :4200      │
└──────┬──────┘
       │ HTTP
       ▼
┌─────────────────┐
│   API Gateway    │ Port 5000
│    (Ocelot)     │
└────────┬────────┘
         │
    ┌────┴────┬──────────┬──────────┐
    │         │          │          │
    ▼         ▼          ▼          │
┌────────┐ ┌────────┐ ┌────────┐  │
│ User   │ │Product │ │ Order  │  │
│Service │ │Service │ │Service │  │
│ :5001  │ │ :5002  │ │ :5003  │  │
└────┬───┘ └────┬───┘ └────┬───┘  │
     │         │          │       │
     ▼         ▼          ▼       │
┌──────────┐ ┌──────────┐ │ ┌──────────┐
│userservice│ │product   │ │ │orderservice│
│   _db    │ │service_db│ │ │   _db    │
│PostgreSQL│ │PostgreSQL│ │ │PostgreSQL│
└──────────┘ └──────────┘ │ └──────────┘
     │         │          │       │
     └─────────┴──────────┴───────┘
                    │
        ┌───────────┴───────────┐
        │                       │
        ▼                       ▼
┌──────────────┐        ┌──────────────┐
│   MongoDB    │        │   RabbitMQ   │
│ (Logging)    │        │ (Messages)  │
└──────────────┘        └──────────────┘
```

> **💡 Lưu ý:** MongoDB và RabbitMQ là **infrastructure services** được các microservices sử dụng trực tiếp, không qua API Gateway.

---

## 🛠️ Công Nghệ

### Backend
- **.NET 8.0** - Framework
- **Entity Framework Core** - ORM
- **PostgreSQL** - Database (Npgsql)
- **MongoDB** - Logging/Events
- **RabbitMQ** - Message Queue
- **Ocelot** - API Gateway
- **Swagger** - API Documentation

### Frontend
- **Angular 17+** - Framework
- **Angular Material** - UI Components

---

## 🚀 Quick Start

### 1. Chạy Backend Services

**Cách 1: Sử dụng Script (Khuyến nghị)**
```powershell
cd Microservice
.\run-all-services.ps1
```

**Cách 2: Chạy thủ công**
```bash
# Terminal 1 - User Service
cd Microservice.Services.UserService
dotnet run

# Terminal 2 - Product Service  
cd Microservice.Services.ProductService
dotnet run

# Terminal 3 - Order Service
cd Microservice.Services.OrderService
dotnet run

# Terminal 4 - API Gateway
cd Microservice.ApiGateway
dotnet run
```

### 2. Chạy Frontend

```bash
cd Microservice/Frontend
npm install
npm start
```

### 3. Truy Cập

- **Frontend:** http://localhost:4200
- **API Gateway Swagger:** http://localhost:5000/swagger
- **User Service Swagger:** http://localhost:5001/swagger
- **Product Service Swagger:** http://localhost:5002/swagger
- **Order Service Swagger:** http://localhost:5003/swagger

---

## 📡 API Endpoints

Tất cả APIs đều truy cập qua **API Gateway** (http://localhost:5000):

### Users
- `GET /api/users` - Danh sách users
- `GET /api/users/{id}` - Chi tiết user
- `POST /api/users` - Tạo user mới
- `PUT /api/users/{id}` - Cập nhật user
- `DELETE /api/users/{id}` - Xóa user

### Products
- `GET /api/products` - Danh sách products
- `GET /api/products/{id}` - Chi tiết product
- `GET /api/products/category/{category}` - Lọc theo category
- `POST /api/products` - Tạo product mới
- `PUT /api/products/{id}` - Cập nhật product
- `PATCH /api/products/{id}/stock` - Cập nhật stock
- `DELETE /api/products/{id}` - Xóa product

### Orders
- `GET /api/orders` - Danh sách orders
- `GET /api/orders/{id}` - Chi tiết order
- `GET /api/orders/user/{userId}` - Orders của user
- `POST /api/orders` - Tạo order mới
- `PUT /api/orders/{id}/status` - Cập nhật status
- `DELETE /api/orders/{id}` - Xóa order

---

## 🗄️ Database Configuration

### PostgreSQL
- **Server:** 47.130.33.106:5432
- **Username:** postgres
- **Password:** 123456
- **Databases:**
  - `userservice_db`
  - `productservice_db`
  - `orderservice_db`

### MongoDB
- **Connection:** MongoDB Atlas
- **Databases:**
  - `microservice_users` (User Service)
  - `microservice_products` (Product Service)
  - `microservice_orders` (Order Service)

### RabbitMQ
- **Server:** 47.130.33.106:5672
- **Username:** guest
- **Password:** guest

---

## 📋 Yêu Cầu

- .NET 8.0 SDK
- Node.js 18+ (cho Frontend)
- PostgreSQL (external server)
- MongoDB Atlas (external)
- RabbitMQ (external server)

---

## 📖 Xem Thêm

- **Hướng dẫn chi tiết:** [HUONG_DAN_CHAY_DU_AN.md](./HUONG_DAN_CHAY_DU_AN.md)
- **Kịch bản demo:** [KICH_BAN_DEMO.md](./KICH_BAN_DEMO.md)
- **Kiến trúc:** [ARCHITECTURE.md](./ARCHITECTURE.md)

---

## 📄 License

Dự án được tạo cho mục đích học tập và nghiên cứu.
