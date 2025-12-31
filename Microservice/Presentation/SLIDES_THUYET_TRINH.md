# 🎤 Slides Thuyết Trình - Dự Án Microservice
## Thời gian: 10 phút | Số slides: 13

---

## 📊 SLIDE 1: Giới Thiệu Dự Án

### Tiêu đề: **Dự Án E-Commerce Backend - Kiến Trúc Microservice**

**Nội dung:**
- **Nhóm:** [Tên nhóm của bạn]
- **Mục tiêu:** Xây dựng hệ thống E-Commerce theo kiến trúc Microservice
- **Vấn đề cần giải quyết:**
  - Tách biệt các domain (User, Product, Order)
  - Mỗi service có thể scale độc lập
  - Dễ bảo trì và phát triển
- **Công nghệ:** .NET 8.0, Angular, PostgreSQL, MongoDB, RabbitMQ

**Gợi ý hình ảnh:** Logo dự án hoặc sơ đồ tổng quan

---

## 🏗️ SLIDE 2: Kiến Trúc Tổng Quan

### Tiêu đề: **Kiến Trúc Hệ Thống Microservice**

**Nội dung:**
- **Frontend:** Angular (Port 4200) - Giao diện người dùng
- **API Gateway RabbitMQ:** (Port 5010) - Điều hướng requests qua message queue
- **Microservices:**
  - User Service (Port 5001)
  - Product Service (Port 5002)
  - Order Service (Port 5003)
- **Databases:** Mỗi service có PostgreSQL riêng
- **Infrastructure:** MongoDB (logging), RabbitMQ (messaging & API Gateway)

**Gợi ý sơ đồ:**
```
Frontend → API Gateway RabbitMQ → RabbitMQ Queue → [User/Product/Order Services] → PostgreSQL
                                                              ↓
                                                      MongoDB + RabbitMQ
```

---

## 👥 SLIDE 3: User Service

### Tiêu đề: **User Service - Quản Lý Người Dùng**

**Nội dung:**
- **Chức năng:**
  - Đăng ký tài khoản mới
  - Xem danh sách và chi tiết user
  - Cập nhật thông tin người dùng
  - Xóa user (soft delete)
- **Database:** PostgreSQL (`userservice_db`)
- **Logging:** MongoDB (`microservice_users`)
- **API Endpoints:** `GET|POST|PUT|DELETE /api/users`
- **Port:** 5001

**Gợi ý demo:** Swagger UI hoặc Frontend

---

## 📦 SLIDE 4: Product Service

### Tiêu đề: **Product Service - Quản Lý Sản Phẩm**

**Nội dung:**
- **Chức năng:**
  - Quản lý danh mục sản phẩm
  - Tìm kiếm theo category
  - Thêm/sửa/xóa sản phẩm
  - Quản lý tồn kho (stock)
- **Database:** PostgreSQL (`productservice_db`)
- **Logging:** MongoDB (`microservice_products`)
- **API Endpoints:** `GET|POST|PUT|PATCH|DELETE /api/products`
- **Port:** 5002

**Gợi ý demo:** Show danh sách sản phẩm trên Frontend

---

## 🛒 SLIDE 5: Order Service

### Tiêu đề: **Order Service - Quản Lý Đơn Hàng**

**Nội dung:**
- **Chức năng:**
  - Tạo đơn hàng mới
  - Xem danh sách và chi tiết đơn hàng
  - Xem đơn hàng theo user
  - Cập nhật trạng thái đơn hàng
  - Tự động trừ tồn kho khi tạo đơn
- **Database:** PostgreSQL (`orderservice_db`)
- **Events:** RabbitMQ (`order.created`, `order.status.updated`)
- **Tích hợp:** Gọi Product Service để kiểm tra stock
- **Port:** 5003

**Gợi ý demo:** Tạo đơn hàng và show RabbitMQ events

---

## 🚪 SLIDE 6: API Gateway RabbitMQ

### Tiêu đề: **API Gateway RabbitMQ - Điều Hướng Requests**

