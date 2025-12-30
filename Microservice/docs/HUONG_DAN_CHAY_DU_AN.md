# 📖 Hướng Dẫn Chạy Dự Án

Hướng dẫn chi tiết từng bước để chạy dự án Microservice.

---

## 📋 Mục Lục

1. [Yêu Cầu Hệ Thống](#yêu-cầu-hệ-thống)
2. [Chuẩn Bị Databases](#chuẩn-bị-databases)
3. [Cách 1: Chạy Local](#cách-1-chạy-local)
4. [Cách 2: Chạy Bằng Docker](#cách-2-chạy-bằng-docker)
5. [Chạy Frontend](#chạy-frontend)
6. [Kiểm Tra và Test](#kiểm-tra-và-test)
7. [Troubleshooting](#troubleshooting)

---

## ✅ Yêu Cầu Hệ Thống

### Phần Mềm:
- **.NET 8.0 SDK** - https://dotnet.microsoft.com/download/dotnet/8.0
- **Node.js 18+** (cho Frontend)
- **Docker Desktop** (nếu chạy bằng Docker)

### Kết Nối:
- ✅ PostgreSQL: `47.130.33.106:5432`
- ✅ RabbitMQ: `47.130.33.106:5672`
- ✅ MongoDB Atlas (internet)

---

## 🗄️ Chuẩn Bị Databases

### 1. Tạo PostgreSQL Databases

Kết nối PostgreSQL và tạo 3 databases:

```sql
CREATE DATABASE userservice_db;
CREATE DATABASE productservice_db;
CREATE DATABASE orderservice_db;
```

**Thông tin kết nối:**
- Server: 47.130.33.106
- Port: 5432
- Username: postgres
- Password: 123456

### 2. Kiểm Tra MongoDB

MongoDB đã được cấu hình trong `appsettings.json`. Đảm bảo connection string đúng.

### 3. Kiểm Tra RabbitMQ

- Server: 47.130.33.106:5672
- Username: guest
- Password: guest

---

## 🚀 Cách 1: Chạy Local

### ⚡ Sử Dụng Script (Khuyến nghị)

```powershell
cd Microservice
.\run-all-services.ps1
```

Script sẽ tự động chạy tất cả services.

### 📝 Chạy Thủ Công

**Terminal 1 - User Service:**
```bash
cd Microservice/Microservice.Services.UserService
dotnet run
```
**Kết quả:** http://localhost:5001/swagger

**Terminal 2 - Product Service:**
```bash
cd Microservice/Microservice.Services.ProductService
dotnet run
```
**Kết quả:** http://localhost:5002/swagger

**Terminal 3 - Order Service:**
```bash
cd Microservice/Microservice.Services.OrderService
dotnet run
```
**Kết quả:** http://localhost:5003/swagger

**Terminal 4 - API Gateway:**
```bash
cd Microservice/Microservice.ApiGateway
dotnet run
```
**Kết quả:** http://localhost:5000/swagger

### ⚠️ Lưu Ý

- **Thứ tự:** Chạy services trước, sau đó mới chạy API Gateway
- **Ports:** Đảm bảo ports 5000-5003 không bị chiếm

---

## 🐳 Cách 2: Chạy Bằng Docker

### 📋 Yêu Cầu Trước Khi Build Docker

#### 1. Cài Đặt Docker Desktop

**Windows:**
- Tải và cài đặt [Docker Desktop for Windows](https://www.docker.com/products/docker-desktop/)
- Đảm bảo Docker Desktop đang chạy (icon Docker trong system tray)
- Kiểm tra Docker đã sẵn sàng:
  ```powershell
  docker --version
  docker-compose --version
  ```

**Linux:**
```bash
# Cài đặt Docker
sudo apt-get update
sudo apt-get install docker.io docker-compose

# Khởi động Docker service
sudo systemctl start docker
sudo systemctl enable docker

# Thêm user vào docker group (không cần sudo)
sudo usermod -aG docker $USER
```

#### 2. Kiểm Tra Kết Nối External Services

Trước khi build Docker, đảm bảo các services external có thể truy cập được:

- ✅ **PostgreSQL:** `47.130.33.106:5432` - Phải có 3 databases: `userservice_db`, `productservice_db`, `orderservice_db`
- ✅ **RabbitMQ:** `47.130.33.106:5672` - Username: `guest`, Password: `guest`
- ✅ **MongoDB Atlas:** Connection string trong `appsettings.json` phải hợp lệ

#### 3. Kiểm Tra Ports

Đảm bảo các ports sau không bị chiếm:
- `5000` - API Gateway
- `5001` - User Service
- `5002` - Product Service
- `5003` - Order Service
- `5010` - API Gateway RabbitMQ

**Windows:**
```powershell
netstat -ano | findstr ":5000 :5001 :5002 :5003 :5010"
```

**Linux/Mac:**
```bash
lsof -i :5000 -i :5001 -i :5002 -i :5003 -i :5010
```

---

### 🏗️ Hiểu Về Cấu Trúc Docker

Dự án sử dụng **multi-stage build** với các Dockerfile riêng cho từng service:

```
Microservice/
├── docker-compose.yml          # Orchestration file
├── .dockerignore               # Files to ignore khi build
├── Microservice.Services.UserService/
│   └── Dockerfile              # Build User Service
├── Microservice.Services.ProductService/
│   └── Dockerfile              # Build Product Service
├── Microservice.Services.OrderService/
│   └── Dockerfile              # Build Order Service
├── Microservice.ApiGateway/
│   └── Dockerfile              # Build API Gateway
└── Microservice.ApiGateway.RabbitMQ/
    └── Dockerfile              # Build API Gateway RabbitMQ
```

**Cấu trúc Dockerfile (Multi-stage build):**
1. **Base stage:** Sử dụng `aspnet:8.0` runtime image (nhẹ)
2. **Build stage:** Sử dụng `sdk:8.0` để compile code
3. **Publish stage:** Publish ứng dụng
4. **Final stage:** Copy published files vào base image

---

### 🔨 Cách 1: Build và Chạy Tất Cả Services (Khuyến nghị)

#### Bước 1: Di Chuyển Đến Thư Mục Dự Án

```bash
cd Microservice
```

#### Bước 2: Build và Khởi Động Tất Cả Containers

**Build và chạy tất cả services:**
```bash
docker-compose up -d --build
```

**Giải thích các tham số:**
- `up` - Khởi động containers
- `-d` - Chạy ở chế độ detached (background)
- `--build` - Build lại images trước khi chạy

**Chỉ build images (không chạy):**
```bash
docker-compose build
```

**Build lại một service cụ thể:**
```bash
docker-compose build user-service
docker-compose build product-service
docker-compose build order-service
docker-compose build api-gateway
docker-compose build api-gateway-rabbitmq
```

#### Bước 3: Kiểm Tra Trạng Thái Containers

**Xem danh sách containers:**
```bash
docker-compose ps
```

**Kết quả mong đợi:**
```
NAME                              STATUS              PORTS
microservice-api-gateway          Up                  0.0.0.0:5000->8080/tcp
microservice-api-gateway-rabbitmq Up                  0.0.0.0:5010->8080/tcp
microservice-order-service        Up                  0.0.0.0:5003->8080/tcp
microservice-product-service      Up                  0.0.0.0:5002->8080/tcp
microservice-user-service         Up                  0.0.0.0:5001->8080/tcp
```

**Xem logs của tất cả services:**
```bash
docker-compose logs -f
```

**Xem logs của một service cụ thể:**
```bash
docker-compose logs -f user-service
docker-compose logs -f product-service
docker-compose logs -f order-service
docker-compose logs -f api-gateway
```

#### Bước 4: Kiểm Tra Health của Services

**Truy cập Swagger UI:**
- API Gateway: http://localhost:5000/swagger
- User Service: http://localhost:5001/swagger
- Product Service: http://localhost:5002/swagger
- Order Service: http://localhost:5003/swagger
- API Gateway RabbitMQ: http://localhost:5010/swagger

**Test API qua Gateway:**
```bash
curl http://localhost:5000/swagger/index.html
```

---

### 🔧 Cách 2: Build Từng Image Riêng Lẻ

Nếu muốn build từng service riêng lẻ để kiểm tra hoặc debug:

#### Build User Service

```bash
cd Microservice
docker build -f Microservice.Services.UserService/Dockerfile -t microservice-user-service:latest .
```

**Chạy container:**
```bash
docker run -d \
  --name user-service \
  -p 5001:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e ConnectionStrings__PostgreSQL="Host=47.130.33.106;Port=5432;Database=userservice_db;Username=postgres;Password=123456" \
  -e MongoDb__ConnectionString="mongodb+srv://datt19112001_db_user:1@mongodbdatnt.bc8xywz.mongodb.net/?retryWrites=true&w=majority" \
  -e MongoDb__Database=microservice_users \
  microservice-user-service:latest
```

#### Build Product Service

```bash
docker build -f Microservice.Services.ProductService/Dockerfile -t microservice-product-service:latest .
```

**Chạy container:**
```bash
docker run -d \
  --name product-service \
  -p 5002:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__PostgreSQL="Host=47.130.33.106;Port=5432;Database=productservice_db;Username=postgres;Password=123456" \
  -e MongoDb__ConnectionString="mongodb+srv://datt19112001_db_user:1@mongodbdatnt.bc8xywz.mongodb.net/?retryWrites=true&w=majority" \
  -e MongoDb__Database=microservice_products \
  microservice-product-service:latest
```

#### Build Order Service

```bash
docker build -f Microservice.Services.OrderService/Dockerfile -t microservice-order-service:latest .
```

**Chạy container:**
```bash
docker run -d \
  --name order-service \
  -p 5003:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__PostgreSQL="Host=47.130.33.106;Port=5432;Database=orderservice_db;Username=postgres;Password=123456" \
  -e RabbitMQ__HostName=47.130.33.106 \
  -e RabbitMQ__Port=5672 \
  -e RabbitMQ__UserName=guest \
  -e RabbitMQ__Password=guest \
  microservice-order-service:latest
```

#### Build API Gateway

```bash
docker build -f Microservice.ApiGateway/Dockerfile -t microservice-api-gateway:latest .
```

**Chạy container:**
```bash
docker run -d \
  --name api-gateway \
  -p 5000:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  --link user-service:user-service \
  --link product-service:product-service \
  --link order-service:order-service \
  microservice-api-gateway:latest
```

---

### 📦 Quản Lý Docker Images và Containers

#### Xem Danh Sách Images

```bash
docker images | grep microservice
```

#### Xóa Images Không Dùng

```bash
# Xóa tất cả images không dùng
docker image prune -a

# Xóa một image cụ thể
docker rmi microservice-user-service:latest
```

#### Xem Danh Sách Containers

```bash
# Containers đang chạy
docker ps

# Tất cả containers (bao gồm đã dừng)
docker ps -a
```

#### Dừng và Xóa Containers

**Dừng tất cả services:**
```bash
docker-compose down
```

**Dừng và xóa volumes:**
```bash
docker-compose down -v
```

**Dừng một service cụ thể:**
```bash
docker-compose stop user-service
```

**Khởi động lại một service:**
```bash
docker-compose restart user-service
```

**Xóa một container:**
```bash
docker stop user-service
docker rm user-service
```

#### Rebuild và Restart

**Rebuild và restart tất cả:**
```bash
docker-compose down
docker-compose up -d --build
```

**Rebuild một service cụ thể:**
```bash
docker-compose up -d --build --no-deps user-service
```

---

### 🐛 Troubleshooting Docker

#### 1. Lỗi Build Docker

**Lỗi: "Cannot connect to Docker daemon"**
```bash
# Windows: Đảm bảo Docker Desktop đang chạy
# Linux: Khởi động Docker service
sudo systemctl start docker
```

**Lỗi: "Port already in use"**
```bash
# Tìm process đang dùng port
# Windows:
netstat -ano | findstr :5001
taskkill /PID <PID> /F

# Linux/Mac:
lsof -ti:5001 | xargs kill -9
```

**Lỗi: "Build context too large"**
- Kiểm tra file `.dockerignore` đã loại trừ `bin/`, `obj/`, `node_modules/`
- Xóa các thư mục build cũ:
  ```bash
  # Windows PowerShell
  Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force
  
  # Linux/Mac
  find . -type d -name "bin" -o -name "obj" | xargs rm -rf
  ```

#### 2. Lỗi Kết Nối Từ Container

**Container không kết nối được PostgreSQL/RabbitMQ:**

- Kiểm tra network: Containers phải có thể truy cập external IP
  ```bash
  # Test từ container
  docker exec -it microservice-user-service ping 47.130.33.106
  ```

- Kiểm tra firewall: Đảm bảo ports 5432, 5672 không bị block

- Kiểm tra connection strings trong `docker-compose.yml`

**Container không giao tiếp với nhau:**

- Kiểm tra network: Tất cả containers phải cùng network `microservice-network`
  ```bash
  docker network inspect microservice-network
  ```

- Kiểm tra service names: Trong `docker-compose.yml`, services gọi nhau bằng tên service (ví dụ: `user-service:8080`)

#### 3. Lỗi Runtime

**Container crash ngay sau khi start:**

- Xem logs để tìm lỗi:
  ```bash
  docker-compose logs user-service
  ```

- Kiểm tra environment variables:
  ```bash
  docker exec microservice-user-service env
  ```

**Service không response:**

- Kiểm tra container đang chạy:
  ```bash
  docker-compose ps
  ```

- Restart service:
  ```bash
  docker-compose restart user-service
  ```

- Kiểm tra port mapping:
  ```bash
  docker port microservice-user-service
  ```

#### 4. Xóa và Build Lại Từ Đầu

Nếu gặp lỗi không rõ nguyên nhân, có thể xóa tất cả và build lại:

```bash
# Dừng và xóa tất cả containers, networks
docker-compose down -v

# Xóa tất cả images của dự án
docker images | grep microservice | awk '{print $3}' | xargs docker rmi -f

# Build lại từ đầu
docker-compose build --no-cache
docker-compose up -d
```

---

### 💡 Best Practices

#### 1. Sử Dụng .dockerignore

File `.dockerignore` đã được cấu hình để loại trừ:
- `bin/`, `obj/` - Build artifacts
- `node_modules/` - Dependencies
- `.git/`, `.vs/` - Version control và IDE files

#### 2. Multi-stage Build

Dockerfiles sử dụng multi-stage build để:
- Giảm kích thước image cuối cùng
- Tách biệt build environment và runtime environment
- Tăng tốc độ build với layer caching

#### 3. Environment Variables

Sử dụng environment variables trong `docker-compose.yml` thay vì hardcode trong code:
- Dễ thay đổi cấu hình
- Bảo mật thông tin nhạy cảm
- Hỗ trợ nhiều môi trường (dev, staging, production)

#### 4. Health Checks

Có thể thêm health checks vào `docker-compose.yml`:
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s
  timeout: 10s
  retries: 3
```

#### 5. Resource Limits

Trong production, nên thêm resource limits:
```yaml
deploy:
  resources:
    limits:
      cpus: '0.5'
      memory: 512M
    reservations:
      cpus: '0.25'
      memory: 256M
```

---

### 📝 Tóm Tắt Các Lệnh Thường Dùng

```bash
# Build và chạy tất cả
docker-compose up -d --build

# Xem logs
docker-compose logs -f

# Xem trạng thái
docker-compose ps

# Dừng tất cả
docker-compose down

# Restart một service
docker-compose restart user-service

# Rebuild một service
docker-compose up -d --build --no-deps user-service

# Xem logs một service
docker-compose logs -f user-service

# Vào trong container
docker exec -it microservice-user-service /bin/bash

# Xóa tất cả và build lại
docker-compose down -v
docker-compose build --no-cache
docker-compose up -d
```

---

## 🎨 Chạy Frontend

```bash
cd Microservice/Frontend
npm install
npm start
```

**Truy cập:** http://localhost:4200

---

## ✅ Kiểm Tra và Test

### 1. Kiểm Tra Services

Truy cập Swagger UI:
- API Gateway: http://localhost:5000/swagger
- User Service: http://localhost:5001/swagger
- Product Service: http://localhost:5002/swagger
- Order Service: http://localhost:5003/swagger

### 2. Test API

**Tạo User:**
```bash
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{"username":"test","email":"test@example.com","password":"123","firstName":"Test","lastName":"User"}'
```

**Tạo Product:**
```bash
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Laptop","description":"High performance","price":15000000,"stock":10,"category":"Electronics"}'
```

**Tạo Order:**
```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{"userId":1,"shippingAddress":"123 Main St","orderItems":[{"productId":1,"quantity":2}]}'
```

---

## 🔧 Troubleshooting

### Lỗi Kết Nối PostgreSQL

- Kiểm tra server `47.130.33.106:5432` có thể truy cập
- Kiểm tra databases đã được tạo
- Kiểm tra username/password: `postgres/123456`

### Lỗi Kết Nối MongoDB

- Kiểm tra connection string trong appsettings.json
- Kiểm tra MongoDB Atlas cluster đang hoạt động
- Kiểm tra network access (whitelist IP)

### Lỗi Kết Nối RabbitMQ

- Kiểm tra server `47.130.33.106:5672`
- Kiểm tra credentials: `guest/guest`
- Kiểm tra firewall/network

### Port Đã Được Sử Dụng

**Windows:**
```powershell
netstat -ano | findstr :5001
taskkill /PID <PID> /F
```

**Linux/Mac:**
```bash
lsof -ti:5001 | xargs kill -9
```

### API Gateway Không Route Được

- Đảm bảo các services đã chạy trước
- Kiểm tra file `ocelot.json`
- Kiểm tra ports trong ocelot.json khớp với services

---

## 📚 Xem Thêm

- **Quick Start:** [QUICKSTART.md](./QUICKSTART.md)
- **Kịch bản demo:** [KICH_BAN_DEMO.md](./KICH_BAN_DEMO.md)
- **Kiến trúc:** [ARCHITECTURE.md](./ARCHITECTURE.md)


cd ~/BTHTPT/Microservice
sed -i '/volumes:/,+1d' docker-compose.yml
cat docker-compose.yml | grep volumes
docker-compose up -d --build

docker volume create portainer_data

docker run -d -p 9000:9000 -p 9443:9443 \
    --name=portainer \
    --restart=always \
    -v /var/run/docker.sock:/var/run/docker.sock \
    -v portainer_data:/data \
    portainer/portainer-ce:latest


docker run -d --name order-service-v2 -p 5013:8080 sha256:5083346a0885e4b7627a40445a298689430ada0bf6da37956d8e5e538715e433
docker run -d --name user-service-v2 -p 5011:8080 sha256:fc26a26b5a740c1398fa944ae596a8ca0ecafe81fa6978e6b8ad0d3a77f386d1
docker run -d --name product-service-v2 -p 5012:8080 sha256:3b22956169dd35d24d61ed4abae6ad076fc113b5860c36defa7ae29cfb7b54ab