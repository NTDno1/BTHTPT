# 📊 Tổng Quan Dự Án Microservice

## 🎯 Mục Đích

Dự án triển khai hệ thống **E-Commerce Backend** theo kiến trúc **Microservice**, minh họa các nguyên tắc từ giáo trình "Các Hệ Thống Phân Tán".

> **💡 Lưu ý:** MongoDB và RabbitMQ là **infrastructure services** được các microservices sử dụng trực tiếp. Xem [GIAI_THICH_KIEN_TRUC.md](./GIAI_THICH_KIEN_TRUC.md) để hiểu rõ hơn.

---

## 🏗️ Kiến Trúc Tổng Thể

```
┌─────────────────────────────────────────┐
│         FRONTEND (Angular)              │
│         http://localhost:4200           │
└──────────────────┬──────────────────────┘
                    │ HTTP Requests
                    ▼
┌─────────────────────────────────────────┐
│         API GATEWAY (Ocelot)            │
│         http://localhost:5000            │
│  - Điều hướng requests                 │
│  - Single entry point                  │
└──────┬──────────┬──────────┬────────────┘
       │          │          │
       ▼          ▼          ▼
┌──────────┐ ┌──────────┐ ┌──────────┐
│  USER    │ │ PRODUCT  │ │  ORDER   │
│ SERVICE  │ │ SERVICE  │ │ SERVICE  │
│  :5001   │ │  :5002   │ │  :5003   │
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
│ (Logging)    │        │ (Messages)   │
└──────────────┘        └──────────────┘
```

---

## 🎨 Các Tính Năng

### 1. 👥 User Service (Port 5001)

**Chức năng:**
- ✅ Đăng ký tài khoản
- ✅ Xem danh sách users
- ✅ Xem chi tiết user
- ✅ Cập nhật thông tin
- ✅ Xóa user (soft delete)

**API:** `GET|POST|PUT|DELETE /api/users`

**Database:** `userservice_db` (PostgreSQL)

---

### 2. 📦 Product Service (Port 5002)

**Chức năng:**
- ✅ Xem danh sách sản phẩm
- ✅ Tìm kiếm theo category
- ✅ Thêm/sửa/xóa sản phẩm
- ✅ Quản lý tồn kho

**API:** `GET|POST|PUT|DELETE /api/products`, `GET /api/products/category/{category}`, `PATCH /api/products/{id}/stock`

**Database:** `productservice_db` (PostgreSQL)

---

### 3. 🛒 Order Service (Port 5003)

**Chức năng:**
- ✅ Tạo đơn hàng mới
- ✅ Xem danh sách đơn hàng
- ✅ Xem đơn hàng theo user
- ✅ Cập nhật trạng thái
- ✅ Tích hợp RabbitMQ

**API:** `GET|POST|PUT|DELETE /api/orders`, `GET /api/orders/user/{userId}`, `PUT /api/orders/{id}/status`

**Database:** `orderservice_db` (PostgreSQL)

**RabbitMQ Events:**
- `order.created`
- `order.status.updated`

---

### 4. 🚪 API Gateway (Port 5000)

**Chức năng:**
- ✅ Single entry point
- ✅ Route requests
- ✅ Load balancing
- ✅ Swagger documentation

---

## 🛠️ Công Nghệ

| Component | Technology |
|-----------|-----------|
| Backend Framework | .NET 8.0 |
| ORM | Entity Framework Core |
| Database | PostgreSQL |
| Logging | MongoDB |
| Message Queue | RabbitMQ |
| API Gateway | Ocelot |
| Frontend | Angular 17+ |
| UI Library | Angular Material |

---

## 📈 Luồng Hoạt Động

### Luồng Client Request:
```
Frontend → API Gateway → Microservice → PostgreSQL
                              ↓
                          MongoDB (log)
```

### Luồng Event-Driven:
```
Order Service → RabbitMQ → [Other Services subscribe]
     ↓
MongoDB (log event)
```

---

## 🎯 Điểm Nổi Bật

1. ✅ **Microservice Architecture** - Mỗi service độc lập
2. ✅ **Database Per Service** - Mỗi service có database riêng
3. ✅ **API Gateway Pattern** - Single entry point
4. ✅ **Event-Driven** - RabbitMQ cho async communication
5. ✅ **Swagger UI** - Tất cả services có documentation

---

## 📚 Tài Liệu

- [README.md](./README.md) - Tổng quan
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Kiến trúc chi tiết
- [HUONG_DAN_CHAY_DU_AN.md](./HUONG_DAN_CHAY_DU_AN.md) - Hướng dẫn chạy
- [KICH_BAN_DEMO.md](./KICH_BAN_DEMO.md) - Kịch bản demo
