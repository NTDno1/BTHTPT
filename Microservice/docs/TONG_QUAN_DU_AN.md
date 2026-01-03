# 📊 Tổng Quan Dự Án Microservice

## 🎯 Mục Đích

Dự án triển khai hệ thống **E-Commerce Backend** theo kiến trúc **Microservice**, minh họa các nguyên tắc từ giáo trình "Các Hệ Thống Phân Tán".

> **💡 Lưu ý:** MongoDB và RabbitMQ là **infrastructure services** được các microservices sử dụng trực tiếp. Xem [GIAI_THICH_KIEN_TRUC.md](./GIAI_THICH_KIEN_TRUC.md) để hiểu rõ hơn.

---

## 🏗️ Kiến Trúc Tổng Thể

```
┌─────────────────────────────────────────┐
│         FRONTEND (Angular 17)           │
│         http://localhost:4200           │
└──────────────────┬──────────────────────┘
                    │ HTTP Requests
                    ▼
┌─────────────────────────────────────────┐
│    API GATEWAY RABBITMQ (PRIMARY)       │
│         http://localhost:5010           │
│  - Điều hướng qua RabbitMQ             │
│  - Load balancing tự động              │
│  - Single entry point                  │
└──────┬──────────┬──────────┬────────────┘
       │          │          │
       │ RabbitMQ │ RabbitMQ │ RabbitMQ
       ▼          ▼          ▼
┌──────────┐ ┌──────────┐ ┌──────────┐
│  USER    │ │ PRODUCT  │ │  ORDER   │
│ SERVICE  │ │ SERVICE  │ │ SERVICE  │
│:5001,5004│ │:5002,5006│ │:5003,5007│
│(2 inst.) │ │(2 inst.) │ │(2 inst.) │
└────┬─────┘ └────┬─────┘ └────┬─────┘
     │            │            │
     ▼            ▼            ▼
┌──────────┐ ┌──────────┐ ┌──────────┐
│PostgreSQL│ │PostgreSQL│ │PostgreSQL│
│userservice│ │product   │ │orderservice│
│   _db    │ │service_db│ │   _db    │
│(47.130.  │ │(47.130.  │ │(47.130.  │
│ 33.106)  │ │ 33.106)  │ │ 33.106)  │
└──────────┘ └──────────┘ └──────────┘
     │            │            │
     └────────────┴────────────┘
                   │
     ┌─────────────┴─────────────┐
     │                           │
     ▼                           ▼
┌──────────────┐        ┌──────────────┐
│ MongoDB Atlas│        │   RabbitMQ   │
│ (Logging)    │        │(47.130.33.106│
│              │        │   :5672)     │
└──────────────┘        └──────────────┘
```

---

## 🎨 Các Tính Năng

### 1. 👥 User Service (Ports 5001, 5004 - Load Balanced)

**Chức năng:**
- ✅ Đăng nhập/Đăng ký với JWT Authentication
- ✅ Xem danh sách users
- ✅ Xem chi tiết user
- ✅ Cập nhật thông tin
- ✅ Xóa user (soft delete)
- ✅ Quản lý địa chỉ giao hàng (UserAddresses)

**API:** 
- Authentication: `POST /api/auth/login`, `POST /api/auth/register`
- Users: `GET|POST|PUT|DELETE /api/users`
- Addresses: `GET|POST|PUT|DELETE /api/users/{userId}/addresses`

**Database:** `userservice_db` (PostgreSQL tại 47.130.33.106)

---

### 2. 📦 Product Service (Ports 5002, 5006 - Load Balanced)

**Chức năng:**
- ✅ Xem danh sách sản phẩm
- ✅ Tìm kiếm theo category
- ✅ Thêm/sửa/xóa sản phẩm
- ✅ Quản lý tồn kho
- ✅ Quản lý giảm giá (discount pricing)
- ✅ Product tags
- ✅ Product reviews (rating, comment, verified purchase)

**API:** 
- Products: `GET|POST|PUT|DELETE /api/products`
- Category: `GET /api/products/category/{category}`
- Stock: `PATCH /api/products/{id}/stock`

