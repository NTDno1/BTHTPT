# 🚀 Hướng Dẫn Triển Khai Dự Án Microservice

Tài liệu này hướng dẫn cách triển khai hệ thống Microservice E-Commerce lên môi trường production. Dự án sử dụng kiến trúc microservice với Docker Compose, RabbitMQ cho load balancing, và API Gateway RabbitMQ làm entry point chính.

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
- **Docker** và **Docker Compose** (bắt buộc)
- **Nginx** (cho reverse proxy và serve frontend)
- **Git** (để clone repository)
- **PostgreSQL Client** (để test kết nối database)

### External Services Cần Có:
- **PostgreSQL Server** (đã setup sẵn tại `47.130.33.106:5432`)
- **MongoDB Atlas** (hoặc MongoDB server khác)
- **RabbitMQ Server** (đã setup sẵn tại `47.130.33.106:5672`)

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
newgrp docker  # Hoặc logout/login lại

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

# Kiểm tra
sudo systemctl status nginx
```

### 3. Clone Repository

```bash
cd /opt
sudo git clone <your-repo-url> microservice
cd microservice
sudo chown -R $USER:$USER /opt/microservice
```

Hoặc upload code lên server bằng FileZilla, WinSCP, etc.

### 4. Chuẩn Bị Database

Đảm bảo các databases đã được tạo trên PostgreSQL server (`47.130.33.106`):

```sql
-- Kết nối đến PostgreSQL server
psql -h 47.130.33.106 -U postgres

-- Tạo databases
CREATE DATABASE userservice_db;
CREATE DATABASE productservice_db;
CREATE DATABASE orderservice_db;

-- Kiểm tra
\l
```

Kiểm tra kết nối từ server:
```bash
psql -h 47.130.33.106 -U postgres -d userservice_db
```

### 5. Kiểm Tra RabbitMQ

```bash
# Test kết nối RabbitMQ
telnet 47.130.33.106 5672

# Hoặc dùng curl (nếu có management plugin)
curl -u guest:guest http://47.130.33.106:15672/api/overview
```

---

## 🚀 Triển Khai Backend Services

### Cách 1: Sử Dụng Docker Compose (Khuyến nghị)

Dự án đã có sẵn file `docker-compose.yml` với cấu hình đầy đủ. Chỉ cần chỉnh sửa một số thông tin:

#### Bước 1: Cập Nhật Thông Tin Kết Nối

Mở file `docker-compose.yml` và cập nhật các thông tin sau:

```yaml
# Thay đổi các giá trị này:
- ConnectionStrings__PostgreSQL=Host=47.130.33.106;Port=5432;Database=userservice_db;Username=postgres;Password=YOUR_PASSWORD
- MongoDb__ConnectionString=YOUR_MONGODB_CONNECTION_STRING
- RabbitMQ__HostName=47.130.33.106
- RabbitMQ__Password=YOUR_RABBITMQ_PASSWORD
```

**Lưu ý:** 
- Thay `YOUR_PASSWORD` bằng password thực tế của PostgreSQL
- Thay `YOUR_MONGODB_CONNECTION_STRING` bằng connection string MongoDB Atlas
- Thay `YOUR_RABBITMQ_PASSWORD` nếu RabbitMQ không dùng `guest/guest`

#### Bước 2: Chuyển Sang Production Mode

Để chạy production, thay đổi `ASPNETCORE_ENVIRONMENT` từ `Development` sang `Production`:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production  # Thay đổi từ Development
```

Hoặc tạo file `docker-compose.production.yml`:

```yaml
version: '3.8'

services:
  user-service-1:
    build:
      context: .
      dockerfile: Microservice.Services.UserService/Dockerfile
    container_name: microservice-user-service-1-prod
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
    restart: unless-stopped

  # ... các services khác tương tự
```

#### Bước 3: Build và Chạy Services

```bash
cd /opt/microservice

# Build images (lần đầu hoặc khi có thay đổi code)
docker-compose build

# Chạy tất cả services
docker-compose up -d

# Kiểm tra status
docker-compose ps

# Xem logs
docker-compose logs -f

# Xem logs một service cụ thể
docker-compose logs -f api-gateway-rabbitmq
```

#### Bước 4: Kiểm Tra Services

