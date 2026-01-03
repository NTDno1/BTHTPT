# 🏗️ Kiến Trúc Microservice - Tài Liệu Chi Tiết

## 📐 Tổng Quan

Dự án triển khai kiến trúc Microservice dựa trên giáo trình "Các Hệ Thống Phân Tán" và best practices thực tế.

---

## 🏛️ Các Thành Phần

### 1. API Gateway RabbitMQ (PRIMARY GATEWAY)

**Vai trò:** Entry point chính cho tất cả client requests, sử dụng RabbitMQ để điều hướng

**Port:** 5010

**Chức năng:**
- Điều hướng requests đến microservices qua RabbitMQ
- Load balancing tự động qua RabbitMQ queues
- CORS configuration
- Swagger documentation
- Route mapping: `/api/users/*` → UserService, `/api/products/*` → ProductService, `/api/orders/*` → OrderService, `/api/auth/*` → UserService

**Swagger:** http://localhost:5010/swagger

**Lưu ý:** API Gateway Ocelot (port 5000) đã bị disable, chỉ sử dụng RabbitMQ Gateway.

---

### 2. User Service

**Domain:** Quản lý người dùng và xác thực

**Ports:** 
- Instance 1: 5001
- Instance 2: 5004 (Load Balanced)

**Database:** `userservice_db` (PostgreSQL)

**MongoDB:** `microservice_users` / `user_logs`

**RabbitMQ:** Consumer service để nhận requests từ API Gateway

**JWT Authentication:** ✅ Đã implement

**API Endpoints:**

**Authentication:**
- `POST /api/auth/login` - Đăng nhập (trả về JWT token)
- `POST /api/auth/register` - Đăng ký tài khoản mới

**User Management:**
- `GET /api/users` - Danh sách users
- `GET /api/users/{id}` - Chi tiết user
- `POST /api/users` - Tạo user mới
- `PUT /api/users/{id}` - Cập nhật user
- `DELETE /api/users/{id}` - Xóa user

**User Addresses:**
- `GET /api/users/{userId}/addresses` - Danh sách địa chỉ của user
- `POST /api/users/{userId}/addresses` - Thêm địa chỉ mới
- `PUT /api/users/{userId}/addresses/{addressId}` - Cập nhật địa chỉ
- `DELETE /api/users/{userId}/addresses/{addressId}` - Xóa địa chỉ

**Swagger:** 
- Instance 1: http://localhost:5001/swagger
- Instance 2: http://localhost:5004/swagger

---

### 3. Product Service

**Domain:** Quản lý sản phẩm

**Ports:**
- Instance 1: 5002
- Instance 2: 5006 (Load Balanced)

**Database:** `productservice_db` (PostgreSQL)

**MongoDB:** `microservice_products` / `product_logs`

**RabbitMQ:** Consumer service để nhận requests từ API Gateway

**API Endpoints:**

**Products:**
- `GET /api/products` - Danh sách products
- `GET /api/products/{id}` - Chi tiết product
- `GET /api/products/category/{category}` - Lọc theo category
- `POST /api/products` - Tạo product mới
- `PUT /api/products/{id}` - Cập nhật product
- `PATCH /api/products/{id}/stock` - Cập nhật stock
- `DELETE /api/products/{id}` - Xóa product

**Product Features:**
- Discount pricing (DiscountPrice, DiscountStartDate, DiscountEndDate)
- Product tags (ProductTags table)
- Product reviews (ProductReviews table với rating, comment, verified purchase)

**Swagger:**
- Instance 1: http://localhost:5002/swagger
- Instance 2: http://localhost:5006/swagger

---

### 4. Order Service

**Domain:** Quản lý đơn hàng

**Ports:**
- Instance 1: 5003
- Instance 2: 5007 (Load Balanced)

**Database:** `orderservice_db` (PostgreSQL)

**MongoDB:** `microservice_orders` / `order_events`

**RabbitMQ:** 
- Server: 47.130.33.106:5672
- Username: guest / Password: guest
- Consumer service để nhận requests từ API Gateway
- Publisher cho order events