**Database:** `productservice_db` (PostgreSQL tại 47.130.33.106)

---

### 3. 🛒 Order Service (Ports 5003, 5007 - Load Balanced)

**Chức năng:**
- ✅ Tạo đơn hàng mới
- ✅ Xem danh sách đơn hàng
- ✅ Xem đơn hàng theo user
- ✅ Cập nhật trạng thái
- ✅ Quản lý OrderItems (chi tiết sản phẩm)
- ✅ OrderStatusHistory (lịch sử trạng thái)
- ✅ Payment information (method, status, transaction ID)
- ✅ Shipping information (carrier, tracking number, dates)
- ✅ Tích hợp RabbitMQ

**API:** 
- Orders: `GET|POST|PUT|DELETE /api/orders`
- User Orders: `GET /api/orders/user/{userId}`
- Status: `PUT /api/orders/{id}/status`

**Database:** `orderservice_db` (PostgreSQL tại 47.130.33.106)

**RabbitMQ:** 
- Server: 47.130.33.106:5672
- Nhận requests từ API Gateway
- Publish events cho các services khác

---

### 4. 🚪 API Gateway RabbitMQ (Port 5010) - PRIMARY GATEWAY

**Chức năng:**
- ✅ Single entry point cho tất cả requests
- ✅ Route requests qua RabbitMQ
- ✅ Load balancing tự động (round-robin)
- ✅ Swagger documentation
- ✅ Route mapping: `/api/users/*`, `/api/products/*`, `/api/orders/*`, `/api/auth/*`

**Lưu ý:** API Gateway Ocelot (port 5000) đã bị disable, chỉ sử dụng RabbitMQ Gateway.

---

## 🛠️ Công Nghệ

| Component | Technology |
|-----------|-----------|
| Backend Framework | .NET 8.0 |
| ORM | Entity Framework Core |
| Database | PostgreSQL (External: 47.130.33.106:5432) |
| Logging | MongoDB Atlas |
| Message Queue | RabbitMQ (External: 47.130.33.106:5672) |
| API Gateway | RabbitMQ Gateway (Custom) |
| Authentication | JWT (JSON Web Tokens) |
| Frontend | Angular 17+ |
| UI Library | Angular Material |
| Containerization | Docker & Docker Compose |

---

## 📈 Luồng Hoạt Động

### Luồng Client Request:
```
Frontend → API Gateway RabbitMQ (5010)
           ↓ (RabbitMQ message)
           Microservice Instance (Load Balanced)
           ↓
           PostgreSQL (47.130.33.106:5432)
           ↓
           MongoDB Atlas (log)
```

### Luồng Event-Driven:
```
Order Service → RabbitMQ (47.130.33.106:5672)
                ↓
        [Other Services subscribe]
                ↓
        MongoDB Atlas (log event)
```

### Load Balancing:
```
API Gateway → RabbitMQ Queue
              ↓ (round-robin)
        Service Instance 1 (port 5001/5002/5003)
        Service Instance 2 (port 5004/5006/5007)
```

---

## 🎯 Điểm Nổi Bật

1. ✅ **Microservice Architecture** - Mỗi service độc lập
2. ✅ **Database Per Service** - Mỗi service có database riêng (PostgreSQL)
3. ✅ **API Gateway RabbitMQ** - Single entry point với load balancing tự động
4. ✅ **Load Balancing** - 2 instances mỗi service, load balancing qua RabbitMQ
5. ✅ **Event-Driven** - RabbitMQ cho async communication và request routing
6. ✅ **JWT Authentication** - Secure authentication với tokens
7. ✅ **Swagger UI** - Tất cả services có documentation
8. ✅ **Docker Compose** - Dễ dàng deploy và scale

---

## 📚 Tài Liệu

- [README.md](./README.md) - Tổng quan
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Kiến trúc chi tiết
- [HUONG_DAN_CHAY_DU_AN.md](./HUONG_DAN_CHAY_DU_AN.md) - Hướng dẫn chạy
- [KICH_BAN_DEMO.md](./KICH_BAN_DEMO.md) - Kịch bản demo
