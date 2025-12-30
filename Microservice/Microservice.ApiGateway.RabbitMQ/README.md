# 🚪 API Gateway RabbitMQ

API Gateway sử dụng RabbitMQ để điều hướng requests đến các microservices thông qua message queue.

## 📋 Tổng Quan

API Gateway RabbitMQ là một API Gateway thứ hai trong hệ thống, sử dụng RabbitMQ làm phương tiện giao tiếp giữa client và các microservices. Khác với API Gateway Ocelot (sử dụng HTTP trực tiếp), API Gateway này sử dụng message queue pattern.

## ❓ Câu Hỏi Thường Gặp

### 1. **Dữ liệu từ client → API Gateway → các service được điều hướng theo route path có đúng không?**

✅ **ĐÚNG HOÀN TOÀN!**

**Luồng điều hướng:**
```
Client Request: GET /api/users/123
    ↓
API Gateway (Port 5010)
    ↓ Parse path: "/api/users/123"
RouteMappingService
    ↓ Map "users" → "UserService"
    ↓ Lấy queue từ config: "api.user.request"
RabbitMQ Queue: api.user.request
    ↓ Message routing
UserService Consumer
    ↓ Xử lý request
```

**Chi tiết:**
1. **Client gửi request** với path: `/api/users/123`
2. **GatewayController** nhận request, parse path
3. **RouteMappingService** tự động map:
   - Extract segment: `"users"` từ path `/api/users/123`
   - Tra cứu trong config: `ServiceRoutes:UserService:RoutePrefix = "users"`
   - → Xác định service: `"UserService"`
4. **RabbitMQGatewayService** lấy queue name từ config:
   - `ServiceRoutes:UserService:Queue = "api.user.request"`
   - → Gửi message vào queue `api.user.request`
5. **UserService Consumer** lắng nghe queue `api.user.request` và nhận message

**Kết luận:** ✅ Dữ liệu được điều hướng chính xác theo route path thông qua RouteMappingService và configuration.

---

### 2. **Khi request được gửi tới API Gateway, RabbitMQ có trả lại Response ngay không hay phải đợi service phản hồi?**

❌ **RabbitMQ KHÔNG trả response ngay!** Phải đợi service xử lý và gửi response về.

**Giải thích chi tiết:**

#### **RabbitMQ chỉ là Message Broker (Trung gian)**
RabbitMQ **KHÔNG xử lý business logic**, nó chỉ:
- ✅ Nhận message từ publisher (Gateway)
- ✅ Lưu message vào queue
- ✅ Chuyển message cho consumer (Service)
- ✅ Nhận response từ service
- ✅ Chuyển response về cho gateway

**RabbitMQ KHÔNG:**
- ❌ Xử lý request
- ❌ Trả response ngay
- ❌ Biết nội dung message

#### **Luồng thực tế (Async - Bất đồng bộ):**

```
1. Client → API Gateway
   ↓ HTTP Request: GET /api/users/123
   
2. API Gateway → RabbitMQ
   ↓ Publish message vào queue "api.user.request"
   ✅ RabbitMQ nhận message → Lưu vào queue
   ⏸️ Gateway ĐỢI (await tcs.Task) - KHÔNG có response ngay!
   
3. RabbitMQ → UserService Consumer
   ↓ Message được consume từ queue
   ⏸️ Service đang xử lý (query database, business logic...)
   
4. UserService → RabbitMQ
   ↓ Publish response vào queue "api.gateway.response"
   ✅ RabbitMQ nhận response → Lưu vào queue
   
5. RabbitMQ → API Gateway Consumer
   ↓ Gateway consumer nhận response
   ✅ Match CorrelationId → Resolve TaskCompletionSource
   ✅ await tcs.Task → Nhận được ApiResponse
   
6. API Gateway → Client
   ↓ HTTP Response: 200 OK + JSON data
   ✅ Client nhận được response
```

#### **Code chứng minh:**

**Trong RabbitMQGatewayService.SendRequestAsync():**
```csharp
// 1. Gửi message vào RabbitMQ
_channel.BasicPublish(
    exchange: "",
    routingKey: requestQueue,  // "api.user.request"
    basicProperties: properties,
    body: body);

// 2. ĐỢI response (KHÔNG có response ngay!)
var response = await tcs.Task.WaitAsync(cts.Token);  // ⏸️ Block ở đây
return response;
```