**Nội dung:**
- **API Gateway RabbitMQ (Port 5010):**
  - Single entry point cho tất cả requests
  - Giao tiếp bất đồng bộ qua message queue
  - Sử dụng RabbitMQ để gửi/nhận requests
  - Mỗi service có RabbitMQConsumerService riêng
  - Load balancing tự nhiên với RabbitMQ
- **Luồng hoạt động:**
  - Frontend → API Gateway → RabbitMQ Queue
  - Consumer Service nhận message và xử lý
  - Response qua RabbitMQ về API Gateway → Frontend
- **Lợi ích:** 
  - Tách biệt client và services
  - Dễ scale với load balancing
  - Xử lý bất đồng bộ, không block

**Gợi ý sơ đồ:** Luồng request qua API Gateway RabbitMQ

---

## ⚖️ SLIDE 7: Load Balancing & Scaling

### Tiêu đề: **Load Balancing & Scaling với RabbitMQ**

**Nội dung:**
- **Load Balancing Tự Nhiên:**
  - RabbitMQ tự động phân phối messages đều cho các consumer instances
  - Nhiều containers cùng service → RabbitMQ round-robin messages
  - Không cần cấu hình thêm, tự động hoạt động
- **Scaling Horizontal:**
  - Chạy thêm container instances khi cần tăng tải
  - Ví dụ: `user-service` + `user-service-v2` cùng lắng nghe queue `api.user.request`
  - RabbitMQ tự động phân phối requests cho cả 2 instances
- **Lợi ích:**
  - Tăng throughput mà không cần thay đổi code
  - Fault tolerance - một instance lỗi, instance khác vẫn hoạt động
  - Dễ scale từng service độc lập theo nhu cầu
- **Demo:**
  - Show nhiều containers cùng service đang chạy
  - Giải thích cách RabbitMQ phân phối messages

**Gợi ý sơ đồ:** Nhiều instances cùng lắng nghe một queue

---

## 🛠️ SLIDE 8: Công Nghệ & Công Cụ

### Tiêu đề: **Công Nghệ Sử Dụng**

**Nội dung:**
- **Backend:**
  - .NET 8.0 - Framework chính
  - Entity Framework Core - ORM
  - RabbitMQ.Client - Message Queue
- **Database:**
  - PostgreSQL - Database chính (3 databases riêng)
  - MongoDB - Logging và events
- **Messaging:**
  - RabbitMQ - Message queue cho async communication
- **Frontend:**
  - Angular 17+ - Framework
  - Angular Material - UI Components
- **DevOps:**
  - Docker & Docker Compose - Containerization
  - Swagger - API Documentation

**Gợi ý hình ảnh:** Logo các công nghệ

---

## 🎬 SLIDE 9: Demo - Tạo User & Product

### Tiêu đề: **Demo: Quản Lý User & Product**

**Nội dung:**
- **Tạo User mới:**
  - Qua Frontend: Form đăng ký
  - Qua Swagger: POST /api/users
  - Show response và database
- **Tạo Product:**
  - Thêm sản phẩm với thông tin đầy đủ
  - Set giá và tồn kho
  - Show danh sách sản phẩm
- **Tích hợp:**
  - User Service và Product Service hoạt động độc lập
  - Mỗi service có database riêng

**Gợi ý:** Live demo trên Frontend hoặc Swagger

---

## 🛒 SLIDE 10: Demo - Tạo Đơn Hàng

### Tiêu đề: **Demo: Tạo Đơn Hàng & RabbitMQ Events**

**Nội dung:**
- **Tạo Order:**
  - Chọn user và sản phẩm
  - Nhập số lượng
  - Tự động kiểm tra tồn kho
  - Tự động trừ stock khi tạo đơn
- **RabbitMQ Events:**
  - Show event `order.created` trong RabbitMQ
  - Order Service publish event sau khi tạo đơn
  - Các service khác có thể subscribe
- **Tích hợp Services:**
  - Order Service gọi Product Service để lấy thông tin
  - Order Service cập nhật stock của Product Service

