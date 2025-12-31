# 🔄 Load Balancing Configuration với RabbitMQ

## 📋 Tổng Quan

Hệ thống đã được cấu hình để chạy **User Service** với **2 instances** và tự động chia tải (load balancing) giữa chúng thông qua **RabbitMQ**.

**Lưu ý:** Hệ thống sử dụng **API Gateway RabbitMQ** (không dùng Ocelot Gateway).

## 🏗️ Kiến Trúc

```
                    ┌──────────────────────┐
                    │  API Gateway RabbitMQ│
                    │   Port: 5010         │
                    └──────────┬───────────┘
                               │
                               │ Publish to Queue
                               ▼
                    ┌──────────────────────┐
                    │   RabbitMQ Queue     │
                    │  api.user.request    │
                    └──────────┬───────────┘
                               │
                    ┌──────────┴───────────┐
                    │  Auto Load Balance   │
                    │  (Round Robin)       │
                    │                      │
         ┌──────────▼──────────┐  ┌────────▼──────────┐
         │  User Service 1     │  │  User Service 2  │
         │  Port: 5001         │  │  Port: 5004      │
         │  Container:         │  │  Container:      │
         │  user-service-1     │  │  user-service-2 │
         │  (Consumer)         │  │  (Consumer)     │
         └──────────┬──────────┘  └────────┬─────────┘
                    │                      │
                    │  Consume from Queue  │
                    │  (Auto distributed)  │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │   PostgreSQL        │
                    │  userservice_db     │
                    └──────────────────────┘
```

## 🔧 Cấu Hình

### 1. Docker Compose

**2 Instances của User Service:**
- **user-service-1:** Port 5001 (exposed)
- **user-service-2:** Port 5004 (exposed)

**Load Balancer Options:**
- **Ocelot Gateway:** Sử dụng Round Robin (mặc định)
- **Nginx Load Balancer:** Optional, port 5005

### 2. RabbitMQ Load Balancing

**Cơ chế tự động:** RabbitMQ tự động phân phối messages giữa các consumers theo **Round Robin**.

**Queue Configuration:**
- Queue: `api.user.request`
- Response Queue: `api.user.response`
- Durable: `true` (queue tồn tại khi RabbitMQ restart)

**Cách hoạt động:**
1. API Gateway RabbitMQ publish request vào queue `api.user.request`
2. Cả 2 instances của User Service đều consume từ cùng queue này
3. RabbitMQ tự động phân phối messages theo round-robin:
   - Message 1 → User Service 1
   - Message 2 → User Service 2
   - Message 3 → User Service 1
   - Message 4 → User Service 2
   - ...

**Configuration File:** `Microservice.ApiGateway.RabbitMQ/appsettings.json`

```json
{
  "ServiceRoutes": {
    "UserService": {
      "Queue": "api.user.request",
      "ResponseQueue": "api.user.response",
      "RoutePrefix": "users"
    }
  }
}
```

### 3. Nginx Load Balancer (Optional)

File: `nginx-lb/user-service-lb.conf`

**Features:**
- Least Connections algorithm
- Health checks với max_fails và fail_timeout
- Automatic failover
- Retry mechanism

## 🚀 Cách Sử Dụng

### Start Services

```bash
# Start tất cả services (bao gồm 2 instances của User Service)
docker-compose up -d

# Kiểm tra status
docker-compose ps

# Xem logs của từng instance
docker logs microservice-user-service-1
docker logs microservice-user-service-2
```

### Test Load Balancing

```bash
# Test qua API Gateway RabbitMQ (sẽ tự động load balance qua RabbitMQ)
curl http://localhost:5010/api/users

# Test trực tiếp từng instance (HTTP - không qua RabbitMQ)
curl http://localhost:5001/api/users
curl http://localhost:5004/api/users

# Test qua Nginx Load Balancer (nếu sử dụng - HTTP direct)
curl http://localhost:5005/api/users
```

### Monitor Load Distribution

```bash
# Xem logs của cả 2 instances để thấy requests được phân phối
docker logs -f microservice-user-service-1 &
docker logs -f microservice-user-service-2 &
```

## 📊 Load Balancing Methods

### Round Robin (Mặc định)
- Phân phối requests theo vòng tròn: Request 1 → Server 1, Request 2 → Server 2, Request 3 → Server 1, ...
- **Ưu điểm:** Đơn giản, công bằng
- **Nhược điểm:** Không tính đến tải hiện tại của server

