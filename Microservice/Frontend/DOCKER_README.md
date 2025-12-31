# Docker Setup cho Frontend

## Cách Build và Chạy

### 1. Build Docker Image

```bash
cd Frontend
docker build -t microservice-frontend .
```

### 2. Chạy Container

```bash
docker run -d \
  -p 4200:80 \
  -e API_URL=http://localhost:5010/api \
  --name microservice-frontend \
  microservice-frontend
```

### 3. Sử dụng Docker Compose

```bash
# Từ thư mục root của project
docker-compose up -d frontend
```

## Cấu Hình API URL

API URL có thể được cấu hình qua environment variable `API_URL`:

- **API Gateway RabbitMQ:** `http://api-gateway-rabbitmq:8080/api` (trong Docker network)
- **API Gateway Ocelot:** `http://api-gateway:8080/api` (trong Docker network)
- **Local development:** `http://localhost:5010/api` hoặc `http://localhost:5000/api`

## Lưu Ý

1. **Khi chạy trong Docker:** Frontend sẽ chạy trên port 80 trong container, expose ra port 4200
2. **API URL:** Cần đảm bảo API URL trỏ đúng đến API Gateway
3. **CORS:** API Gateway cần được cấu hình để cho phép requests từ frontend domain

## Troubleshooting

### Frontend không kết nối được API

1. Kiểm tra API URL trong environment variable
2. Kiểm tra network connectivity giữa frontend và backend containers
3. Kiểm tra CORS configuration trong API Gateway
4. Xem logs: `docker logs microservice-frontend`

### Build fails

1. Đảm bảo `node_modules` đã được install: `npm install`
2. Kiểm tra Angular CLI version: `ng version`
3. Xem build logs để biết lỗi cụ thể

