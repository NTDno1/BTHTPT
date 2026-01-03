# 🎬 Kịch Bản Demo Dự Án Microservice

Kịch bản demo chi tiết để trình bày dự án Microservice (25 phút).

---

## 📋 Chuẩn Bị Trước Khi Demo

### ✅ Checklist

- [ ] Tất cả backend services đang chạy
  - API Gateway RabbitMQ (PRIMARY): http://localhost:5010
  - User Service Instance 1: http://localhost:5001
  - User Service Instance 2: http://localhost:5004
  - Product Service Instance 1: http://localhost:5002
  - Product Service Instance 2: http://localhost:5006
  - Order Service Instance 1: http://localhost:5003
  - Order Service Instance 2: http://localhost:5007

- [ ] Frontend đang chạy: http://localhost:4200

- [ ] Databases đã được tạo và có dữ liệu mẫu

- [ ] Mở các tab trình duyệt:
  1. Frontend Angular - http://localhost:4200
  2. API Gateway RabbitMQ Swagger - http://localhost:5010/swagger
  3. User Service Instance 1 Swagger - http://localhost:5001/swagger
  4. User Service Instance 2 Swagger - http://localhost:5004/swagger
  5. Product Service Instance 1 Swagger - http://localhost:5002/swagger
  6. Product Service Instance 2 Swagger - http://localhost:5006/swagger
  7. Order Service Instance 1 Swagger - http://localhost:5003/swagger
  8. Order Service Instance 2 Swagger - http://localhost:5007/swagger

---

## 🎯 Phần 1: Giới Thiệu Tổng Quan (3 phút)

### Lời Nói:

> "Hôm nay tôi sẽ trình bày về dự án E-Commerce Backend được xây dựng theo kiến trúc Microservice sử dụng .NET 8.0.
> 
> Hệ thống bao gồm:
> - User Service: Quản lý người dùng (2 instances - Load Balanced)
> - Product Service: Quản lý sản phẩm (2 instances - Load Balanced)
> - Order Service: Quản lý đơn hàng (2 instances - Load Balanced)
> - API Gateway RabbitMQ: Điều hướng requests qua RabbitMQ với load balancing tự động
> 
> Mỗi service có database riêng và có thể deploy độc lập. Load balancing được thực hiện tự động qua RabbitMQ."

**Hành động:**
- Mở Frontend: http://localhost:4200
- Giới thiệu giao diện

---

## 🏗️ Phần 2: Kiến Trúc Hệ Thống (5 phút)

### Lời Nói:

> "Đây là kiến trúc tổng thể của hệ thống:
> 
> - Frontend Angular gửi requests đến API Gateway RabbitMQ (port 5010)
> - API Gateway RabbitMQ gửi requests qua RabbitMQ queues
> - RabbitMQ tự động phân phối requests đến các service instances (load balancing)
> - Mỗi service có PostgreSQL database riêng
> - MongoDB được dùng cho logging
> - RabbitMQ được dùng cho cả request routing và message queue"

**Hành động:**
- Mở các Swagger UI để show endpoints
- Giải thích sơ đồ kiến trúc

---

## 👥 Phần 3: Demo User Service (4 phút)

### Lời Nói:

> "Bây giờ tôi sẽ demo User Service. Service này quản lý thông tin người dùng."

**Hành động:**

1. **Mở User Service Swagger:** http://localhost:5001/swagger hoặc http://localhost:5004/swagger
   - Giải thích các endpoints
   - GET /api/users - Lấy danh sách users
   - POST /api/auth/login - Đăng nhập với JWT
   - POST /api/auth/register - Đăng ký