```bash
# Test API Gateway (entry point chính)
curl http://localhost:5010/health

# Test User Service instances
curl http://localhost:5001/health
curl http://localhost:5004/health

# Test Product Service instances
curl http://localhost:5002/health
curl http://localhost:5006/health

# Test Order Service instances
curl http://localhost:5003/health
curl http://localhost:5007/health

# Test API qua Gateway
curl http://localhost:5010/api/users
curl http://localhost:5010/api/products
```

#### Kiến Trúc Load Balancing

Dự án sử dụng **RabbitMQ Load Balancing** tự động:
- Mỗi service có **2 instances** chạy song song
- RabbitMQ tự động phân phối requests đến các instances
- API Gateway RabbitMQ (`5010`) là entry point duy nhất
- Không cần Nginx load balancer cho services

**Port Mapping:**
- API Gateway RabbitMQ: `5010` (PRIMARY)
- User Service: `5001`, `5004`
- Product Service: `5002`, `5006`
- Order Service: `5003`, `5007`
- Frontend: `4200`

### Cách 2: Chạy Trực Tiếp (Không Docker) - Không Khuyến Nghị

Nếu không muốn dùng Docker, có thể publish và chạy trực tiếp:

```bash
# Publish từng service
cd Microservice.Services.UserService
dotnet publish -c Release -o /opt/microservice/user-service

cd ../Microservice.Services.ProductService
dotnet publish -c Release -o /opt/microservice/product-service

cd ../Microservice.Services.OrderService
dotnet publish -c Release -o /opt/microservice/order-service

cd ../Microservice.ApiGateway.RabbitMQ
dotnet publish -c Release -o /opt/microservice/api-gateway
```

Tạo systemd service files để tự động khởi động:

**`/etc/systemd/system/user-service-1.service`:**
```ini
[Unit]
Description=User Service Instance 1
After=network.target

[Service]
Type=simple
WorkingDirectory=/opt/microservice/user-service
ExecStart=/usr/bin/dotnet /opt/microservice/user-service/Microservice.Services.UserService.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5001
Environment=ConnectionStrings__PostgreSQL=Host=47.130.33.106;Port=5432;Database=userservice_db;Username=postgres;Password=YOUR_PASSWORD

[Install]
WantedBy=multi-user.target
```

Sau đó:
```bash
sudo systemctl daemon-reload
sudo systemctl enable user-service-1
sudo systemctl start user-service-1
sudo systemctl status user-service-1
```

---

## 🌐 Cấu Hình Nginx Reverse Proxy

Nginx sẽ đóng vai trò reverse proxy cho API Gateway và serve static files cho Frontend.

### Tạo Nginx Config

Tạo file `/etc/nginx/sites-available/microservice`:

```nginx
# API Gateway - Entry point chính
server {
    listen 80;
    server_name api.yourdomain.com; # Thay bằng domain của bạn hoặc IP

    location / {
        proxy_pass http://localhost:5010;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        
        # Timeout settings
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # Health check endpoint
    location /health {
        proxy_pass http://localhost:5010/health;
        access_log off;
    }
}

# Frontend - Serve Angular app
server {
    listen 80;
    server_name yourdomain.com www.yourdomain.com; # Thay bằng domain của bạn

    root /opt/microservice/Frontend/dist/microservice-frontend/browser;
    index index.html;

    # Gzip compression
    gzip on;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # Cache static assets
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # API proxy (nếu frontend gọi API qua cùng domain)
    location /api {
        proxy_pass http://localhost:5010;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Enable Site

```bash
# Tạo symbolic link
sudo ln -s /etc/nginx/sites-available/microservice /etc/nginx/sites-enabled/

# Xóa default site (optional)
sudo rm /etc/nginx/sites-enabled/default

# Kiểm tra config
sudo nginx -t

# Reload Nginx
sudo systemctl reload nginx
```

### Kiểm Tra

```bash
# Test Nginx config
sudo nginx -t

# Test API Gateway qua Nginx
curl http://api.yourdomain.com/health

# Test Frontend
curl http://yourdomain.com
```

---

## 🔒 Setup SSL/HTTPS

Sử dụng Let's Encrypt để có SSL miễn phí:

### Cài Đặt Certbot

```bash
sudo apt-get update
sudo apt-get install certbot python3-certbot-nginx -y
```

### Lấy SSL Certificate

```bash
# Lấy certificate cho API domain
sudo certbot --nginx -d api.yourdomain.com

# Lấy certificate cho Frontend domain
sudo certbot --nginx -d yourdomain.com -d www.yourdomain.com
```

Certbot sẽ tự động:
- Tạo SSL certificate
- Cập nhật Nginx config để sử dụng HTTPS
- Setup auto-renewal

### Auto Renewal

Let's Encrypt certificates hết hạn sau 90 ngày. Certbot tự động setup cron job để renew:

```bash
# Test renewal (dry-run)
sudo certbot renew --dry-run

