# 📐 Sơ Đồ Kiến Trúc - Copy vào Slides

## 🏗️ Sơ Đồ Tổng Quan Hệ Thống

```
┌─────────────────────────────────────────┐
│         FRONTEND (Angular)              │
│         http://localhost:4200           │
└──────────────────┬──────────────────────┘
                    │ HTTP Requests
                    ▼
┌─────────────────────────────────────────┐
│      API GATEWAY RABBITMQ              │
│      http://localhost:5010              │
│  - Điều hướng qua RabbitMQ             │
│  - Single entry point                  │
└──────────────────┬──────────────────────┘
                    │
                    ▼
            ┌──────────────┐
            │   RabbitMQ   │
            │   Queues:    │
            │ api.user.    │
            │ api.product. │
            │ api.order.   │
            └──────┬───────┘
                   │
        ┌──────────┼──────────┐
        │          │          │
        ▼          ▼          ▼
┌──────────┐ ┌──────────┐ ┌──────────┐
│  USER    │ │ PRODUCT  │ │  ORDER   │
│ SERVICE  │ │ SERVICE  │ │ SERVICE  │
│  :5001   │ │  :5002   │ │  :5003   │
│Consumer  │ │Consumer  │ │Consumer  │
└────┬─────┘ └────┬─────┘ └────┬─────┘
     │            │            │
     ▼            ▼            ▼
┌──────────┐ ┌──────────┐ ┌──────────┐
│PostgreSQL│ │PostgreSQL│ │PostgreSQL│
│userservice│ │product   │ │orderservice│
│   _db    │ │service_db│ │   _db    │
└──────────┘ └──────────┘ └──────────┘
     │            │            │
     └────────────┴────────────┘
                   │
     ┌─────────────┴─────────────┐
     │                           │
     ▼                           ▼
┌──────────────┐        ┌──────────────┐
│   MongoDB    │        │   RabbitMQ   │
│ (Logging)    │        │ (Events)     │
└──────────────┘        └──────────────┘
```

---

## 🔄 Luồng Giao Tiếp Qua API Gateway RabbitMQ

```
Frontend
   │
   │ HTTP Request
   ▼
API Gateway RabbitMQ
   │
   │ Publish to Queue
   ▼
RabbitMQ Queue (api.user.request)
   │
   │ Consume Message
   ▼
RabbitMQConsumerService (User Service)
   │
   │ Process Request
   ▼
User Service
   │
   │ Response
   ▼
RabbitMQ Queue (api.gateway.response)
   │
   │ Consume Response
   ▼
API Gateway RabbitMQ
   │
   │ HTTP Response
   ▼
Frontend
```

---

## 📨 Luồng Giao Tiếp Asynchronous (RabbitMQ)

```
Frontend
   │
   │ HTTP Request
   ▼
API Gateway RabbitMQ
   │
   │ Publish to Queue
   ▼
RabbitMQ Queue (api.user.request)
   │
   │ Consume Message
   ▼
RabbitMQConsumerService
   │
   │ Call Business Logic
   ▼
User Service
   │
   │ Process & Response
   ▼
RabbitMQ Queue (api.gateway.response)
   │
   │ Consume Response
   ▼
API Gateway RabbitMQ
   │
   │ HTTP Response
   ▼
Frontend
```

---

## 🛒 Luồng Tạo Đơn Hàng (Chi Tiết)

```
1. Frontend → API Gateway RabbitMQ
   POST /api/orders
   
2. API Gateway RabbitMQ → RabbitMQ Queue
   Publish to: api.order.request
   
3. RabbitMQConsumerService (Order Service)
   Consume message from queue
   
4. Order Service → Product Service (HTTP)
   GET /api/products/{id}
   (Lấy thông tin: giá, tồn kho)
   
5. Order Service
   - Kiểm tra tồn kho
   - Tính tổng tiền
   - Tạo Order trong database
   
6. Order Service → Product Service (HTTP)
   PATCH /api/products/{id}/stock
   (Trừ tồn kho)
   
7. Order Service → RabbitMQ
   Publish event: "order.created"
   
8. Order Service → RabbitMQ Queue
   Publish response to: api.gateway.response
   
9. API Gateway RabbitMQ
   Consume response from queue
   
10. API Gateway RabbitMQ → Frontend
    HTTP Response (Order created)
```

---

## 🏛️ Kiến Trúc Microservice vs Monolithic

### Monolithic Architecture
```
┌─────────────────────────────┐
│     Monolithic Application  │
│  ┌──────┬──────┬──────────┐ │
│  │ User │Product│ Order   │ │
│  └──────┴──────┴──────────┘ │
│         │                   │
│         ▼                   │
│    ┌─────────┐              │
│    │Database │              │
│    └─────────┘              │
└─────────────────────────────┘
```

