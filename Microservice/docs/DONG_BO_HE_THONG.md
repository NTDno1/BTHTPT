# ✅ Đồng Bộ Hệ Thống - Checklist

## 📋 Thông Tin Cấu Hình Đã Đồng Bộ

### Ports Configuration ✅

| Service | HTTP Port | Swagger URL | Notes |
|---------|-----------|-------------|-------|
| **API Gateway RabbitMQ (PRIMARY)** | **5010** | http://localhost:5010/swagger | Entry point chính |
| User Service Instance 1 | 5001 | http://localhost:5001/swagger | Load Balanced |
| User Service Instance 2 | 5004 | http://localhost:5004/swagger | Load Balanced |
| Product Service Instance 1 | 5002 | http://localhost:5002/swagger | Load Balanced |
| Product Service Instance 2 | 5006 | http://localhost:5006/swagger | Load Balanced |
| Order Service Instance 1 | 5003 | http://localhost:5003/swagger | Load Balanced |
| Order Service Instance 2 | 5007 | http://localhost:5007/swagger | Load Balanced |
| Frontend | 4200 | http://localhost:4200 | Angular app |

**Lưu ý:** API Gateway Ocelot (port 5000) đã bị disable.

### Database Configuration ✅

| Service | Database Name | Type | Connection |
|---------|---------------|------|------------|
| User Service | userservice_db | PostgreSQL | 47.130.33.106:5432 |
| Product Service | productservice_db | PostgreSQL | 47.130.33.106:5432 |
| Order Service | orderservice_db | PostgreSQL | 47.130.33.106:5432 |

### MongoDB Configuration ✅

| Service | Database | Collection |
|---------|----------|------------|
| User Service | microservice_users | user_logs |
| Product Service | microservice_products | product_logs |
| Order Service | microservice_orders | order_events |

**Connection String:** `mongodb+srv://datt19112001_db_user:1@mongodbdatnt.bc8xywz.mongodb.net/?retryWrites=true&w=majority`

### RabbitMQ Configuration ✅

- **Host:** 47.130.33.106
- **Port:** 5672
- **Username:** guest
- **Password:** guest
- **Management UI:** http://47.130.33.106:15672 (nếu có)

### API Gateway RabbitMQ Routes ✅

| Route | Service | Queue | Instances |
|-------|---------|-------|-----------|
| /api/users/* | User Service | api.user.request | 5001, 5004 |
| /api/auth/* | User Service | api.user.request | 5001, 5004 |
| /api/products/* | Product Service | api.product.request | 5002, 5006 |
| /api/orders/* | Order Service | api.order.request | 5003, 5007 |

**Load Balancing:** Tự động qua RabbitMQ (Round Robin)

---

## 📁 Files Đã Được Đồng Bộ

### Configuration Files ✅

- [x] `Microservice.ApiGateway/Properties/launchSettings.json` - Port 5000
- [x] `Microservice.Services.UserService/Properties/launchSettings.json` - Port 5001
- [x] `Microservice.Services.ProductService/Properties/launchSettings.json` - Port 5002
- [x] `Microservice.Services.OrderService/Properties/launchSettings.json` - Port 5003
- [x] `Microservice.ApiGateway/ocelot.json` - Routes configuration
- [x] `Microservice.*/appsettings.json` - Database và service settings

### Documentation Files ✅

- [x] `README.md` - Tổng quan, ports, database info
- [x] `HUONG_DAN_CHAY_DU_AN.md` - Hướng dẫn chạy với đúng ports
- [x] `QUICKSTART.md` - Quick start guide
- [x] `ARCHITECTURE.md` - Kiến trúc với đúng database names
- [x] `KICH_BAN_DEMO.md` - Kịch bản demo với đúng URLs
- [x] `TONG_QUAN_DU_AN.md` - Tổng quan dự án
- [x] `TONG_KET_DU_AN.md` - Tổng kết
- [x] `GIAI_THICH_KIEN_TRUC.md` - Giải thích kiến trúc
- [x] `THONG_TIN_DONG_BO.md` - Thông tin đồng bộ

### Frontend Files ✅

- [x] `Frontend/src/app/services/api.service.ts` - API_BASE_URL = http://localhost:5000/api
- [x] `Frontend/src/app/components/home/home.component.ts` - URLs hiển thị

### Scripts ✅

- [x] `run-all-services.ps1` - Hiển thị đúng URLs
- [x] `stop-all-services.ps1` - Script dừng services

---

## 🔍 Kiểm Tra Đồng Bộ

### Test Checklist:

1. **Kiểm tra Ports:**
   ```bash
   # Chạy từng service và kiểm tra port
   dotnet run --project Microservice.Services.UserService
   # Phải chạy trên http://localhost:5001
   ```

2. **Kiểm tra API Gateway RabbitMQ:**
   ```bash
   # Chạy API Gateway RabbitMQ
   dotnet run --project Microservice.ApiGateway.RabbitMQ
   # Phải chạy trên http://localhost:5010
   # Test route: http://localhost:5010/api/users
   ```

3. **Kiểm tra Swagger:**
   - API Gateway RabbitMQ: http://localhost:5010/swagger ✅
   - User Service Instance 1: http://localhost:5001/swagger ✅
   - User Service Instance 2: http://localhost:5004/swagger ✅
   - Product Service Instance 1: http://localhost:5002/swagger ✅
   - Product Service Instance 2: http://localhost:5006/swagger ✅
   - Order Service Instance 1: http://localhost:5003/swagger ✅
   - Order Service Instance 2: http://localhost:5007/swagger ✅

4. **Kiểm tra Frontend:**
   ```bash
   cd Frontend
   npm start
   # Phải kết nối được với http://localhost:5010/api
   ```

---

## 📝 Lưu Ý Quan Trọng

1. **Thứ tự chạy services:**
   - Chạy User, Product, Order Services trước
   - Sau đó mới chạy API Gateway

2. **Database:**
   - Đảm bảo 3 databases đã được tạo trong PostgreSQL
   - Connection strings đã được cấu hình trong appsettings.json

3. **RabbitMQ:**
   - Đảm bảo server 47.130.33.106:5672 có thể truy cập được
   - Chỉ Order Service sử dụng RabbitMQ

4. **Frontend:**
   - API_BASE_URL phải trỏ đến API Gateway RabbitMQ (port 5010)
   - Không trỏ trực tiếp đến các services
   - File: `Frontend/src/app/config/environment.ts` - `apiGatewayApiUrl`

---

## ✅ Kết Luận

Tất cả các file đã được đồng bộ:
- ✅ Ports configuration
- ✅ Database configuration
- ✅ API Gateway routes
- ✅ Documentation
- ✅ Frontend configuration
- ✅ Scripts

**Hệ thống đã sẵn sàng để chạy và demo!**

---

## 🔗 Xem Thêm

- [THONG_TIN_DONG_BO.md](./THONG_TIN_DONG_BO.md) - Thông tin đồng bộ đầy đủ
- [README.md](./README.md) - Tổng quan dự án
