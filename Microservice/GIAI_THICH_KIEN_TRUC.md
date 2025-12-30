# 🏗️ Giải Thích Kiến Trúc - MongoDB và RabbitMQ

## ❓ Câu Hỏi: Tại Sao MongoDB và RabbitMQ Không Đứng Trước API Gateway?

### ✅ Trả Lời:

**MongoDB và RabbitMQ KHÔNG đứng trước hay sau API Gateway** - chúng là các **Infrastructure Services** (dịch vụ hạ tầng) được các microservices sử dụng trực tiếp.

---

## 🎯 Vai Trò Của Từng Thành Phần

### 1. API Gateway (Ocelot)

**Vai trò:** Entry point cho **client requests**

**Sử dụng bởi:** Frontend, Mobile App, External clients

**Chức năng:** Route HTTP requests đến đúng microservice

**Port:** 5000

---

### 2. Microservices (User, Product, Order)

**Vai trò:** Xử lý business logic

**Sử dụng bởi:** API Gateway (cho client requests)

**Sử dụng:** PostgreSQL, MongoDB, RabbitMQ (trực tiếp)

**Ports:** 5001, 5002, 5003

---

### 3. PostgreSQL

**Vai trò:** Primary database cho mỗi service

**Sử dụng bởi:** Các microservices (trực tiếp)

**Không qua:** API Gateway

**Databases:**
- `userservice_db`
- `productservice_db`
- `orderservice_db`

---

### 4. MongoDB

**Vai trò:** Logging và events storage

**Sử dụng bởi:** Tất cả microservices (trực tiếp)

**Không qua:** API Gateway

**Lý do:** Đây là internal service, không phải API endpoint

**Databases:**
- `microservice_users`
- `microservice_products`
- `microservice_orders`

---

### 5. RabbitMQ

**Vai trò:** Message queue cho async communication

**Sử dụng bởi:** Order Service và các services khác (trực tiếp)

**Không qua:** API Gateway

**Lý do:** Đây là internal messaging, không phải HTTP API

**Server:** 47.130.33.106:5672

**Queues:**
- `order.created`
- `order.status.updated`

---

## 📊 Sơ Đồ Luồng Dữ Liệu

### Luồng Client Request (HTTP):
```
Frontend → API Gateway → Microservice → PostgreSQL
                              ↓
                          MongoDB (logging)
```

### Luồng Internal Communication (Message Queue):
```
Order Service → RabbitMQ → [Other Services subscribe]
     ↓
MongoDB (log event)
```

---

## 🔑 Điểm Quan Trọng

### 1. API Gateway chỉ xử lý HTTP requests từ client
- ❌ Không xử lý database connections
- ❌ Không xử lý message queue
- ✅ Chỉ route HTTP requests

### 2. MongoDB và RabbitMQ là internal services
- ❌ Client không truy cập trực tiếp
- ✅ Chỉ các microservices sử dụng
- ✅ Không cần đi qua API Gateway

### 3. Kiến trúc đúng:
```
Client → API Gateway → Microservices
                              ↓
                    ┌─────────┴─────────┐
                    │                    │
              PostgreSQL          MongoDB/RabbitMQ
              (Database)          (Infrastructure)
```

---

## 🎨 Sơ Đồ Đúng

```
Frontend
    ↓ HTTP
API Gateway (Entry Point cho Client)
    ↓
Microservices (User, Product, Order)
    ↓
┌───────────┬───────────┬───────────┐
│           │           │           │
PostgreSQL  MongoDB   RabbitMQ
(Database)  (Logging)  (Messages)
```

---

## 📝 Kết Luận

**MongoDB và RabbitMQ đứng song song với các microservices**, không phải trước hay sau API Gateway. Chúng là **infrastructure layer** mà các services sử dụng.

**Tóm tắt:**
- ✅ API Gateway: Cho client requests (HTTP)
- ✅ PostgreSQL: Primary database (trực tiếp từ services)
- ✅ MongoDB: Logging/Events (trực tiếp từ services)
- ✅ RabbitMQ: Message Queue (trực tiếp từ services)

---

## 🔗 Xem Thêm

- [ARCHITECTURE.md](./ARCHITECTURE.md) - Kiến trúc chi tiết
- [README.md](./README.md) - Tổng quan dự án