**TaskCompletionSource chỉ được resolve khi:**
```csharp
// Trong Consumer handler (khi nhận response từ service)
consumer.Received += (model, ea) =>
{
    var response = JsonSerializer.Deserialize<ApiResponse>(message);
    
    // Match CorrelationId và resolve TaskCompletionSource
    if (_pendingRequests.TryRemove(response.CorrelationId, out var tcs))
    {
        tcs.SetResult(response);  // ✅ Lúc này mới có response!
    }
};
```

#### **Timeline thực tế:**

```
T=0ms:   Client gửi request
T=1ms:   Gateway nhận request, parse path
T=2ms:   Gateway gửi message vào RabbitMQ queue
T=3ms:   RabbitMQ lưu message vào queue ✅
         ⏸️ Gateway ĐỢI (await tcs.Task) - KHÔNG có response!
T=5ms:   UserService consumer nhận message từ queue
T=10ms:  UserService query database
T=50ms:  UserService xử lý business logic
T=100ms: UserService tạo response
T=101ms: UserService gửi response vào RabbitMQ
T=102ms: RabbitMQ lưu response vào queue ✅
T=103ms: Gateway consumer nhận response
T=104ms: Gateway match CorrelationId → tcs.SetResult()
T=105ms: await tcs.Task → Nhận được response ✅
T=106ms: Gateway trả HTTP response về client
T=107ms: Client nhận được response
```

**Kết luận:** 
- ❌ RabbitMQ **KHÔNG trả response ngay** khi nhận request
- ✅ Phải **đợi service xử lý** và gửi response về
- ✅ Gateway sử dụng **async/await pattern** với **TaskCompletionSource** để đợi response
- ✅ Timeout: Nếu service không phản hồi trong 30 giây → Gateway trả `504 Gateway Timeout`

---

### **So sánh với API Gateway Ocelot (HTTP trực tiếp):**

| Đặc điểm | Ocelot Gateway | RabbitMQ Gateway |
|----------|---------------|------------------|
| **Giao tiếp** | HTTP trực tiếp | Message Queue (RabbitMQ) |
| **Response time** | Nhanh hơn (direct) | Chậm hơn (qua queue) |
| **Điều hướng** | Theo route config | Theo route path + queue config |
| **Async** | Synchronous HTTP | Asynchronous messaging |
| **Decoupling** | Tight coupling | Loose coupling |
| **Scalability** | Phụ thuộc HTTP | Dễ scale với queue |

---

## 🔄 Load Balancing & Scaling

### **Câu hỏi 1: Product Service chạy trên 2 máy chủ khác nhau - RabbitMQ xử lý như thế nào?**

✅ **RabbitMQ tự động phân phối messages theo Round-Robin Pattern!**

#### **Work Queue Pattern (Competing Consumers)**

Khi có **nhiều consumers** cùng lắng nghe **một queue**, RabbitMQ sẽ tự động phân phối messages theo nguyên tắc **Round-Robin** (luân phiên).

**Ví dụ: 2 Product Service instances**

```
┌─────────────────┐
│  API Gateway    │
└────────┬────────┘
         │ Gửi messages vào queue
         ↓
┌─────────────────────────────┐
│  RabbitMQ Queue             │
│  "api.product.request"      │
│  ┌─────────────────────┐    │
│  │ Message 1           │    │
│  │ Message 2           │    │
│  │ Message 3           │    │
│  │ Message 4           │    │
│  │ Message 5           │    │
│  └─────────────────────┘    │
└────────┬────────────────────┘
         │ RabbitMQ phân phối
         ├──────────────┬──────────────┐
         ↓              ↓              ↓
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ Product Service │ │ Product Service │ │ Product Service │
│ Instance 1      │ │ Instance 2      │ │ Instance 3      │
│ (Server A)      │ │ (Server B)      │ │ (Server C)      │
│                 │ │                 │ │                 │
│ Consumer 1      │ │ Consumer 2      │ │ Consumer 3      │
└─────────────────┘ └─────────────────┘ └─────────────────┘
```

**Phân phối messages:**
- Message 1 → Consumer 1 (Server A)
- Message 2 → Consumer 2 (Server B)
- Message 3 → Consumer 3 (Server C)
- Message 4 → Consumer 1 (Server A) - Quay vòng
- Message 5 → Consumer 2 (Server B)
- ...