### Least Connections
- Phân phối đến server có ít connections nhất
- **Ưu điểm:** Tối ưu khi các requests có thời gian xử lý khác nhau
- **Nhược điểm:** Cần tracking connections

## 🔍 Health Checks

### RabbitMQ Health Checks

RabbitMQ tự động quản lý consumers:
- Nếu một consumer disconnect, messages sẽ được chuyển sang consumer còn lại
- Nếu consumer không acknowledge message, message sẽ được redeliver
- Nếu tất cả consumers down, messages sẽ được queue lại

**Consumer Acknowledgment:**
- Messages chỉ được xóa khỏi queue sau khi consumer acknowledge
- Nếu consumer crash, message sẽ được redeliver đến consumer khác

### Nginx Health Checks (Optional - cho HTTP direct access)

Nginx sử dụng `max_fails` và `fail_timeout`:
- `max_fails=3`: Sau 3 lần fail, server được đánh dấu là down
- `fail_timeout=30s`: Sau 30 giây, server được thử lại

## 🎯 Use Cases

### Khi Nào Cần Load Balancing?

1. **High Traffic:** Khi một instance không đủ xử lý
2. **High Availability:** Khi cần failover tự động
3. **Zero Downtime:** Khi cần update một instance mà không downtime
4. **Performance:** Khi cần giảm response time

### Khi Nào Không Cần?

1. **Low Traffic:** Một instance đủ xử lý
2. **Stateful Services:** Services có state (cần sticky sessions)
3. **Resource Constraints:** Không đủ resources cho nhiều instances

## 🔧 Tùy Chỉnh

### Thêm Instance Thứ 3

Thêm vào `docker-compose.yml`:

```yaml
user-service-3:
  build:
    context: .
    dockerfile: Microservice.Services.UserService/Dockerfile
  container_name: microservice-user-service-3
  ports:
    - "5006:8080"
  # ... same environment as others
  # Đảm bảo service này cũng consume từ queue "api.user.request"
```

**Với RabbitMQ:** Không cần cấu hình thêm! Chỉ cần đảm bảo instance mới consume từ cùng queue `api.user.request`, RabbitMQ sẽ tự động phân phối messages cho nó.

### Thay Đổi Load Balancing Method

**Với RabbitMQ:** Load balancing method được quyết định bởi RabbitMQ consumer prefetch và acknowledgment:

**Round Robin (Mặc định):**
- RabbitMQ phân phối messages theo vòng tròn
- Mỗi consumer nhận 1 message tại một thời điểm (prefetch=1)

**Fair Dispatch (Có thể cấu hình):**
- Set `prefetch=1` trong consumer để đảm bảo fair distribution
- Consumer chỉ nhận message mới sau khi acknowledge message cũ

**Cấu hình trong User Service Consumer:**
```csharp
channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
```

## 📈 Monitoring

### Xem Metrics

```bash
# CPU và Memory usage
docker stats microservice-user-service-1 microservice-user-service-2

# Request count per instance
docker logs microservice-user-service-1 | grep "GET\|POST" | wc -l
docker logs microservice-user-service-2 | grep "GET\|POST" | wc -l
```

### Logs Analysis

```bash
# Xem logs của cả 2 instances
docker-compose logs user-service-1 user-service-2

# Follow logs
docker-compose logs -f user-service-1 user-service-2
```

## ⚠️ Lưu Ý

1. **Database:** Cả 2 instances dùng chung database → cần đảm bảo không có race conditions
2. **Sessions:** Nếu có sessions, cần sticky sessions hoặc shared session store
3. **Caching:** Nếu có cache, cần shared cache (Redis) hoặc cache invalidation
4. **File Uploads:** Nếu có file uploads, cần shared storage

## 🔐 Best Practices

1. **Health Checks:** Luôn enable health checks
2. **Graceful Shutdown:** Đảm bảo services shutdown gracefully
3. **Monitoring:** Monitor cả 2 instances
4. **Logging:** Centralized logging để dễ debug
5. **Configuration:** Dùng environment variables cho configuration

## 📚 Thêm Thông Tin

- [Ocelot Documentation](https://ocelot.readthedocs.io/)
- [Nginx Load Balancing](https://nginx.org/en/docs/http/load_balancing.html)
- [Docker Compose Documentation](https://docs.docker.com/compose/)

