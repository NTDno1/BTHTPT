# 📊 Tổng Hợp Load Balancing - Tất Cả Services

## 🎯 Tổng Quan

Tất cả 3 services (User, Product, Order) đã được clone thành **2 instances** mỗi service để chịu tải song song thông qua **RabbitMQ Load Balancing**.

## 🏗️ Kiến Trúc Tổng Thể

```
                    ┌──────────────────────┐
                    │  API Gateway RabbitMQ│
                    │      Port: 5010      │
                    └──────────┬───────────┘
                               │
                ┌──────────────┼──────────────┐
                │              │              │
                ▼              ▼              ▼
        ┌───────────┐  ┌───────────┐  ┌───────────┐
        │   Queue   │  │   Queue   │  │   Queue   │
        │api.user.  │  │api.product│  │api.order. │
        │  request  │  │  request  │  │  request  │
        └─────┬─────┘  └─────┬─────┘  └─────┬─────┘
              │              │              │
    ┌─────────┴─────────┐   │              │
    │  Auto Distribute  │   │              │
    └─────────┬─────────┘   │              │
              │              │              │
    ┌─────────┴─────────┐   │              │
    │                   │   │              │
    ▼                   ▼   ▼              ▼
┌─────────┐      ┌─────────┐      ┌─────────┐
│User Svc1│      │Product  │      │Order    │
│Port:5001│      │Svc1     │      │Svc1     │
└─────────┘      │Port:5002│      │Port:5003│
                 └─────────┘      └─────────┘
┌─────────┐      ┌─────────┐      ┌─────────┐
│User Svc2│      │Product  │      │Order    │
│Port:5004│      │Svc2     │      │Svc2     │
└─────────┘      │Port:5006│      │Port:5007│
                 └─────────┘      └─────────┘
```

## 📋 Danh Sách Containers

### User Service (2 Instances)
- **user-service-1:** Port `5001`
- **user-service-2:** Port `5004`
- **Queue:** `api.user.request`
- **Response Queue:** `api.user.response`

### Product Service (2 Instances)
- **product-service-1:** Port `5002`
- **product-service-2:** Port `5006`
- **Queue:** `api.product.request`
- **Response Queue:** `api.product.response`

### Order Service (2 Instances)
- **order-service-1:** Port `5003`
- **order-service-2:** Port `5007`
- **Queue:** `api.order.request`
- **Response Queue:** `api.order.response`

## 🔄 Cơ Chế Load Balancing

### RabbitMQ Auto Distribution

RabbitMQ tự động phân phối messages giữa các consumers theo **Round Robin**:

**User Service:**
- Request 1 → Queue → user-service-1
- Request 2 → Queue → user-service-2
- Request 3 → Queue → user-service-1
- Request 4 → Queue → user-service-2
- ...

**Product Service:**
- Request 1 → Queue → product-service-1
- Request 2 → Queue → product-service-2
- Request 3 → Queue → product-service-1
- Request 4 → Queue → product-service-2
- ...

**Order Service:**
- Request 1 → Queue → order-service-1
- Request 2 → Queue → order-service-2
- Request 3 → Queue → order-service-1
- Request 4 → Queue → order-service-2
- ...

## 🚀 Cách Sử Dụng

### Start All Services

```bash
# Start tất cả services (6 service instances + 1 gateway)
docker-compose up -d

# Kiểm tra status
docker-compose ps

# Xem tất cả containers
docker ps | grep microservice
```

### Test Load Balancing

```bash
# Test User Service
for i in {1..10}; do
  curl http://localhost:5010/api/users
  sleep 0.5
done

# Test Product Service
for i in {1..10}; do
  curl http://localhost:5010/api/products
  sleep 0.5
done

# Test Order Service
for i in {1..10}; do
  curl http://localhost:5010/api/orders
  sleep 0.5
done
```

### Monitor All Instances

```bash
# Xem logs của tất cả User Service instances
docker logs -f microservice-user-service-1 &
docker logs -f microservice-user-service-2 &

# Xem logs của tất cả Product Service instances
docker logs -f microservice-product-service-1 &
docker logs -f microservice-product-service-2 &

# Xem logs của tất cả Order Service instances
docker logs -f microservice-order-service-1 &
docker logs -f microservice-order-service-2 &
```