#### **Cách hoạt động:**

**1. Mỗi Product Service instance tạo consumer:**
```csharp
// Product Service Instance 1 (Server A)
_channel.BasicConsume(
    queue: "api.product.request",
    autoAck: false,
    consumer: consumer1);

// Product Service Instance 2 (Server B)
_channel.BasicConsume(
    queue: "api.product.request",  // CÙNG queue!
    autoAck: false,
    consumer: consumer2);
```

**2. RabbitMQ tự động phân phối:**
- ✅ RabbitMQ **tự quyết định** phân phối messages
- ✅ **Round-Robin**: Messages được phân phối luân phiên
- ✅ **Fair Dispatch**: Chỉ gửi message khi consumer sẵn sàng (prefetch)
- ✅ **Auto Load Balancing**: Tự động cân bằng tải

**3. Prefetch Count (Quan trọng!):**
```csharp
// Giới hạn số messages mỗi consumer nhận cùng lúc
_channel.BasicQos(
    prefetchSize: 0,
    prefetchCount: 1,  // Chỉ nhận 1 message tại một thời điểm
    global: false);
```

**Lợi ích:**
- ✅ Consumer nhanh nhận nhiều messages hơn
- ✅ Consumer chậm không bị quá tải
- ✅ Tự động cân bằng tải

#### **Kết luận:**

✅ **RabbitMQ quyết định load balancing**, không phải 2 server!

**Ưu điểm:**
- ✅ **Tự động**: Không cần cấu hình thêm
- ✅ **Cân bằng**: Tự động phân phối đều
- ✅ **Linh hoạt**: Thêm/xóa instances dễ dàng
- ✅ **Resilient**: Nếu 1 instance down, messages tự động chuyển sang instance khác

**Lưu ý:**
- ⚠️ Cần set `prefetchCount` để tránh 1 consumer nhận quá nhiều messages
- ⚠️ Messages phải **idempotent** (xử lý nhiều lần không ảnh hưởng)
- ⚠️ Cần xử lý **duplicate messages** nếu consumer crash giữa chừng

---

### **Câu hỏi 2: API Gateway quá tải - Phải làm sao? Có thường gặp không?**

⚠️ **Đây là vấn đề THƯỜNG GẶP trong production!** Có nhiều giải pháp:

#### **Vấn đề thực tế:**

**Khi nào xảy ra:**
- 📈 Traffic spike (Black Friday, flash sale)
- 🚀 Viral content
- 🔥 DDoS attacks
- 📱 Mobile app release
- 🎯 Marketing campaigns

**Triệu chứng:**
- ⏱️ Response time tăng cao
- ❌ 503 Service Unavailable
- 🔴 Connection timeouts
- 💾 Memory/CPU usage cao
- 🐌 Request queue dài

#### **Giải pháp 1: Horizontal Scaling (Scale Out)**

**Thêm nhiều API Gateway instances:**

```
                    ┌─────────────┐
                    │ Load Balancer│
                    │  (Nginx/HAProxy)│
                    └──────┬──────┘
                           │
        ┌──────────────────┼──────────────────┐
        ↓                  ↓                  ↓
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ API Gateway  │  │ API Gateway  │  │ API Gateway  │
│ Instance 1   │  │ Instance 2   │  │ Instance 3   │
│ (Port 5010)  │  │ (Port 5011)  │  │ (Port 5012)  │
└──────┬───────┘  └──────┬───────┘  └──────┬───────┘
       │                 │                  │
       └─────────────────┼──────────────────┘
                          ↓
                  ┌───────────────┐
                  │   RabbitMQ    │
                  │     Server    │
                  └───────────────┘
```

**Cách triển khai:**
1. **Docker/Kubernetes**: Scale pods/containers
2. **Load Balancer**: Nginx, HAProxy, AWS ALB
3. **Health Checks**: Tự động loại bỏ unhealthy instances

**Ví dụ với Docker Compose:**
```yaml
api-gateway-rabbitmq:
  build: .
  deploy:
    replicas: 3  # 3 instances
  ports:
    - "5010-5012:8080"
```

**Ví dụ với Kubernetes:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api-gateway-rabbitmq
spec:
  replicas: 5  # 5 instances
  template:
    spec:
      containers:
      - name: api-gateway
        image: api-gateway-rabbitmq:latest