### Microservice Architecture
```
┌──────┐ ┌────────┐ ┌───────┐
│ User │ │Product │ │ Order │
│Service│ │Service │ │Service│
└───┬───┘ └───┬────┘ └───┬───┘
    │         │          │
    ▼         ▼          ▼
┌──────┐ ┌────────┐ ┌───────┐
│  DB  │ │  DB    │ │  DB   │
└──────┘ └────────┘ └───────┘
```

---

## 📊 API Gateway RabbitMQ - Kiến Trúc

### API Gateway RabbitMQ (Asynchronous - Đang sử dụng)
```
Frontend → API Gateway RabbitMQ → RabbitMQ Queue → Consumer Service → Service → Queue → Response
         (Message queue, async processing, load balancing tự nhiên)
```

**Đặc điểm:**
- ✅ Bất đồng bộ (Asynchronous)
- ✅ Load balancing tự nhiên với RabbitMQ
- ✅ Decoupling giữa Frontend và Services
- ✅ Mỗi service có RabbitMQConsumerService riêng
- ✅ Dễ scale bằng cách tăng số instances

---

## ⚖️ Load Balancing với RabbitMQ

### Sơ Đồ Load Balancing

```
API Gateway RabbitMQ
         │
         │ Publish Messages
         ▼
┌─────────────────────┐
│  RabbitMQ Queue     │
│  api.user.request   │
└──────────┬──────────┘
           │
    ┌──────┴──────┐
    │             │
    ▼             ▼
┌─────────┐  ┌─────────┐
│ User    │  │ User    │
│ Service │  │ Service │
│ (v1)    │  │ (v2)    │
│ :5001   │  │ :5011   │
└─────────┘  └─────────┘

RabbitMQ tự động phân phối messages:
- Message 1 → User Service v1
- Message 2 → User Service v2
- Message 3 → User Service v1
- Message 4 → User Service v2
(Round-robin)
```

### Cách Hoạt Động

```
1. Frontend gửi 10 requests
   ↓
2. API Gateway RabbitMQ publish vào queue
   ↓
3. RabbitMQ phân phối:
   - Request 1, 3, 5, 7, 9 → user-service (v1)
   - Request 2, 4, 6, 8, 10 → user-service-v2
   ↓
4. Cả 2 instances xử lý song song
   ↓
5. Tăng throughput gấp đôi!
```

### Lợi Ích

- ✅ **Tự động:** Không cần cấu hình load balancer
- ✅ **Fault Tolerance:** Một instance lỗi, instance khác vẫn hoạt động
- ✅ **Dễ Scale:** Chỉ cần chạy thêm container
- ✅ **Cân bằng tải:** RabbitMQ đảm bảo phân phối đều

---

## 🔐 Database Per Service Pattern

```
User Service          Product Service        Order Service
     │                     │                      │
     ▼                     ▼                      ▼
┌──────────┐         ┌──────────┐          ┌──────────┐
│userservice│         │product   │          │orderservice│
│   _db    │         │service_db│          │   _db    │
└──────────┘         └──────────┘          └──────────┘

Mỗi service có database riêng:
✅ Độc lập về dữ liệu
✅ Có thể chọn công nghệ DB khác nhau
✅ Tránh conflict khi scale
```

---

## 📦 Docker Architecture

```
┌─────────────────────────────────────┐
│      Docker Compose Network         │
│      (microservice-network)         │
│                                     │
│  ┌──────────┐  ┌──────────┐         │
│  │  User    │  │ Product │         │
│  │ Container│  │Container│         │
│  └──────────┘  └──────────┘         │
│                                     │
│  ┌──────────┐  ┌──────────┐         │
│  │  Order   │  │   API    │         │
│  │ Container│  │ Gateway  │         │
│  └──────────┘  └──────────┘         │
│                                     │
└─────────────────────────────────────┘
```

---

## 🎯 Event-Driven Architecture

```
Order Service
     │
     │ Publish Event
     ▼
┌─────────────────┐
│  RabbitMQ       │
│  Queue:         │
│  order.created  │
└────────┬────────┘
         │
    ┌────┴────┬──────────┐
    │        │          │
    ▼        ▼          ▼
┌────────┐ ┌────────┐ ┌────────┐
│Email   │ │Payment │ │Analytics│
│Service │ │Service │ │Service │
└────────┘ └────────┘ └────────┘

Các service subscribe và xử lý event độc lập
```

---

## 💡 Tips Sử Dụng Sơ Đồ

1. **Copy vào PowerPoint:**
   - Chọn text → Format → Font: Consolas hoặc Courier New
   - Size: 10-12pt
   - Background: Trắng hoặc xám nhạt

2. **Vẽ lại bằng công cụ:**
   - Sử dụng Draw.io, Lucidchart, hoặc PowerPoint shapes
   - Thêm màu sắc để dễ phân biệt
   - Thêm mũi tên chỉ hướng

3. **Trình bày:**
   - Giải thích từng thành phần
   - Chỉ vào sơ đồ khi nói
   - Highlight phần đang nói đến

