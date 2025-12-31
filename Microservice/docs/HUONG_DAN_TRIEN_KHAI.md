# 🚀 Hướng Dẫn Triển Khai Dự Án Microservice

Tài liệu này hướng dẫn cách triển khai hệ thống Microservice lên môi trường production. Mình đã test trên Ubuntu Server 20.04 và Windows Server, các bước dưới đây hoạt động tốt.

---

## 📋 Mục Lục

1. [Yêu Cầu Server](#yêu-cầu-server)
2. [Chuẩn Bị Môi Trường](#chuẩn-bị-môi-trường)
3. [Triển Khai Backend Services](#triển-khai-backend-services)
4. [Cấu Hình Nginx Reverse Proxy](#cấu-hình-nginx-reverse-proxy)
5. [Setup SSL/HTTPS](#setup-sslhttps)
6. [Triển Khai Frontend](#triển-khai-frontend)
7. [Monitoring và Logging](#monitoring-và-logging)
8. [Backup và Recovery](#backup-và-recovery)
9. [Troubleshooting](#troubleshooting)

---

## 💻 Yêu Cầu Server

### Tối Thiểu:
- **CPU:** 2 cores
- **RAM:** 4GB
- **Disk:** 20GB
- **OS:** Ubuntu 20.04+ hoặc Windows Server 2019+

### Khuyến Nghị (cho production):
- **CPU:** 4 cores
- **RAM:** 8GB+
- **Disk:** 50GB+ (SSD)
- **Network:** Public IP với domain name

### Phần Mềm Cần Cài:
- Docker và Docker Compose
- Nginx (cho reverse proxy)
- Git
- .NET 8.0 Runtime (nếu chạy không dùng Docker)

---

## 🔧 Chuẩn Bị Môi Trường

### 1. Cài Đặt Docker trên Ubuntu

```bash
# Update system
sudo apt-get update
sudo apt-get upgrade -y

# Cài đặt Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Thêm user vào docker group (không cần sudo)
sudo usermod -aG docker $USER

# Cài đặt Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/download/v2.20.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Kiểm tra
docker --version
docker-compose --version
```

### 2. Cài Đặt Nginx

```bash
sudo apt-get install nginx -y
sudo systemctl start nginx
sudo systemctl enable nginx
```

### 3. Clone Repository

```bash
cd /opt
sudo git clone <your-repo-url> microservice
cd microservice
```

Hoặc upload code lên server bằng FileZilla, WinSCP, etc.

### 4. Chuẩn Bị Database

Đảm bảo các databases đã được tạo trên PostgreSQL server:

```sql
CREATE DATABASE userservice_db;
CREATE DATABASE productservice_db;
CREATE DATABASE orderservice_db;
```

Kiểm tra kết nối từ server:
```bash
psql -h 47.130.33.106 -U postgres -d userservice_db
```

---

## 🐳 Triển Khai Backend Services

### Cách 1: Sử Dụng Docker Compose (Khuyến nghị)

#### Bước 1: Tạo File docker-compose.production.yml

Tạo file mới `docker-compose.production.yml` trong thư mục gốc:

```yaml
version: '3.8'

services:
  user-service:
    build:
      context: .
      dockerfile: Microservice.Services.UserService/Dockerfile
    container_name: microservice-user-service-prod
    restart: unless-stopped
    ports:
      - "5001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__PostgreSQL=Host=47.130.33.106;Port=5432;Database=userservice_db;Username=postgres;Password=YOUR_PASSWORD
      - MongoDb__ConnectionString=YOUR_MONGODB_CONNECTION_STRING
      - MongoDb__Database=microservice_users
    networks:
      - microservice-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  product-service:
    build:
      context: .
      dockerfile: Microservice.Services.ProductService/Dockerfile
    container_name: microservice-product-service-prod
    restart: unless-stopped
    ports:
      - "5002:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__PostgreSQL=Host=47.130.33.106;Port=5432;Database=productservice_db;Username=postgres;Password=YOUR_PASSWORD
      - MongoDb__ConnectionString=YOUR_MONGODB_CONNECTION_STRING
      - MongoDb__Database=microservice_products
    networks:
      - microservice-network

  order-service:
    build:
      context: .
      dockerfile: Microservice.Services.OrderService/Dockerfile
    container_name: microservice-order-service-prod
    restart: unless-stopped
    ports:
      - "5003:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__PostgreSQL=Host=47.130.33.106;Port=5432;Database=orderservice_db;Username=postgres;Password=YOUR_PASSWORD
      - MongoDb__ConnectionString=YOUR_MONGODB_CONNECTION_STRING
      - MongoDb__Database=microservice_orders
      - RabbitMQ__HostName=47.130.33.106
      - RabbitMQ__Port=5672
      - RabbitMQ__UserName=guest
      - RabbitMQ__Password=guest
      - ServiceUrls__ProductService=http://product-service:8080
      - ServiceUrls__UserService=http://user-service:8080
    networks:
      - microservice-network

  api-gateway:
    build:
      context: .
      dockerfile: Microservice.ApiGateway/Dockerfile
    container_name: microservice-api-gateway-prod
    restart: unless-stopped
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    depends_on:
      - user-service
      - product-service
      - order-service
    networks:
      - microservice-network

networks:
  microservice-network:
    driver: bridge
```

**Lưu ý:** Thay `YOUR_PASSWORD` và `YOUR_MONGODB_CONNECTION_STRING` bằng thông tin thực tế của bạn.

#### Bước 2: Build và Chạy

```bash
cd /opt/microservice

# Build images
docker-compose -f docker-compose.production.yml build

# Chạy services
docker-compose -f docker-compose.production.yml up -d

# Kiểm tra
docker-compose -f docker-compose.production.yml ps
docker-compose -f docker-compose.production.yml logs -f
```

#### Bước 3: Kiểm Tra Services

```bash
# Test từng service
curl http://localhost:5001/swagger
curl http://localhost:5002/swagger
curl http://localhost:5003/swagger
curl http://localhost:5000/swagger
```

### Cách 2: Chạy Trực Tiếp (Không Docker)

Nếu không muốn dùng Docker, có thể publish và chạy trực tiếp:

```bash
# Publish từng service
cd Microservice.Services.UserService
dotnet publish -c Release -o /opt/microservice/user-service

cd ../Microservice.Services.ProductService
dotnet publish -c Release -o /opt/microservice/product-service

cd ../Microservice.Services.OrderService
dotnet publish -c Release -o /opt/microservice/order-service

cd ../Microservice.ApiGateway
dotnet publish -c Release -o /opt/microservice/api-gateway
```

Tạo systemd service files để tự động khởi động:

**/etc/systemd/system/user-service.service:**
```ini
[Unit]
Description=User Service
After=network.target

[Service]
Type=simple
WorkingDirectory=/opt/microservice/user-service
ExecStart=/usr/bin/dotnet /opt/microservice/user-service/Microservice.Services.UserService.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5001

[Install]
WantedBy=multi-user.target
```

Tương tự tạo cho các services khác, sau đó:

```bash
sudo systemctl daemon-reload
sudo systemctl enable user-service
sudo systemctl start user-service
sudo systemctl status user-service
```

---

## 🌐 Cấu Hình Nginx Reverse Proxy

Nginx sẽ đóng vai trò reverse proxy, điều hướng requests đến các services và xử lý SSL.

### 1. Tạo Nginx Config

Tạo file `/etc/nginx/sites-available/microservice`:

```nginx
# API Gateway - Entry point chính
server {
    listen 80;
    server_name api.yourdomain.com;  # Thay bằng domain của bạn

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}

# User Service (nếu cần truy cập trực tiếp)
server {
    listen 80;
    server_name user-api.yourdomain.com;

    location / {
        proxy_pass http://localhost:5001;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
}

# Product Service
server {
    listen 80;
    server_name product-api.yourdomain.com;

    location / {
        proxy_pass http://localhost:5002;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
}

# Order Service
server {
    listen 80;
    server_name order-api.yourdomain.com;

    location / {
        proxy_pass http://localhost:5003;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
}
```

### 2. Enable Site

```bash
sudo ln -s /etc/nginx/sites-available/microservice /etc/nginx/sites-enabled/
sudo nginx -t  # Kiểm tra config
sudo systemctl reload nginx
```

### 3. Kiểm Tra

Truy cập `http://api.yourdomain.com/swagger` để xem API Gateway.

---

## 🔒 Setup SSL/HTTPS

Sử dụng Let's Encrypt để có SSL miễn phí:

### 1. Cài Đặt Certbot

```bash
sudo apt-get install certbot python3-certbot-nginx -y
```

### 2. Lấy SSL Certificate

```bash
sudo certbot --nginx -d api.yourdomain.com -d user-api.yourdomain.com -d product-api.yourdomain.com -d order-api.yourdomain.com
```

Certbot sẽ tự động cập nhật Nginx config để sử dụng HTTPS.

### 3. Auto Renewal

Let's Encrypt certificates hết hạn sau 90 ngày. Certbot tự động setup cron job để renew, nhưng có thể kiểm tra:

```bash
sudo certbot renew --dry-run
```

---

## 🎨 Triển Khai Frontend

### Cách 1: Build và Deploy Static Files

```bash
cd Frontend

# Install dependencies
npm install

# Build production
npm run build -- --configuration production

# Output sẽ ở thư mục dist/
```

### 2. Cấu Hình Nginx cho Frontend

Thêm vào `/etc/nginx/sites-available/microservice`:

```nginx
server {
    listen 80;
    server_name yourdomain.com www.yourdomain.com;

    root /opt/microservice/Frontend/dist/frontend/browser;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # Cache static assets
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

Sau đó copy files:
```bash
sudo cp -r Frontend/dist/frontend/browser/* /opt/microservice/Frontend/dist/frontend/browser/
```

### Cách 2: Chạy Frontend với PM2 (Node.js)

Nếu muốn chạy `ng serve` trên production (không khuyến nghị):

```bash
npm install -g pm2
cd Frontend
pm2 start "npm start" --name frontend
pm2 save
pm2 startup
```

---

## 📊 Monitoring và Logging

### 1. Xem Logs Docker

```bash
# Logs tất cả services
docker-compose -f docker-compose.production.yml logs -f

# Logs một service cụ thể
docker-compose -f docker-compose.production.yml logs -f user-service

# Logs với timestamp
docker-compose -f docker-compose.production.yml logs -f --timestamps
```

### 2. Monitoring với Docker Stats

```bash
# Xem resource usage
docker stats

# Xem một container cụ thể
docker stats microservice-user-service-prod
```

### 3. Setup Log Rotation

Tạo file `/etc/docker/daemon.json`:

```json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  }
}
```

Restart Docker:
```bash
sudo systemctl restart docker
```

### 4. Health Check Script

Tạo script kiểm tra health của services:

```bash
#!/bin/bash
# health-check.sh

services=("http://localhost:5000/health" "http://localhost:5001/health" "http://localhost:5002/health" "http://localhost:5003/health")

for service in "${services[@]}"; do
    if curl -f -s $service > /dev/null; then
        echo "✅ $service is UP"
    else
        echo "❌ $service is DOWN"
        # Có thể gửi email hoặc notification ở đây
    fi
done
```

Chạy định kỳ với cron:
```bash
chmod +x health-check.sh
crontab -e
# Thêm dòng: */5 * * * * /opt/microservice/health-check.sh
```

---

## 💾 Backup và Recovery

### 1. Backup PostgreSQL Databases

Tạo script backup:

```bash
#!/bin/bash
# backup-databases.sh

BACKUP_DIR="/opt/backups"
DATE=$(date +%Y%m%d_%H%M%S)

mkdir -p $BACKUP_DIR

# Backup từng database
pg_dump -h 47.130.33.106 -U postgres userservice_db > $BACKUP_DIR/userservice_db_$DATE.sql
pg_dump -h 47.130.33.106 -U postgres productservice_db > $BACKUP_DIR/productservice_db_$DATE.sql
pg_dump -h 47.130.33.106 -U postgres orderservice_db > $BACKUP_DIR/orderservice_db_$DATE.sql

# Compress
tar -czf $BACKUP_DIR/backup_$DATE.tar.gz $BACKUP_DIR/*.sql
rm $BACKUP_DIR/*.sql

# Xóa backups cũ hơn 7 ngày
find $BACKUP_DIR -name "backup_*.tar.gz" -mtime +7 -delete

echo "Backup completed: backup_$DATE.tar.gz"
```

Setup cron để backup hàng ngày:
```bash
chmod +x backup-databases.sh
crontab -e
# Thêm: 0 2 * * * /opt/microservice/backup-databases.sh
```

### 2. Restore Database

```bash
# Extract backup
tar -xzf backup_20240101_020000.tar.gz

# Restore
psql -h 47.130.33.106 -U postgres -d userservice_db < userservice_db_20240101_020000.sql
```

### 3. Backup Docker Volumes (nếu có)

```bash
docker run --rm -v microservice_data:/data -v $(pwd):/backup ubuntu tar czf /backup/volume_backup.tar.gz /data
```

---

## 🔧 Troubleshooting

### Services Không Khởi Động

```bash
# Kiểm tra logs
docker-compose -f docker-compose.production.yml logs user-service

# Kiểm tra container status
docker ps -a

# Restart service
docker-compose -f docker-compose.production.yml restart user-service
```

### Lỗi Kết Nối Database

```bash
# Test kết nối từ server
psql -h 47.130.33.106 -U postgres -d userservice_db

# Kiểm tra firewall
sudo ufw status
sudo ufw allow 5432/tcp  # Nếu cần
```

### Nginx 502 Bad Gateway

```bash
# Kiểm tra services đang chạy
docker ps

# Kiểm tra Nginx error logs
sudo tail -f /var/log/nginx/error.log

# Test proxy
curl http://localhost:5000
```

### Port Đã Được Sử Dụng

```bash
# Tìm process đang dùng port
sudo lsof -i :5000
sudo netstat -tulpn | grep :5000

# Kill process
sudo kill -9 <PID>
```

### Out of Memory

```bash
# Kiểm tra memory usage
free -h
docker stats

# Giới hạn memory cho containers trong docker-compose.yml
services:
  user-service:
    deploy:
      resources:
        limits:
          memory: 512M
```

---

## 📝 Checklist Triển Khai

Trước khi go-live, đảm bảo:

- [ ] Tất cả services đang chạy và healthy
- [ ] Database connections hoạt động
- [ ] RabbitMQ connection OK
- [ ] MongoDB connection OK
- [ ] Nginx reverse proxy cấu hình đúng
- [ ] SSL certificate đã được cài đặt
- [ ] Frontend build và deploy thành công
- [ ] Health checks đang chạy
- [ ] Backup script đã setup
- [ ] Firewall rules đã cấu hình
- [ ] Monitoring đang hoạt động
- [ ] Test tất cả API endpoints
- [ ] Test từ frontend

---

## 🎯 Tips và Best Practices

1. **Sử dụng Environment Variables:** Không hardcode passwords trong code, dùng environment variables hoặc secrets management.

2. **Resource Limits:** Đặt limits cho Docker containers để tránh một service chiếm hết resources.

3. **Health Checks:** Implement health check endpoints cho tất cả services.

4. **Logging:** Centralize logs, có thể dùng ELK stack hoặc đơn giản hơn là file logs với rotation.

5. **Monitoring:** Setup monitoring tools như Prometheus + Grafana nếu có thời gian.

6. **Security:**
   - Đổi default passwords
   - Sử dụng firewall (ufw)
   - Chỉ mở ports cần thiết
   - Regular updates

7. **Performance:**
   - Enable gzip compression trong Nginx
   - Cache static assets
   - Optimize database queries

---

## 📚 Tài Liệu Tham Khảo

- [Docker Documentation](https://docs.docker.com/)
- [Nginx Documentation](https://nginx.org/en/docs/)
- [Let's Encrypt](https://letsencrypt.org/)
- [.NET Production Best Practices](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/)

---

**Lưu ý:** Tài liệu này dựa trên kinh nghiệm triển khai thực tế. Tùy vào môi trường cụ thể, có thể cần điều chỉnh một số bước. Nếu gặp vấn đề, check logs và Google là cách tốt nhất để debug! 😊