# Kiểm tra cron job
sudo systemctl status certbot.timer
```

---

## 🎨 Triển Khai Frontend

### Cách 1: Sử Dụng Docker (Khuyến nghị)

Frontend đã được Dockerized với multi-stage build:

```bash
cd /opt/microservice

# Build và chạy frontend container
docker-compose up -d frontend

# Kiểm tra
curl http://localhost:4200
```

**Cấu hình API URL:**

Frontend sử dụng file `Frontend/src/app/config/environment.ts` để cấu hình API URL. Khi build, Docker sẽ inject API URL từ environment variable:

```yaml
# Trong docker-compose.yml
frontend:
  environment:
    - API_URL=http://103.82.26.211:5010/api  # Thay bằng domain/IP thực tế
```

**Lưu ý:** 
- Nếu dùng domain với HTTPS, đổi thành: `https://api.yourdomain.com/api`
- File `docker-entrypoint.sh` sẽ tự động thay thế API URL trong built files

### Cách 2: Build và Deploy Static Files

Nếu muốn serve frontend bằng Nginx trực tiếp:

```bash
cd /opt/microservice/Frontend

# Install dependencies
npm install

# Build production
npm run build -- --configuration production

# Output sẽ ở thư mục dist/microservice-frontend/browser/
```

**Cập nhật API URL trong environment.ts:**

Mở `Frontend/src/app/config/environment.ts` và cập nhật:

```typescript
export const environment = {
  apiGatewayUrl: 'https://api.yourdomain.com',  // Thay bằng domain thực tế
  apiGatewayApiUrl: 'https://api.yourdomain.com/api',
  // ... các URL khác
};
```

Sau đó build lại:
```bash
npm run build -- --configuration production
```

**Copy files lên Nginx:**

```bash
# Copy built files
sudo cp -r dist/microservice-frontend/browser/* /var/www/html/

# Hoặc cấu hình Nginx trỏ đến thư mục build
# (đã cấu hình trong phần Nginx ở trên)
```

### Cách 3: Chạy Frontend với PM2 (Không khuyến nghị cho production)

Chỉ dùng cho development:

```bash
npm install -g pm2
cd Frontend
pm2 start "npm start" --name frontend
pm2 save
pm2 startup
```

---

## 📊 Monitoring và Logging

### Xem Logs Docker

```bash
# Logs tất cả services
docker-compose logs -f

# Logs một service cụ thể
docker-compose logs -f api-gateway-rabbitmq
docker-compose logs -f user-service-1

# Logs với timestamp
docker-compose logs -f --timestamps

# Logs 100 dòng cuối
docker-compose logs --tail=100 user-service-1
```

### Monitoring với Docker Stats

```bash
# Xem resource usage của tất cả containers
docker stats

# Xem một container cụ thể
docker stats microservice-api-gateway-rabbitmq

# Xem với format tùy chỉnh
docker stats --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}"
```

### Setup Log Rotation

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

### Health Check Script

Tạo script kiểm tra health của services:

**`/opt/microservice/scripts/health-check.sh`:**

```bash
#!/bin/bash
# health-check.sh

services=(
    "http://localhost:5010/health:API Gateway"
    "http://localhost:5001/health:User Service 1"
    "http://localhost:5004/health:User Service 2"
    "http://localhost:5002/health:Product Service 1"
    "http://localhost:5006/health:Product Service 2"
    "http://localhost:5003/health:Order Service 1"
    "http://localhost:5007/health:Order Service 2"
)

echo "=== Health Check - $(date) ==="

for service in "${services[@]}"; do
    IFS=':' read -r url name <<< "$service"
    if curl -f -s -o /dev/null -w "%{http_code}" "$url" | grep -q "200"; then
        echo "✅ $name is UP"
    else
        echo "❌ $name is DOWN"
        # Có thể gửi email hoặc notification ở đây
    fi
done

echo "================================"
```

Chạy định kỳ với cron:
```bash
chmod +x /opt/microservice/scripts/health-check.sh

# Thêm vào crontab
crontab -e
# Thêm dòng: */5 * * * * /opt/microservice/scripts/health-check.sh >> /var/log/health-check.log 2>&1
```

### Monitoring MongoDB và RabbitMQ

