# 🏗️ Kiến Trúc Microservice - Tài Liệu Chi Tiết

## 📐 Tổng Quan

Dự án triển khai kiến trúc Microservice dựa trên giáo trình "Các Hệ Thống Phân Tán" và best practices thực tế.

---

## 🏛️ Các Thành Phần

### 1. API Gateway (Ocelot)

**Vai trò:** Entry point cho tất cả client requests

**Port:** 5000

**Chức năng:**
- Điều hướng requests đến microservices
- Load balancing
- CORS configuration
- Swagger documentation

**Swagger:** http://localhost:5000/swagger

---

### 2. User Service

**Domain:** Quản lý người dùng

**Port:** 5001

**Database:** `userservice_db` (PostgreSQL)

**MongoDB:** `microservice_users` / `user_logs`

**API Endpoints:**
- `GET /api/users` - Danh sách users
- `GET /api/users/{id}` - Chi tiết user
- `POST /api/users` - Tạo user mới
- `PUT /api/users/{id}` - Cập nhật user
- `DELETE /api/users/{id}` - Xóa user

**Swagger:** http://localhost:5001/swagger

---

### 3. Product Service

**Domain:** Quản lý sản phẩm

**Port:** 5002

**Database:** `productservice_db` (PostgreSQL)

**MongoDB:** `microservice_products` / `product_logs`

**API Endpoints:**
- `GET /api/products` - Danh sách products
- `GET /api/products/{id}` - Chi tiết product
- `GET /api/products/category/{category}` - Lọc theo category
- `POST /api/products` - Tạo product mới
- `PUT /api/products/{id}` - Cập nhật product
- `PATCH /api/products/{id}/stock` - Cập nhật stock
- `DELETE /api/products/{id}` - Xóa product

**Swagger:** http://localhost:5002/swagger

---

### 4. Order Service

**Domain:** Quản lý đơn hàng

**Port:** 5003

**Database:** `orderservice_db` (PostgreSQL)

**MongoDB:** `microservice_orders` / `order_events`

**RabbitMQ:** 
- Server: 47.130.33.106:5672
- Queues: `order.created`, `order.status.updated`

**API Endpoints:**
- `GET /api/orders` - Danh sách orders
- `GET /api/orders/{id}` - Chi tiết order
- `GET /api/orders/user/{userId}` - Orders của user
- `POST /api/orders` - Tạo order mới
- `PUT /api/orders/{id}/status` - Cập nhật status
- `DELETE /api/orders/{id}` - Xóa order

**Swagger:** http://localhost:5003/swagger

---

## 🔄 Luồng Giao Tiếp

### Synchronous (HTTP/REST)

```
Client → API Gateway → Microservice → PostgreSQL
```

**Lưu ý:** Tất cả client requests đều đi qua API Gateway.

### Asynchronous (RabbitMQ)

```
Order Service → RabbitMQ (trực tiếp)
                ↓
        [Other Services subscribe]
```

**Lưu ý:** RabbitMQ được sử dụng trực tiếp, không qua Gateway.

### Infrastructure Services

```
Tất cả Services → MongoDB (trực tiếp)
                  - Logging
                  - Events storage

Order Service → RabbitMQ (trực tiếp)
                  - Event publishing
```

**Lưu ý:** MongoDB và RabbitMQ là infrastructure services.

---

## 🗄️ Database Design

### Database Per Service Pattern

Mỗi service có database riêng:

| Service | Database | Type |
|---------|----------|------|
| User Service | `userservice_db` | PostgreSQL |
| Product Service | `productservice_db` | PostgreSQL |
| Order Service | `orderservice_db` | PostgreSQL |

### Schema

**userservice_db:**
- `Users` - Thông tin người dùng

**productservice_db:**
- `Products` - Thông tin sản phẩm

**orderservice_db:**
- `Orders` - Thông tin đơn hàng
- `OrderItems` - Chi tiết items

---

## 📦 Shared Libraries

**Microservice.Common:**
- `BaseEntity` - Base class cho entities
- `MessageEvent` - Model cho events
- `IMessagePublisher` - Interface cho publishing
- `IMessageConsumer` - Interface cho consuming

---

## 🔐 Security

**Hiện tại:**
- ✅ Password hashing (BCrypt)
- ✅ CORS configuration

**Có thể mở rộng:**
- ⏳ JWT Authentication
- ⏳ Role-based Authorization
- ⏳ Rate Limiting

---

## 📈 Scalability

- Mỗi service có thể scale độc lập
- Load balancing qua API Gateway
- Stateless services

---

## 📚 Nguyên Tắc Thiết Kế

1. **Tính độc lập** - Mỗi service độc lập
2. **Gắn kết lỏng** - Giao tiếp qua API và message queue
3. **Tính mô đun** - Mỗi service tập trung một domain
4. **Tính trong suốt** - API Gateway che giấu phức tạp
5. **Khả năng mở rộng** - Dễ scale từng service
6. **Tính chịu lỗi** - Fault isolation

---

## 🔮 Có Thể Mở Rộng

- Service Discovery (Consul)
- Configuration Server
- Circuit Breaker (Polly)
- Distributed Tracing
- API Versioning
- Caching (Redis)
- Kafka (high-throughput)
