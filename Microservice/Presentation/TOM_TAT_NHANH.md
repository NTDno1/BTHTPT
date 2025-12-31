# ⚡ Tóm Tắt Nhanh - Thuyết Trình Microservice

## 📋 Checklist Trước Khi Thuyết Trình

- [ ] Tất cả services đang chạy (User, Product, Order, API Gateway)
- [ ] Frontend Angular đang chạy trên port 4200
- [ ] Có dữ liệu mẫu (users, products)
- [ ] Swagger UI của các services đã mở sẵn
- [ ] RabbitMQ Management UI đã mở
- [ ] Đã test các chức năng demo
- [ ] Chuẩn bị sẵn các câu hỏi có thể được hỏi

---

## ⏱️ Phân Bổ Thời Gian (10 phút)

| Slide | Nội dung | Thời gian |
|-------|----------|-----------|
| 1-2 | Giới thiệu + Kiến trúc | 1.5 phút |
| 3-6 | Chi tiết services | 2 phút |
| 7 | **Load Balancing & Scaling** | 1 phút |
| 8 | Công nghệ | 0.5 phút |
| 9-10 | **DEMO** (quan trọng!) | 3 phút |
| 11-12 | Giao tiếp + Đánh giá | 1.5 phút |
| 13 | Kết luận | 0.5 phút |

---

## 🎯 Điểm Nhấn Chính

### 1. Kiến Trúc (Slide 2)
- Frontend → API Gateway RabbitMQ → RabbitMQ Queue → 3 Microservices
- Mỗi service có RabbitMQConsumerService và database riêng
- MongoDB cho logging, RabbitMQ cho messaging & API Gateway

### 2. Load Balancing (Slide 7) ⭐ MỚI
- **Nhiều containers cùng service:** user-service + user-service-v2
- **RabbitMQ tự động phân phối:** Round-robin messages
- **Scaling horizontal:** Chỉ cần chạy thêm containers
- **Fault tolerance:** Một instance lỗi, instance khác vẫn hoạt động

### 3. Demo Quan Trọng (Slide 9-10)
- **Tạo User & Product:** Show tính độc lập
- **Tạo Order:** Show tích hợp giữa services + RabbitMQ events

### 4. Giao Tiếp (Slide 11)
- **API Gateway:** RabbitMQ Queue (Frontend → Services)
- **Giữa Services:** HTTP/REST (Order → Product)
- **Events:** RabbitMQ (order.created, order.status.updated)

---

## 💬 Câu Nói Mở Đầu

> "Xin chào thầy và các bạn. Hôm nay em sẽ trình bày về dự án E-Commerce Backend được xây dựng theo kiến trúc Microservice. Dự án này giải quyết các vấn đề của kiến trúc monolithic truyền thống như khó scale, khó bảo trì, và rủi ro khi một phần lỗi ảnh hưởng toàn hệ thống."

---

## 🎬 Demo Script Ngắn Gọn

### Demo 1: Tạo User (30 giây)
1. Mở Frontend → Tab Users
2. Click "Tạo mới"
3. Điền thông tin → Lưu
4. Show: "User đã được tạo, lưu trong database riêng"

### Demo 2: Tạo Product (30 giây)
1. Tab Products → "Tạo mới"
2. Điền thông tin sản phẩm → Lưu
3. Show: "Product Service hoạt động độc lập với User Service"

### Demo 3: Tạo Order (1.5 phút) ⭐ QUAN TRỌNG
1. Tab Orders → "Tạo mới"
2. Chọn User và Product
3. Nhập số lượng → Lưu
4. **Giải thích:**
   - "Order Service gọi Product Service để kiểm tra tồn kho"
   - "Tự động trừ stock sau khi tạo đơn"
   - "Publish event 'order.created' lên RabbitMQ"
5. Mở RabbitMQ Management UI → Show queue

### Demo 4: Load Balancing (Nếu có thời gian) ⭐ MỚI
1. Show Docker containers đang chạy:
   - user-service + user-service-v2
   - product-service + product-service-v2
   - order-service + order-service-v2
2. **Giải thích:**
   - "Cả 2 instances cùng lắng nghe một queue"
   - "RabbitMQ tự động phân phối requests đều"
   - "Tăng throughput mà không cần thay đổi code"
3. Gửi nhiều requests → Show logs của cả 2 instances xử lý

---

## ❓ Câu Hỏi Thường Gặp (Q&A)

### Q1: Tại sao chọn Microservice thay vì Monolithic?
**A:** 
- Scale độc lập từng service
- Dễ bảo trì và phát triển
- Fault isolation
- Công nghệ linh hoạt

### Q2: Làm thế nào xử lý distributed transactions?
**A:**
- Sử dụng Saga pattern
- Event-driven architecture với RabbitMQ
- Compensating transactions

### Q3: Tại sao chọn API Gateway RabbitMQ?
**A:**
- **Bất đồng bộ:** Xử lý requests qua message queue
- **Load balancing tự nhiên:** RabbitMQ phân phối messages đều cho các instances
- **Decoupling:** Tách biệt Frontend và Services
- **Dễ scale:** Chỉ cần tăng số instances của service

### Q4: Cách scale services như thế nào?
**A:**
- Docker containers - chạy nhiều instances cùng service
- Load balancing tự động với RabbitMQ
- RabbitMQ phân phối messages đều cho các instances (round-robin)
- Scale từng service độc lập theo nhu cầu
- Ví dụ: user-service + user-service-v2 cùng lắng nghe queue

### Q5: Làm thế nào RabbitMQ phân phối tải?
**A:**
- RabbitMQ sử dụng round-robin algorithm
- Mỗi consumer nhận một message, xử lý xong mới nhận message tiếp theo
- Nếu có 2 instances, message 1 → instance 1, message 2 → instance 2, message 3 → instance 1, ...
- Tự động, không cần cấu hình

---

## 📊 Sơ Đồ Kiến Trúc (Vẽ Nhanh)

```
Frontend (Angular)
    ↓
API Gateway RabbitMQ (:5010)
    ↓
RabbitMQ Queue
    ↓
┌───────┬─────────┬─────────┐
│ User  │ Product │ Order   │
│ :5001 │  :5002  │  :5003  │
│ :5011 │  :5012  │  :5013  │
│(v2)   │  (v2)   │  (v2)   │
│Consumer│Consumer│Consumer │
└───┬───┴────┬────┴────┬────┘
    │        │         │
    ▼        ▼         ▼
PostgreSQL (3 databases riêng)
    │        │         │
    └────────┴─────────┘
         │
    ┌────┴────┐
    ▼         ▼
 MongoDB   RabbitMQ
         (Load Balancing)
```

---

## 🎤 Câu Nói Kết Thúc

> "Tóm lại, chúng em đã xây dựng thành công hệ thống E-Commerce theo kiến trúc Microservice với 3 microservices và API Gateway RabbitMQ, tích hợp RabbitMQ cho async communication. Hướng phát triển tiếp theo bao gồm Authentication, Service Discovery, và Monitoring. Cảm ơn thầy và các bạn đã lắng nghe. Em xin mời các câu hỏi."

---

## ⚠️ Lưu Ý

1. **Đừng quá nhanh:** Nói rõ ràng, dễ hiểu
2. **Tập trung vào demo:** Đây là phần quan trọng nhất
3. **Chuẩn bị backup:** Nếu demo lỗi, có thể show code hoặc Swagger
4. **Tự tin:** Bạn đã làm dự án này, bạn hiểu rõ nhất!