```bash
# Test MongoDB connection
mongosh "YOUR_MONGODB_CONNECTION_STRING"

# Test RabbitMQ (nếu có management plugin)
curl -u guest:guest http://47.130.33.106:15672/api/overview
```

---

## 💾 Backup và Recovery

### Backup PostgreSQL Databases

Tạo script backup:

**`/opt/microservice/scripts/backup-databases.sh`:**

```bash
#!/bin/bash
# backup-databases.sh

BACKUP_DIR="/opt/backups"
DATE=$(date +%Y%m%d_%H%M%S)
PGHOST="47.130.33.106"
PGUSER="postgres"
PGPASSWORD="YOUR_PASSWORD"  # Thay bằng password thực tế

export PGPASSWORD

mkdir -p $BACKUP_DIR

echo "Starting backup at $(date)"

# Backup từng database
pg_dump -h $PGHOST -U $PGUSER userservice_db > $BACKUP_DIR/userservice_db_$DATE.sql
pg_dump -h $PGHOST -U $PGUSER productservice_db > $BACKUP_DIR/productservice_db_$DATE.sql
pg_dump -h $PGHOST -U $PGUSER orderservice_db > $BACKUP_DIR/orderservice_db_$DATE.sql

# Compress
tar -czf $BACKUP_DIR/backup_$DATE.tar.gz $BACKUP_DIR/*.sql
rm $BACKUP_DIR/*.sql

# Xóa backups cũ hơn 7 ngày
find $BACKUP_DIR -name "backup_*.tar.gz" -mtime +7 -delete

echo "Backup completed: backup_$DATE.tar.gz"
unset PGPASSWORD
```

Setup cron để backup hàng ngày:
```bash
chmod +x /opt/microservice/scripts/backup-databases.sh

# Thêm vào crontab (backup lúc 2h sáng mỗi ngày)
crontab -e
# Thêm: 0 2 * * * /opt/microservice/scripts/backup-databases.sh >> /var/log/backup.log 2>&1
```

### Restore Database

```bash
# Extract backup
cd /opt/backups
tar -xzf backup_20240101_020000.tar.gz

# Restore từng database
export PGPASSWORD=YOUR_PASSWORD
psql -h 47.130.33.106 -U postgres -d userservice_db < userservice_db_20240101_020000.sql
psql -h 47.130.33.106 -U postgres -d productservice_db < productservice_db_20240101_020000.sql
psql -h 47.130.33.106 -U postgres -d orderservice_db < orderservice_db_20240101_020000.sql
unset PGPASSWORD
```

### Backup Docker Images

```bash
# Save images
docker save microservice-user-service-1:latest | gzip > user-service-image.tar.gz
docker save microservice-api-gateway-rabbitmq:latest | gzip > api-gateway-image.tar.gz

# Load images (khi restore)
docker load < user-service-image.tar.gz
```

---

## 🔧 Troubleshooting

### Services Không Khởi Động

```bash
# Kiểm tra logs
docker-compose logs user-service-1

# Kiểm tra container status
docker ps -a

# Kiểm tra logs chi tiết
docker logs microservice-user-service-1

# Restart service
docker-compose restart user-service-1

# Rebuild và restart
docker-compose up -d --build user-service-1
```

### Lỗi Kết Nối Database

```bash
# Test kết nối từ server
psql -h 47.130.33.106 -U postgres -d userservice_db

# Kiểm tra firewall
sudo ufw status
sudo ufw allow 5432/tcp  # Nếu cần (chỉ cho phép từ IP cụ thể)

# Kiểm tra PostgreSQL đang listen
sudo netstat -tulpn | grep 5432
```

### Nginx 502 Bad Gateway

```bash
# Kiểm tra services đang chạy
docker ps

# Kiểm tra Nginx error logs
sudo tail -f /var/log/nginx/error.log

# Test proxy
curl http://localhost:5010/health

# Kiểm tra Nginx config
sudo nginx -t
```

### Port Đã Được Sử Dụng

```bash
# Tìm process đang dùng port
sudo lsof -i :5010
sudo netstat -tulpn | grep :5010

# Kill process
sudo kill -9 <PID>

# Hoặc đổi port trong docker-compose.yml
```

### Out of Memory

```bash
# Kiểm tra memory usage
free -h
docker stats

# Giới hạn memory cho containers trong docker-compose.yml
services:
  user-service-1:
    deploy:
      resources:
        limits:
          memory: 512M
        reservations:
          memory: 256M
```

