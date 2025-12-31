# 🔄 RabbitMQ Load Balancing - Hướng Dẫn Chi Tiết

## 📋 Tổng Quan

Hệ thống sử dụng **API Gateway RabbitMQ** làm entry point duy nhất. Load balancing được thực hiện tự động bởi **RabbitMQ** khi nhiều consumers consume từ cùng một queue.

## 🎯 Kiến Trúc Load Balancing với RabbitMQ

### Flow Diagram

```
Client Request
    │
    ▼
┌─────────────────────────┐
│  API Gateway RabbitMQ   │
│      Port: 5010         │
└───────────┬─────────────┘
            │
            │ Publish to Queue
            │ (api.user.request)
            ▼
┌─────────────────────────┐
│    RabbitMQ Queue       │
│  api.user.request       │
│  (Durable Queue)        │
└───────────┬─────────────┘
            │
            │ Auto Distribution
            │ (Round Robin)
    ┌───────┴───────┐
    │               │
    ▼               ▼
┌─────────┐    ┌─────────┐
│User Svc1│    │User Svc2│
│Port:5001│    │Port:5004│
│Consumer │    │Consumer │
└────┬────┘    └────┬────┘
     │              │
     │ Process &    │ Process &
     │ Acknowledge  │ Acknowledge
     │              │
     └──────┬───────┘
            │
            │ Publish Response
            │ (api.user.response)
            ▼
┌─────────────────────────┐
│  API Gateway RabbitMQ   │
│   (Response Handler)    │
└───────────┬─────────────┘
            │
            ▼
      Client Response
```

## 🔧 Cấu Hình

### 1. API Gateway RabbitMQ Configuration

**File:** `Microservice.ApiGateway.RabbitMQ/appsettings.json`

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

### 2. Docker Compose

**2 Instances của User Service:**
- `user-service-1`: Port 5001
- `user-service-2`: Port 5004

**Cả 2 instances đều consume từ queue:** `api.user.request`

### 3. RabbitMQ Queue Properties

- **Durable:** `true` - Queue tồn tại khi RabbitMQ restart
- **Auto-delete:** `false` - Queue không tự động xóa
- **Exclusive:** `false` - Nhiều consumers có thể connect

## 🚀 Cách Hoạt Động

### Round Robin Distribution

RabbitMQ tự động phân phối messages theo round-robin:

1. **Request 1** → Queue → **User Service 1** (consume)
2. **Request 2** → Queue → **User Service 2** (consume)
3. **Request 3** → Queue → **User Service 1** (consume)
4. **Request 4** → Queue → **User Service 2** (consume)
5. ...

### Message Acknowledgment

- Consumer phải **acknowledge** message sau khi xử lý xong
- Nếu consumer crash trước khi acknowledge, message sẽ được **redeliver** đến consumer khác
- Đảm bảo không mất messages

### Fair Dispatch

Để đảm bảo fair distribution, set `prefetch=1`:

```csharp
channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
```

Điều này đảm bảo:
- Mỗi consumer chỉ nhận 1 message tại một thời điểm
- Consumer chỉ nhận message mới sau khi acknowledge message cũ
- Messages được phân phối đều giữa các consumers

## 📊 Ưu Điểm của RabbitMQ Load Balancing

### 1. Tự Động
- Không cần cấu hình thêm
- RabbitMQ tự động phân phối messages

### 2. Fault Tolerant
- Nếu một consumer down, messages tự động chuyển sang consumer khác
- Messages được queue lại nếu tất cả consumers down

### 3. Scalable
- Dễ dàng thêm/bớt consumers
- Không cần restart gateway

### 4. Decoupled
- Gateway không cần biết có bao nhiêu consumers
- Consumers có thể start/stop độc lập

## 🔍 Monitoring

### Xem Queue Status

Truy cập RabbitMQ Management UI: `http://47.130.33.106:15672`

**Queue: `api.user.request`**
- **Messages:** Số messages đang chờ
- **Consumers:** Số consumers đang active (sẽ là 2)
- **Message Rate:** Tốc độ messages được consume

### Xem Consumer Status

Trong RabbitMQ Management UI:
- Vào tab **Queues** → Chọn queue `api.user.request`
- Xem tab **Consumers** để thấy các consumers đang connect

### Logs

```bash
# Xem logs của cả 2 instances
docker logs -f microservice-user-service-1
docker logs -f microservice-user-service-2

# Xem logs của API Gateway
docker logs -f microservice-api-gateway-rabbitmq
```

## 🧪 Testing

### Test Load Balancing

```bash
# Gửi nhiều requests và xem logs
for i in {1..10}; do
  curl http://localhost:5010/api/users
  sleep 1
done

# Xem logs của cả 2 instances để thấy requests được phân phối
docker logs microservice-user-service-1 | grep "GET\|POST"
docker logs microservice-user-service-2 | grep "GET\|POST"
```

### Test Fault Tolerance

```bash
# Stop một instance
docker stop microservice-user-service-1

# Gửi requests - tất cả sẽ đi đến instance 2
curl http://localhost:5010/api/users

# Start lại instance 1
docker start microservice-user-service-1

# Requests sẽ được phân phối lại giữa cả 2
```

## ⚙️ Tùy Chỉnh

### Thêm Instance Thứ 3

1. Thêm vào `docker-compose.yml`:
```yaml
user-service-3:
  # ... same config as others
```

2. **Không cần cấu hình thêm!** RabbitMQ tự động phân phối messages cho instance mới.

### Thay Đổi Prefetch Count

Trong User Service consumer code:
```csharp
// Cho phép consumer nhận nhiều messages cùng lúc
channel.BasicQos(prefetchSize: 0, prefetchCount: 5, global: false);
```

**Lưu ý:** Prefetch cao hơn có thể dẫn đến load không đều nếu processing time khác nhau.

## ⚠️ Lưu Ý Quan Trọng

### 1. Message Ordering
- RabbitMQ đảm bảo message order **trong một queue**
- Nhưng với nhiều consumers, messages có thể được xử lý out-of-order
- Nếu cần strict ordering, chỉ dùng 1 consumer hoặc dùng message grouping

### 2. Idempotency
- Đảm bảo operations là idempotent
- Nếu message được redeliver, không nên tạo duplicate data

### 3. Database Connections
- Cả 2 instances dùng chung database
- Đảm bảo connection pool đủ lớn
- Cân nhắc transaction isolation levels

### 4. Shared State
- Không có shared in-memory state giữa các instances
- Sử dụng database hoặc cache (Redis) cho shared state

## 📈 Performance Tips

1. **Prefetch Count:** Set = 1 cho fair distribution
2. **Acknowledgment:** Acknowledge sau khi xử lý xong (không phải trước)
3. **Connection Pooling:** Sử dụng connection pooling cho database
4. **Monitoring:** Monitor queue length và consumer count
5. **Scaling:** Thêm consumers khi queue length tăng

## 🔐 Best Practices

1. **Always Acknowledge:** Acknowledge messages sau khi xử lý thành công
2. **Error Handling:** Handle errors và requeue nếu cần
3. **Timeout:** Set timeout cho requests
4. **Monitoring:** Monitor queue và consumer health
5. **Graceful Shutdown:** Shutdown gracefully để finish processing messages

## 📚 Tài Liệu Tham Khảo

- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [RabbitMQ Work Queues](https://www.rabbitmq.com/tutorials/tutorial-two-dotnet.html)
- [RabbitMQ Fair Dispatch](https://www.rabbitmq.com/tutorials/tutorial-two-dotnet.html#fair-dispatch)