2. **Đăng ký User mới:**
   - POST /api/auth/register (qua API Gateway: http://localhost:5010/api/auth/register)
   - Body: `{"username":"demo","email":"demo@example.com","password":"123456","firstName":"Demo","lastName":"User"}`

3. **Đăng nhập:**
   - POST /api/auth/login (qua API Gateway: http://localhost:5010/api/auth/login)
   - Body: `{"username":"demo","password":"123456"}`
   - Nhận JWT token

4. **Xem danh sách users:**
   - GET /api/users (qua API Gateway: http://localhost:5010/api/users)

5. **Qua Frontend:**
   - Mở tab Users
   - Show danh sách users

---

## 📦 Phần 4: Demo Product Service (4 phút)

### Lời Nói:

> "Product Service quản lý sản phẩm và tồn kho."

**Hành động:**

1. **Mở Product Service Swagger:** http://localhost:5002/swagger
   - Giải thích endpoints

2. **Tạo Product:**
   - POST /api/products
   - Body: `{"name":"Laptop Dell","description":"High performance laptop","price":20000000,"stock":5,"category":"Electronics"}`

3. **Lọc theo category:**
   - GET /api/products/category/Electronics

4. **Qua Frontend:**
   - Mở tab Products
   - Show danh sách và filter

---

## 🛒 Phần 5: Demo Order Service với RabbitMQ (6 phút)

### Lời Nói:

> "Order Service là service phức tạp nhất, tích hợp với RabbitMQ để publish events."

**Hành động:**

1. **Mở Order Service Swagger:** http://localhost:5003/swagger
   - Giải thích về RabbitMQ integration

2. **Tạo Order:**
   - POST /api/orders
   - Body: `{"userId":1,"shippingAddress":"123 Main St","orderItems":[{"productId":1,"quantity":2}]}`
   - **Giải thích:** Order được tạo và event được publish vào RabbitMQ

3. **Xem Orders:**
   - GET /api/orders
   - GET /api/orders/user/1

4. **Cập nhật Status:**
   - PUT /api/orders/1/status
   - Body: `{"status":"Processing"}`
   - **Giải thích:** Status update cũng publish event

5. **Qua Frontend:**
   - Mở tab Orders
   - Show danh sách và update status

---

## 🚪 Phần 6: Demo API Gateway (3 phút)

### Lời Nói:

> "API Gateway là single entry point cho tất cả requests. Client chỉ cần biết một URL duy nhất."

**Hành động:**

1. **Mở API Gateway Swagger:** http://localhost:5000/swagger
   - Giải thích về routing

2. **Test qua API Gateway:**
   - GET http://localhost:5000/api/users
   - GET http://localhost:5000/api/products
   - GET http://localhost:5000/api/orders

3. **So sánh:**
   - Show rằng cùng một request có thể gọi qua Gateway hoặc trực tiếp service
   - Giải thích lợi ích của API Gateway

---

## 📊 Tổng Kết (2 phút)

### Lời Nói:

> "Tóm lại, dự án này minh họa:
> 
> 1. ✅ Kiến trúc Microservice với database per service
> 2. ✅ API Gateway pattern
> 3. ✅ Event-driven architecture với RabbitMQ
> 4. ✅ Swagger documentation cho tất cả services
> 5. ✅ Frontend Angular tích hợp với backend
> 
> Hệ thống có thể scale độc lập từng service và dễ dàng mở rộng."

**Hành động:**
- Tổng kết lại các điểm chính
- Mở Q&A

---

## ❓ Câu Hỏi Thường Gặp

### Q: Tại sao mỗi service có database riêng?
**A:** Đảm bảo tính độc lập, có thể deploy và scale độc lập, tránh tight coupling.

### Q: RabbitMQ được dùng để làm gì?
**A:** Cho event-driven communication, Order Service publish events khi có thay đổi.

### Q: API Gateway có vai trò gì?
**A:** Single entry point, che giấu sự phức tạp, dễ dàng thêm authentication, rate limiting.

### Q: MongoDB được dùng để làm gì?
**A:** Logging và events storage, không phải primary database.

---

## 📝 Ghi Chú

- **Thời gian:** 25 phút (có thể điều chỉnh)
- **Điểm nhấn:** RabbitMQ integration, API Gateway, Database per service
- **Demo trực tiếp:** Ưu tiên demo qua Swagger và Frontend

---

## 🔗 Links Tham Khảo

- [README.md](./README.md) - Tổng quan dự án
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Kiến trúc chi tiết
- [HUONG_DAN_CHAY_DU_AN.md](./HUONG_DAN_CHAY_DU_AN.md) - Hướng dẫn chạy