### RabbitMQ Connection Issues

```bash
# Test kết nối RabbitMQ
telnet 47.130.33.106 5672

# Kiểm tra RabbitMQ đang chạy
curl -u guest:guest http://47.130.33.106:15672/api/overview

# Xem logs của services có dùng RabbitMQ
docker-compose logs order-service-1 | grep -i rabbitmq
```

### Frontend Không Kết Nối Được API

```bash
# Kiểm tra API Gateway đang chạy
curl http://localhost:5010/health

# Kiểm tra CORS settings trong API Gateway
docker-compose logs api-gateway-rabbitmq | grep -i cors

# Kiểm tra API URL trong frontend container
docker exec microservice-frontend cat /usr/share/nginx/html/main*.js | grep -i "api"
```

### Database Schema Issues

Nếu gặp lỗi về missing tables/columns, services sẽ tự động tạo khi khởi động. Hoặc chạy thủ công:

```bash
# User Service - Tạo UserAddresses table
psql -h 47.130.33.106 -U postgres -d userservice_db -f Microservice.Services.UserService/create-useraddresses-table.sql

# User Service - Thêm missing columns
psql -h 47.130.33.106 -U postgres -d userservice_db -f Microservice.Services.UserService/add-missing-columns.sql

# Order Service - Tạo tables
psql -h 47.130.33.106 -U postgres -d orderservice_db -f Microservice.Services.OrderService/create-order-tables.sql
```

---

## ✅ Checklist Triển Khai

Trước khi go-live, đảm bảo:

- [ ] Tất cả services đang chạy và healthy (`docker-compose ps`)
- [ ] Database connections hoạt động (test từng service)
- [ ] RabbitMQ connection OK (test từ Order Service)
- [ ] MongoDB connection OK (check logs)
- [ ] API Gateway RabbitMQ đang chạy trên port 5010
- [ ] Nginx reverse proxy cấu hình đúng
- [ ] SSL certificate đã được cài đặt (nếu dùng domain)
- [ ] Frontend build và deploy thành công
- [ ] Frontend có thể gọi API qua API Gateway
- [ ] Health checks đang chạy
- [ ] Backup script đã setup
- [ ] Firewall rules đã cấu hình (chỉ mở ports cần thiết)
- [ ] Test tất cả API endpoints
- [ ] Test authentication (login/register)
- [ ] Test từ frontend (tạo user, product, order)
- [ ] Load balancing hoạt động (2 instances mỗi service)

---

## 💡 Tips và Best Practices

### Security

1. **Environment Variables:** Không hardcode passwords trong code, dùng environment variables
2. **Firewall:** Chỉ mở ports cần thiết:
   ```bash
   sudo ufw allow 80/tcp
   sudo ufw allow 443/tcp
   sudo ufw allow 22/tcp  # SSH
   sudo ufw enable
   ```
3. **Passwords:** Đổi default passwords cho PostgreSQL, RabbitMQ
4. **JWT Secret:** Đổi JWT secret key trong `appsettings.json`
5. **HTTPS:** Luôn dùng HTTPS cho production

### Performance

1. **Resource Limits:** Đặt limits cho Docker containers
2. **Gzip Compression:** Enable trong Nginx
3. **Cache Static Assets:** Configure trong Nginx
4. **Database Indexing:** Đảm bảo indexes được tạo
5. **Connection Pooling:** Cấu hình trong connection strings

### Monitoring

1. **Health Checks:** Implement và monitor health endpoints
2. **Logging:** Centralize logs, setup rotation
3. **Alerts:** Setup alerts cho service downtime
4. **Metrics:** Track response times, error rates

### Backup

1. **Automated Backups:** Setup cron jobs
2. **Test Restores:** Định kỳ test restore process
3. **Offsite Backups:** Lưu backups ở nơi khác
4. **Backup Retention:** Giữ backups theo policy

---

## 📚 Tài Liệu Tham Khảo

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Nginx Documentation](https://nginx.org/en/docs/)
- [Let's Encrypt](https://letsencrypt.org/)
- [.NET Production Best Practices](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/production-best-practices)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

---

## 🆘 Hỗ Trợ

Nếu gặp vấn đề, kiểm tra:
1. Logs của services: `docker-compose logs -f <service-name>`
2. Health endpoints: `curl http://localhost:<port>/health`
3. Network connectivity: `docker network inspect microservice-network`
4. Container status: `docker ps -a`

---

**Chúc bạn triển khai thành công! 🎉**