```

#### **Giải pháp 2: Vertical Scaling (Scale Up)**

**Tăng resources cho API Gateway:**
- 💾 Tăng RAM
- 🖥️ Tăng CPU cores
- 💿 Tăng disk I/O
- 🌐 Tăng network bandwidth

**Giới hạn:**
- ⚠️ Chi phí cao
- ⚠️ Có giới hạn phần cứng
- ⚠️ Không giải quyết được single point of failure

#### **Giải pháp 3: Caching**

**Cache responses để giảm tải:**

```csharp
// Thêm caching middleware
builder.Services.AddMemoryCache();

// Hoặc Redis cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
```

**Cache strategies:**
- ✅ **Response Caching**: Cache HTTP responses
- ✅ **In-Memory Cache**: Cache trong memory
- ✅ **Distributed Cache**: Redis, Memcached
- ✅ **CDN**: CloudFlare, AWS CloudFront

#### **Giải pháp 4: Rate Limiting**

**Giới hạn số requests từ mỗi client:**

```csharp
// Thêm rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,  // 100 requests
                Window = TimeSpan.FromMinutes(1)  // per minute
            }));
});
```

**Lợi ích:**
- ✅ Bảo vệ khỏi DDoS
- ✅ Đảm bảo fair usage
- ✅ Giảm tải cho backend

#### **Giải pháp 5: Circuit Breaker Pattern**

**Tự động ngắt kết nối khi service down:**

```csharp
// Thêm Polly Circuit Breaker
builder.Services.AddHttpClient("ProductService")
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30));
}
```

#### **Giải pháp 6: Message Queue Buffering**

**Lợi ích của RabbitMQ trong trường hợp này:**

✅ **RabbitMQ đóng vai trò buffer:**
- Gateway gửi messages vào queue → **Không bị block**
- Messages được lưu trong queue → **Không mất dữ liệu**
- Services xử lý từ từ → **Không quá tải**

**So sánh với HTTP trực tiếp:**

| Tình huống | HTTP Direct | RabbitMQ |
|-----------|-------------|----------|
| **Gateway quá tải** | ❌ Block requests | ✅ Queue messages |
| **Service chậm** | ❌ Timeout | ✅ Messages chờ trong queue |
| **Service down** | ❌ 503 Error | ✅ Messages chờ, retry sau |
| **Traffic spike** | ❌ Reject requests | ✅ Buffer trong queue |

#### **Giải pháp 7: Async Processing (Fire and Forget)**

**Cho các operations không cần response ngay:**

```csharp
// Thay vì đợi response
public async Task<IActionResult> CreateOrder([FromBody] OrderDto order)
{
    // Gửi message và không đợi response
    await _gatewayService.PublishAsync(order, "order.created");
    
    // Trả về ngay
    return Accepted(new { message = "Order is being processed" });
}
```

#### **Best Practices trong Production:**

1. **Monitoring & Alerting:**
   - 📊 Monitor request rate, response time
   - 🚨 Alert khi quá ngưỡng
   - 📈 Metrics: Prometheus, Grafana

2. **Auto Scaling:**
   - 🤖 Auto scale dựa trên CPU/Memory
   - 📈 Scale dựa trên queue length
   - ⚡ Scale dựa trên request rate

3. **Health Checks:**
   - ❤️ Liveness probe
   - ✅ Readiness probe
   - 🔄 Graceful shutdown

4. **Connection Pooling:**
   - 🔌 Reuse RabbitMQ connections
   - 📦 Connection pooling
   - ⚡ Async I/O

5. **Request Timeout:**
   - ⏱️ Set timeout hợp lý (30s)
   - 🔄 Retry với exponential backoff
   - ❌ Fail fast cho critical errors

#### **Kết luận:**

✅ **Vấn đề API Gateway quá tải RẤT THƯỜNG GẶP trong production!**

**Giải pháp tốt nhất:**
1. ✅ **Horizontal Scaling** (Thêm instances)
2. ✅ **Load Balancer** (Phân phối traffic)
3. ✅ **RabbitMQ Buffering** (Queue messages)
4. ✅ **Caching** (Giảm tải backend)
5. ✅ **Rate Limiting** (Bảo vệ khỏi abuse)
6. ✅ **Monitoring** (Phát hiện sớm)

**RabbitMQ Gateway có ưu điểm:**
- ✅ **Resilient**: Messages không bị mất khi gateway quá tải
- ✅ **Buffering**: Queue đóng vai trò buffer
- ✅ **Decoupling**: Gateway và services độc lập
- ✅ **Scalability**: Dễ scale services độc lập

## 🎯 Đặc Điểm

- ✅ **Message-Based Communication**: Sử dụng RabbitMQ để gửi/nhận requests
- ✅ **RPC Pattern**: Sử dụng correlation ID để match requests và responses
- ✅ **Async Processing**: Hỗ trợ xử lý bất đồng bộ
- ✅ **Timeout Handling**: Tự động timeout sau 30 giây nếu không nhận được response
- ✅ **Auto Routing**: Tự động điều hướng dựa trên path, không cần viết controller riêng cho mỗi service
- ✅ **Configuration-Based**: Cấu hình routes trong appsettings.json
- ✅ **Swagger Documentation**: Tích hợp Swagger UI

## 🔧 Cấu Hình

### Port
- **HTTP**: 5010
- **HTTPS**: 5011

### RabbitMQ Configuration
```json
{
  "RabbitMQ": {
    "HostName": "47.130.33.106",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest"
  }
}
```

### Service Routes
Gateway sử dụng các queues sau để giao tiếp với services:
- **UserService**: `api.user.request` / `api.user.response`
- **ProductService**: `api.product.request` / `api.product.response`
- **OrderService**: `api.order.request` / `api.order.response`

## 🚀 Chạy Dự Án

### Local Development
```bash
cd Microservice/Microservice.ApiGateway.RabbitMQ
dotnet run
```

### Docker
```bash
docker-compose up api-gateway-rabbitmq
```

## 📡 API Endpoints

Gateway tự động điều hướng tất cả requests dựa trên path. Chỉ cần một controller duy nhất (`GatewayController`) xử lý tất cả routes.

### Auto Routing Pattern
Gateway tự động map path với service dựa trên configuration:
- `/api/users/**` → `UserService`
- `/api/products/**` → `ProductService`
- `/api/orders/**` → `OrderService`

### Ví dụ Endpoints

**Users:**
- `GET /api/users` - Lấy danh sách users
- `GET /api/users/{id}` - Lấy user theo ID
- `POST /api/users` - Tạo user mới
- `PUT /api/users/{id}` - Cập nhật user
- `DELETE /api/users/{id}` - Xóa user

### 📝 Ví Dụ Cụ Thể: POST Request

**Request từ Client:**
```http
POST http://localhost:5010/api/users
Content-Type: application/json

{
  "name": "Jane Doe",
  "email": "jane@example.com"
}
```

**ApiRequest được tạo trong Gateway:**
```json
{
  "Method": "POST",
  "Path": "/api/users",
  "QueryParameters": null,
  "Headers": {
    "Content-Type": "application/json",
    "Host": "localhost:5010"
  },
  "Body": {
    "name": "Jane Doe",
    "email": "jane@example.com"
  },
  "CorrelationId": "abc123-def456-ghi789",
  "Timestamp": "2024-01-15T10:30:00Z"
}
```

**Message trong RabbitMQ Queue `api.user.request`:**
- **Routing Key**: `api.user.request`
- **CorrelationId**: `abc123-def456-ghi789`
- **ReplyTo**: `api.gateway.response`
- **Body**: JSON của ApiRequest trên

**Microservice xử lý và tạo response:**
```json
{
  "StatusCode": 201,
  "Data": {
    "id": 456,
    "name": "Jane Doe",
    "email": "jane@example.com",
    "createdAt": "2024-01-15T10:30:01Z"
  },
  "ErrorMessage": null,
  "CorrelationId": "abc123-def456-ghi789",
  "Timestamp": "2024-01-15T10:30:01Z"
}
```

**Response gửi về Client:**
```http
HTTP/1.1 201 Created
Content-Type: application/json

{
  "id": 456,
  "name": "Jane Doe",
  "email": "jane@example.com",
  "createdAt": "2024-01-15T10:30:01Z"
}
```

**Products:**
- `GET /api/products` - Lấy danh sách products
- `GET /api/products/{id}` - Lấy product theo ID
- `POST /api/products` - Tạo product mới
- `PUT /api/products/{id}` - Cập nhật product
- `DELETE /api/products/{id}` - Xóa product

**Orders:**
- `GET /api/orders` - Lấy danh sách orders
- `GET /api/orders/{id}` - Lấy order theo ID
- `POST /api/orders` - Tạo order mới
- `PUT /api/orders/{id}` - Cập nhật order
- `DELETE /api/orders/{id}` - Xóa order

**Health Check:**
- `GET /api/health` - Kiểm tra trạng thái service

### Thêm Service Mới

Để thêm service mới, chỉ cần cập nhật `appsettings.json`:

```json
{
  "ServiceRoutes": {
    "NewService": {
      "Queue": "api.newservice.request",
      "ResponseQueue": "api.newservice.response",
      "RoutePrefix": "newservice"
    }
  }
}
```

Sau đó gateway sẽ tự động route `/api/newservice/**` đến `NewService`!

## 🔄 Luồng Hoạt Động Chi Tiết

### 📤 **LUỒNG REQUEST (Client → Microservice)**

#### **Bước 1: Client Gửi HTTP Request**
```
Client (Browser/Postman/App)
    ↓ HTTP Request
GET http://localhost:5010/api/users/123
Headers: { "Content-Type": "application/json" }
```

**Dữ liệu gửi đi:**
- Method: `GET`
- Path: `/api/users/123`
- Headers: HTTP headers từ client
- Body: (null cho GET request)

---

#### **Bước 2: GatewayController Nhận Request**
```
GatewayController.RouteRequest()
    ↓ Xử lý
```

**Xử lý trong GatewayController:**
1. **Parse Path**: `/api/users/123` → extract `users/123`
2. **Route Mapping**: Gọi `RouteMappingService.GetServiceNameFromPath()`
   - Input: `/api/users/123`
   - Output: `"UserService"`
3. **Đọc Request Data**:
   - Method: `"GET"`
   - Path: `"/api/users/123"`
   - Query Parameters: Parse từ URL (nếu có)
   - Headers: Copy từ HTTP request
   - Body: Đọc từ Request.Body (nếu có)

**Tạo ApiRequest Object:**
```json
{
  "Method": "GET",
  "Path": "/api/users/123",
  "QueryParameters": null,
  "Headers": { "Content-Type": "application/json", ... },
  "Body": null,
  "CorrelationId": "550e8400-e29b-41d4-a716-446655440000",
  "Timestamp": "2024-01-15T10:30:00Z"
}
```

---

#### **Bước 3: RabbitMQGatewayService Gửi Request**
```
RabbitMQGatewayService.SendRequestAsync()
    ↓
```

**Xử lý:**
1. **Lấy Queue Name**: Từ config `ServiceRoutes:UserService:Queue` → `"api.user.request"`
2. **Tạo TaskCompletionSource**: 
   - Key: `CorrelationId` = `"550e8400-e29b-41d4-a716-446655440000"`
   - Value: `TaskCompletionSource<ApiResponse>` (để đợi response)
   - Lưu vào `_pendingRequests` dictionary
3. **Serialize Request**: Convert `ApiRequest` object → JSON string
4. **Tạo RabbitMQ Message Properties**:
   - `CorrelationId`: `"550e8400-e29b-41d4-a716-446655440000"`
   - `ReplyTo`: `"api.gateway.response"` (queue để nhận response)
   - `Persistent`: `true`
5. **Publish Message**:
   - Exchange: `""` (default exchange)
   - Routing Key: `"api.user.request"`
   - Body: JSON bytes của ApiRequest

**Message trong RabbitMQ Queue `api.user.request`:**
```json
{
  "Method": "GET",
  "Path": "/api/users/123",
  "QueryParameters": null,
  "Headers": { ... },
  "Body": null,
  "CorrelationId": "550e8400-e29b-41d4-a716-446655440000",
  "Timestamp": "2024-01-15T10:30:00Z"
}
```

**RabbitMQ Message Properties:**
- CorrelationId: `"550e8400-e29b-41d4-a716-446655440000"`
- ReplyTo: `"api.gateway.response"`

---

#### **Bước 4: Microservice Consumer Nhận Request**
```
RabbitMQ Queue: api.user.request
    ↓ Message được consume
UserService Consumer (Background Service)
    ↓
```

**Microservice cần có Consumer để:**
1. **Lắng nghe Queue**: `api.user.request`
2. **Nhận Message**: Deserialize `ApiRequest` object
3. **Xử lý Request**:
   - Parse `Path`: `/api/users/123` → extract ID = `123`
   - Gọi business logic: `GetUserById(123)`
   - Query database hoặc xử lý logic
4. **Tạo Response**: Tạo `ApiResponse` object

**ApiResponse được tạo:**
```json
{
  "StatusCode": 200,
  "Data": {
    "id": 123,
    "name": "John Doe",
    "email": "john@example.com"
  },
  "ErrorMessage": null,
  "CorrelationId": "550e8400-e29b-41d4-a716-446655440000",
  "Timestamp": "2024-01-15T10:30:01Z"
}
```

**⚠️ Lưu ý**: CorrelationId phải giữ nguyên từ request để gateway có thể match!

---

### 📥 **LUỒNG RESPONSE (Microservice → Client)**

#### **Bước 5: Microservice Gửi Response**
```
UserService Consumer
    ↓ Publish response
RabbitMQ Queue: api.gateway.response
```

**Microservice gửi response:**
1. **Serialize Response**: Convert `ApiResponse` object → JSON string
2. **Tạo RabbitMQ Message Properties**:
   - `CorrelationId`: Lấy từ request ban đầu (`"550e8400-e29b-41d4-a716-446655440000"`)
   - Routing Key: `"api.gateway.response"` (từ `ReplyTo` của request)
3. **Publish Message**:
   - Exchange: `""` (default exchange)
   - Routing Key: `"api.gateway.response"`
   - Body: JSON bytes của ApiResponse

**Message trong RabbitMQ Queue `api.gateway.response`:**
```json
{
  "StatusCode": 200,
  "Data": {
    "id": 123,
    "name": "John Doe",
    "email": "john@example.com"
  },
  "ErrorMessage": null,
  "CorrelationId": "550e8400-e29b-41d4-a716-446655440000",
  "Timestamp": "2024-01-15T10:30:01Z"
}
```

---

#### **Bước 6: Gateway Consumer Nhận Response**
```
RabbitMQ Queue: api.gateway.response
    ↓ Message được consume
RabbitMQGatewayService Consumer (EventingBasicConsumer)
    ↓
```

**Xử lý trong Gateway Consumer:**
1. **Nhận Message**: Deserialize `ApiResponse` object
2. **Match Correlation ID**: 
   - Lấy `CorrelationId` từ response: `"550e8400-e29b-41d4-a716-446655440000"`
   - Tìm trong `_pendingRequests` dictionary
   - Tìm thấy: `TaskCompletionSource` tương ứng
3. **Set Result**: 
   - `tcs.SetResult(response)` → Unblock `await tcs.Task` trong `SendRequestAsync()`
4. **Acknowledge Message**: `_channel.BasicAck()` để xác nhận đã xử lý

**Dictionary `_pendingRequests` sau khi match:**
```csharp
_pendingRequests.Remove("550e8400-e29b-41d4-a716-446655440000");
// TaskCompletionSource được resolve với ApiResponse
```

---

#### **Bước 7: GatewayController Trả Response**
```
RabbitMQGatewayService.SendRequestAsync()
    ↓ await tcs.Task → nhận được ApiResponse
GatewayController.RouteRequest()
    ↓
```

**Xử lý:**
1. **Nhận ApiResponse**: Từ `await _gatewayService.SendRequestAsync()`
2. **Trả về HTTP Response**:
   - Status Code: `response.StatusCode` (200)
   - Body: `response.Data` (JSON object)
   - Headers: HTTP response headers

**HTTP Response gửi về Client:**
```
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": 123,
  "name": "John Doe",
  "email": "john@example.com"
}
```

---

#### **Bước 8: Client Nhận Response**
```
GatewayController
    ↓ HTTP Response
Client (Browser/Postman/App)
```

**Client nhận được:**
- Status Code: `200 OK`
- Response Body: JSON data của user
- Headers: HTTP response headers

---

### 🔄 **Sơ Đồ Tổng Quan (Bidirectional Flow)**

```
┌─────────┐
│ Client  │
└────┬────┘
     │ ① HTTP Request: GET /api/users/123
     ↓
┌─────────────────────┐
│ GatewayController   │ ② Parse path, tạo ApiRequest
│ - RouteRequest()    │ ③ RouteMappingService: "users" → "UserService"
└────┬────────────────┘
     │ ④ ApiRequest object
     ↓
┌──────────────────────────┐
│ RabbitMQGatewayService   │ ⑤ Serialize, tạo CorrelationId
│ - SendRequestAsync()     │ ⑥ Lưu TaskCompletionSource vào _pendingRequests
└────┬─────────────────────┘
     │ ⑦ Publish message với CorrelationId
     ↓
┌─────────────────────┐
│ RabbitMQ Server     │
│ Queue:              │
│ api.user.request    │ ⑧ Message chờ trong queue
└────┬────────────────┘
     │ ⑨ Consumer nhận message
     ↓
┌─────────────────────┐
│ UserService         │ ⑩ Deserialize ApiRequest
│ Consumer            │ ⑪ Xử lý: GetUserById(123)
│ - ProcessRequest()  │ ⑫ Query database
└────┬────────────────┘
     │ ⑬ Tạo ApiResponse với cùng CorrelationId
     ↓
┌─────────────────────┐
│ RabbitMQ Server     │
│ Queue:              │ ⑭ Publish response message
│ api.gateway.response│
└────┬────────────────┘
     │ ⑮ Consumer nhận response
     ↓
┌──────────────────────────┐
│ RabbitMQGatewayService   │ ⑯ Match CorrelationId
│ Consumer                 │ ⑰ Tìm TaskCompletionSource trong _pendingRequests
│ - EventingBasicConsumer  │ ⑱ tcs.SetResult(response)
└────┬─────────────────────┘
     │ ⑲ await tcs.Task → nhận ApiResponse
     ↓
┌─────────────────────┐
│ GatewayController   │ ⑳ Trả HTTP Response
│ - RouteRequest()    │
└────┬────────────────┘
     │ ㉑ HTTP Response: 200 OK + JSON data
     ↓
┌─────────┐
│ Client  │ ㉒ Nhận response, hiển thị data
└─────────┘
```

---

### 🔑 **Điểm Quan Trọng**

#### **1. Correlation ID Pattern (RPC Pattern)**
- **Mục đích**: Match request và response trong async messaging
- **Cách hoạt động**:
  - Gateway tạo unique `CorrelationId` cho mỗi request
  - Lưu `TaskCompletionSource` với key = `CorrelationId`
  - Microservice giữ nguyên `CorrelationId` trong response
  - Gateway match `CorrelationId` để resolve `TaskCompletionSource`

#### **2. Pending Requests Dictionary**
```csharp
ConcurrentDictionary<string, TaskCompletionSource<ApiResponse>> _pendingRequests
```
- **Key**: CorrelationId (string)
- **Value**: TaskCompletionSource để đợi response
- **Thread-safe**: Sử dụng `ConcurrentDictionary` cho multi-thread

#### **3. Timeout Handling**
- Gateway đợi response tối đa **30 giây**
- Nếu timeout: Trả về `504 Gateway Timeout`
- Cleanup: Xóa `TaskCompletionSource` khỏi `_pendingRequests`

#### **4. Queue Names**
- **Request Queue**: `api.{service}.request` (ví dụ: `api.user.request`)
- **Response Queue**: `api.gateway.response` (chung cho tất cả services)
- **ReplyTo Property**: Microservice biết gửi response về đâu

## 🎨 Kiến Trúc

### Generic Controller Pattern
Gateway sử dụng một controller duy nhất (`GatewayController`) với catch-all route `[Route("api/{**path}")]` để xử lý tất cả requests. Controller tự động:
1. Parse path để xác định service
2. Đọc request body, query parameters, headers
3. Gửi request qua RabbitMQ
4. Đợi và trả về response

### Route Mapping
`RouteMappingService` tự động map routes từ configuration:
- Đọc `ServiceRoutes` từ `appsettings.json`
- Map route prefix (ví dụ: "users") với service name (ví dụ: "UserService")
- Hỗ trợ fallback: tự động convert "UserService" → "users"

## 📝 Lưu Ý

⚠️ **Quan trọng**: Để API Gateway RabbitMQ hoạt động, các microservices cần phải có consumers để lắng nghe các request queues và gửi responses về response queue.

Hiện tại, các microservices chỉ có producers (gửi events), chưa có consumers để xử lý requests từ gateway. Bạn cần implement consumers trong các services để gateway có thể hoạt động đầy đủ.

## 🔗 Xem Thêm

- [API Gateway Ocelot](../Microservice.ApiGateway/README.md) - API Gateway sử dụng Ocelot
- [Architecture Documentation](../ARCHITECTURE.md) - Kiến trúc tổng thể