**Gợi ý:** Show RabbitMQ Management UI với events

---

## 🔄 SLIDE 11: Giao Tiếp Giữa Services

### Tiêu đề: **Giao Tiếp Giữa Microservices**

**Nội dung:**
- **Synchronous (HTTP/REST):**
  - Order Service gọi Product Service qua HTTP
  - Lấy thông tin sản phẩm (giá, tồn kho)
  - Cập nhật stock sau khi tạo đơn
- **Asynchronous (RabbitMQ):**
  - Order Service publish event `order.created`
  - Các service khác có thể subscribe
  - Decoupling giữa các services
- **API Gateway RabbitMQ (Chính):**
  - Frontend → API Gateway RabbitMQ → RabbitMQ Queue
  - RabbitMQConsumerService nhận message và xử lý
  - Response qua RabbitMQ về API Gateway → Frontend
  - Tất cả requests đều qua message queue

**Gợi ý sơ đồ:** Luồng giao tiếp sync và async

---

## ✅ SLIDE 12: Ưu Điểm & Thách Thức

### Tiêu đề: **Ưu Điểm & Thách Thức**

**Nội dung:**
- **Ưu điểm:**
  - Mỗi service có thể scale độc lập
  - Dễ bảo trì và phát triển
  - Tách biệt database, tránh conflict
  - Công nghệ linh hoạt cho từng service
  - Fault isolation - một service lỗi không ảnh hưởng toàn hệ thống
- **Thách thức:**
  - Phức tạp hơn monolithic
  - Cần quản lý nhiều services
  - Distributed transactions phức tạp
  - Cần infrastructure (RabbitMQ, MongoDB)

**Gợi ý:** So sánh với Monolithic architecture

---

## 🚀 SLIDE 13: Kết Luận & Hướng Phát Triển

### Tiêu đề: **Kết Luận & Hướng Phát Triển**

**Nội dung:**
- **Kết luận:**
  - Đã xây dựng thành công hệ thống E-Commerce theo kiến trúc Microservice
  - 3 microservices hoạt động độc lập
  - API Gateway RabbitMQ xử lý tất cả requests qua message queue
  - Tích hợp RabbitMQ cho async communication
  - Frontend Angular kết nối qua API Gateway RabbitMQ (Port 5010)
- **Hướng phát triển:**
  - Thêm Authentication & Authorization (JWT)
  - Implement Service Discovery
  - Thêm Monitoring & Logging (ELK Stack)
  - Deploy lên cloud (AWS, Azure)
  - Thêm Unit Tests & Integration Tests
- **Cảm ơn!** Q&A

**Gợi ý:** Timeline roadmap hoặc architecture diagram

---

## 📝 Ghi Chú Cho Người Thuyết Trình

### Thời gian phân bổ (10 phút):
- **Slide 1-2:** 1.5 phút - Giới thiệu và kiến trúc
- **Slide 3-6:** 2 phút - Chi tiết các services
- **Slide 7:** 1 phút - Load Balancing & Scaling
- **Slide 8:** 0.5 phút - Công nghệ
- **Slide 9-10:** 3 phút - Demo (quan trọng nhất!)
- **Slide 11-12:** 1.5 phút - Giao tiếp và đánh giá
- **Slide 13:** 0.5 phút - Kết luận

### Tips:
- Chuẩn bị sẵn demo trên Frontend và Swagger
- Mở RabbitMQ Management UI để show events
- Có thể skip một số slides nếu thiếu thời gian
- Tập trung vào demo (Slide 8-9) vì đây là phần quan trọng nhất

---

## 🎯 Checklist Trước Khi Thuyết Trình

- [ ] Tất cả services đang chạy
- [ ] Frontend đang chạy và có dữ liệu mẫu
- [ ] Swagger UI của các services đã mở
- [ ] RabbitMQ Management UI đã mở
- [ ] Đã test các chức năng demo
- [ ] Chuẩn bị sẵn các câu hỏi có thể được hỏi