**API Endpoints:**

**Orders:**
- `GET /api/orders` - Danh sách orders
- `GET /api/orders/{id}` - Chi tiết order
- `GET /api/orders/user/{userId}` - Orders của user
- `POST /api/orders` - Tạo order mới
- `PUT /api/orders/{id}/status` - Cập nhật status
- `DELETE /api/orders/{id}` - Xóa order

**Order Features:**
- OrderItems (chi tiết sản phẩm trong đơn hàng)
- OrderStatusHistory (lịch sử thay đổi trạng thái)
- Payment information (PaymentMethod, PaymentStatus, PaymentTransactionId, PaymentDate)
- Shipping information (ShippingCarrier, TrackingNumber, ShippedDate, DeliveredDate)
- Notes field

**Swagger:**
- Instance 1: http://localhost:5003/swagger
- Instance 2: http://localhost:5007/swagger

---

## 🔄 Luồng Giao Tiếp

### Synchronous (HTTP/REST qua RabbitMQ Gateway)

```
Client → API Gateway RabbitMQ (port 5010)
         ↓ (RabbitMQ message)
         Microservice Instance (Load Balanced)
         ↓
         PostgreSQL Database
```

**Lưu ý:** 
- Tất cả client requests đều đi qua API Gateway RabbitMQ
- Gateway gửi request qua RabbitMQ queue đến service instances
- Load balancing tự động qua RabbitMQ (round-robin giữa các instances)
- Mỗi service có 2 instances để load balancing

### Asynchronous (RabbitMQ Events)

```
Order Service → RabbitMQ (publish events)
                ↓
        [Other Services subscribe to events]
```

**Lưu ý:** RabbitMQ được sử dụng cho cả synchronous routing (qua Gateway) và asynchronous events.

### Infrastructure Services

```
Tất cả Services → MongoDB Atlas (trực tiếp)
                  - Logging
                  - Events storage

Tất cả Services → RabbitMQ (47.130.33.106:5672)
                  - Request routing (via Gateway)
                  - Event publishing/subscribing
```

**Lưu ý:** 
- MongoDB Atlas được sử dụng cho logging
- RabbitMQ server external tại 47.130.33.106:5672
- PostgreSQL server external tại 47.130.33.106:5432

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
- `Users` - Thông tin người dùng (với Role, AvatarUrl)
- `UserAddresses` - Địa chỉ giao hàng của users

**productservice_db:**
- `Products` - Thông tin sản phẩm (với DiscountPrice, DiscountStartDate, DiscountEndDate)
- `ProductReviews` - Đánh giá sản phẩm (rating, comment, verified purchase)
- `ProductTags` - Tags cho sản phẩm

**orderservice_db:**
- `Orders` - Thông tin đơn hàng (với PaymentMethod, PaymentStatus, ShippingCarrier, TrackingNumber, etc.)
- `OrderItems` - Chi tiết items trong đơn hàng
- `OrderStatusHistory` - Lịch sử thay đổi trạng thái đơn hàng

---

## 📦 Shared Libraries

**Microservice.Common:**
- `BaseEntity` - Base class cho entities
- `MessageEvent` - Model cho events
- `IMessagePublisher` - Interface cho publishing
- `IMessageConsumer` - Interface cho consuming

---

## 🔐 Security

**Đã implement:**
- ✅ Password hashing (BCrypt)
- ✅ JWT Authentication (JWT tokens với expiration)
- ✅ Refresh tokens
- ✅ CORS configuration
- ✅ Role-based user system (Admin, Customer)

**Có thể mở rộng:**
- ⏳ Role-based Authorization middleware
- ⏳ Rate Limiting
- ⏳ API Key authentication

---

## 📈 Scalability

- Mỗi service có thể scale độc lập
- Load balancing tự động qua RabbitMQ (2 instances mỗi service)
- Stateless services
- Horizontal scaling: Có thể thêm nhiều instances hơn bằng cách tăng số lượng containers trong docker-compose.yml

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