### View Stats

```bash
# Xem CPU và Memory của tất cả instances
docker stats \
  microservice-user-service-1 \
  microservice-user-service-2 \
  microservice-product-service-1 \
  microservice-product-service-2 \
  microservice-order-service-1 \
  microservice-order-service-2
```

## 📊 Port Mapping

| Service | Instance | Port | Container Name |
|---------|----------|------|----------------|
| **User Service** | 1 | 5001 | microservice-user-service-1 |
| **User Service** | 2 | 5004 | microservice-user-service-2 |
| **Product Service** | 1 | 5002 | microservice-product-service-1 |
| **Product Service** | 2 | 5006 | microservice-product-service-2 |
| **Order Service** | 1 | 5003 | microservice-order-service-1 |
| **Order Service** | 2 | 5007 | microservice-order-service-2 |
| **API Gateway RabbitMQ** | - | 5010 | microservice-api-gateway-rabbitmq |
| **Frontend** | - | 4200 | microservice-frontend |

## 🔍 RabbitMQ Queues

| Queue | Service | Consumers | Purpose |
|-------|---------|-----------|---------|
| `api.user.request` | User Service | 2 | Requests từ API Gateway |
| `api.user.response` | User Service | 1 (Gateway) | Responses về API Gateway |
| `api.product.request` | Product Service | 2 | Requests từ API Gateway |
| `api.product.response` | Product Service | 1 (Gateway) | Responses về API Gateway |
| `api.order.request` | Order Service | 2 | Requests từ API Gateway |
| `api.order.response` | Order Service | 1 (Gateway) | Responses về API Gateway |

## ✅ Ưu Điểm

1. **High Availability:** Nếu một instance down, instance còn lại vẫn hoạt động
2. **Load Distribution:** Tải được phân phối đều giữa các instances
3. **Scalability:** Dễ dàng scale thêm instances khi cần
4. **Fault Tolerance:** RabbitMQ tự động chuyển messages khi một consumer down
5. **Zero Configuration:** Không cần cấu hình load balancer riêng

## ⚠️ Lưu Ý

1. **Database:** Tất cả instances của cùng service dùng chung database
2. **State:** Không có shared in-memory state giữa instances
3. **Transactions:** Đảm bảo database transactions được handle đúng
4. **Idempotency:** Operations phải idempotent để tránh duplicate khi retry

## 🔧 Troubleshooting

### Kiểm tra Queue Status

Truy cập RabbitMQ Management: `http://47.130.33.106:15672`

- Vào tab **Queues**
- Kiểm tra số **Consumers** cho mỗi queue (sẽ là 2 cho request queues)
- Kiểm tra **Message Rate** để thấy load distribution

### Kiểm tra Container Health

```bash
# Xem health của tất cả containers
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# Xem logs nếu có lỗi
docker-compose logs --tail=50
```

### Test Individual Instances

```bash
# Test trực tiếp từng instance (bypass RabbitMQ)
curl http://localhost:5001/api/users
curl http://localhost:5004/api/users
curl http://localhost:5002/api/products
curl http://localhost:5006/api/products
curl http://localhost:5003/api/orders
curl http://localhost:5007/api/orders
```

## 📈 Performance

Với 2 instances mỗi service:
- **Throughput:** Tăng gấp đôi so với 1 instance
- **Response Time:** Giảm do load được phân phối
- **Availability:** Tăng cao hơn (nếu 1 instance down, vẫn còn 1 instance)

## 🎯 Kết Luận

Hệ thống hiện có:
- ✅ **6 Service Instances** (2 instances × 3 services)
- ✅ **1 API Gateway** (RabbitMQ)
- ✅ **1 Frontend**
- ✅ **Tự động Load Balancing** qua RabbitMQ
- ✅ **High Availability** với fault tolerance

Tất cả requests từ Frontend đi qua API Gateway RabbitMQ và được tự động phân phối đến các service instances thông qua RabbitMQ queues.

