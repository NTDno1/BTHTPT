# 🐳 Docker Setup cho Frontend Angular

## 📋 Tổng Quan

Frontend được containerize sử dụng multi-stage Docker build:
- **Stage 1:** Build Angular application với Node.js
- **Stage 2:** Serve static files với Nginx

## 🚀 Cách Sử Dụng

### Option 1: Sử dụng Docker Compose (Khuyến nghị)

```bash
# Từ thư mục root của project
docker-compose up -d frontend
```

Frontend sẽ chạy tại: `http://localhost:4200`

### Option 2: Build và chạy thủ công

```bash
cd Frontend

# Build image
docker build -t microservice-frontend .

# Chạy container
docker run -d \
  -p 4200:80 \
  -e API_URL=http://localhost:5010/api \
  --name microservice-frontend \
  microservice-frontend
```

## ⚙️ Cấu Hình

### Environment Variables

| Variable | Mô Tả | Mặc Định |
|----------|-------|----------|
| `API_URL` | URL của API Gateway | `http://localhost:5010/api` |

### API URL Options

1. **API Gateway RabbitMQ (Port 5010) - PRIMARY:**
   ```bash
   -e API_URL=http://localhost:5010/api
   ```

2. **Docker Network (nếu truy cập từ container):**
   ```bash
   -e API_URL=http://api-gateway-rabbitmq:8080/api
   ```

**Lưu ý:** API Gateway Ocelot (port 5000) đã bị disable, chỉ sử dụng RabbitMQ Gateway (port 5010).

## 🔧 Cấu Trúc Files

```
Frontend/
├── Dockerfile              # Multi-stage build
├── nginx.conf             # Nginx configuration
├── docker-entrypoint.sh   # Entrypoint script để inject API URL
└── .dockerignore          # Files to ignore khi build
```

## 📝 Nginx Configuration

Nginx được cấu hình để:
- Serve static files từ `/usr/share/nginx/html`
- Handle Angular routing (SPA) - tất cả routes trả về `index.html`
- Enable gzip compression
- Cache static assets (JS, CSS, images)
- Security headers

## 🐛 Troubleshooting

### 1. Frontend không kết nối được API

**Nguyên nhân:** API URL không đúng hoặc CORS issue

**Giải pháp:**
- Kiểm tra API URL trong environment variable
- Đảm bảo API Gateway đang chạy
- Kiểm tra CORS configuration trong API Gateway
- Xem browser console để xem lỗi cụ thể

### 2. Build fails

**Nguyên nhân:** Dependencies hoặc build errors

**Giải pháp:**
```bash
# Xóa node_modules và rebuild
cd Frontend
rm -rf node_modules package-lock.json
npm install
npm run build
```

### 3. Container không start

**Giải pháp:**
```bash
# Xem logs
docker logs microservice-frontend

# Kiểm tra container status
docker ps -a | grep frontend
```

### 4. API URL không được replace

**Nguyên nhân:** Script không chạy hoặc files đã được minified

**Giải pháp:**
- Kiểm tra entrypoint script có executable: `chmod +x docker-entrypoint.sh`
- Kiểm tra logs: `docker logs microservice-frontend`
- Đảm bảo environment variable `API_URL` được set

## 📊 Performance

- **Build time:** ~2-3 phút (lần đầu), ~30s (với cache)
- **Image size:** ~50-60MB (sau khi build)
- **Startup time:** < 1 giây

## 🔒 Security

- Nginx với security headers
- Gzip compression enabled
- Static assets caching
- No source code exposure

## 📚 Thêm Thông Tin

Xem thêm:
- [Docker Compose Documentation](../docker-compose.yml)
- [Angular Documentation](https://angular.io/docs)
- [Nginx Documentation](https://nginx.org/en/docs/)

