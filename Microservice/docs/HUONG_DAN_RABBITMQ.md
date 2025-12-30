# Hướng Dẫn API Gateway RabbitMQ - Tổng Hợp

## 📑 Mục Lục

1. [Giải Thích RabbitMQConsumerService](#1-giải-thích-rabbitmqconsumerservice)
2. [Queue Được Tạo Ở Đâu?](#2-queue-được-tạo-ở-đâu)
3. [Load Balancing với Multiple Instances](#3-load-balancing-với-multiple-instances)
4. [Hướng Dẫn Restart Service](#4-hướng-dẫn-restart-service)

---

## 1. Giải Thích RabbitMQConsumerService

### 🎯 Tại Sao Cần RabbitMQConsumerService?

#### Kiến Trúc Microservice với RabbitMQ

Khi bạn sử dụng **API Gateway RabbitMQ**, luồng hoạt động khác hoàn toàn so với API Gateway thông thường:

#### **API Gateway Thông Thường (Ocelot):**
```
Frontend → API Gateway (Ocelot) → HTTP Request → UserService → HTTP Response → API Gateway → Frontend
```
- **Trực tiếp**: API Gateway gọi trực tiếp đến UserService qua HTTP
- **Đồng bộ**: Phải đợi response ngay lập tức
- **Không cần Consumer**: UserService chỉ cần có Controller để nhận HTTP requests

#### **API Gateway RabbitMQ:**
```
Frontend → API Gateway RabbitMQ → RabbitMQ Queue → RabbitMQConsumerService → UserService → Response Queue → API Gateway RabbitMQ → Frontend
```
- **Bất đồng bộ**: Sử dụng Message Queue (RabbitMQ)
- **Cần Consumer**: Phải có service lắng nghe queue để nhận requests

### 🔄 Vai Trò Của RabbitMQConsumerService

#### 1. **Lắng Nghe Queue**
- API Gateway RabbitMQ gửi request vào queue `api.user.request`
- **RabbitMQConsumerService** lắng nghe queue này
- Khi có message mới, nó nhận và xử lý

#### 2. **Chuyển Đổi Request**
- Nhận message từ RabbitMQ (dạng JSON string)
- Deserialize thành `ApiRequest` object
- Parse path và method để biết cần gọi function nào

#### 3. **Gọi Business Logic**
- Gọi các methods của `IUserService` (GetAllUsersAsync, CreateUserAsync, etc.)
- Xử lý request giống như Controller thông thường

#### 4. **Gửi Response Về**
- Serialize kết quả thành JSON
- Gửi response vào queue `api.gateway.response`
- API Gateway RabbitMQ nhận response và trả về cho Frontend

### 📊 So Sánh

| Khía Cạnh | API Gateway Thông Thường | API Gateway RabbitMQ |
|-----------|-------------------------|---------------------|
| **Communication** | HTTP trực tiếp | RabbitMQ Queue |
| **UserService cần** | Controller (nhận HTTP) | RabbitMQConsumerService (nhận từ queue) |
| **Đồng bộ/Bất đồng bộ** | Đồng bộ | Bất đồng bộ |
| **Decoupling** | Ít (phụ thuộc trực tiếp) | Nhiều (qua message queue) |

### 🎨 Luồng Hoạt Động Chi Tiết

#### Khi Frontend gọi `GET /api/users`:

1. **Frontend** → Gửi HTTP request đến `http://localhost:5010/api/users`

2. **API Gateway RabbitMQ** (GatewayController):
   - Nhận HTTP request
   - Tạo `ApiRequest` object
   - Serialize thành JSON
   - Gửi vào queue `api.user.request`
   - Đợi response từ queue `api.gateway.response`

3. **RabbitMQConsumerService** (trong UserService):
   - Lắng nghe queue `api.user.request`
   - Nhận message
   - Deserialize thành `ApiRequest`
   - Parse: Method = GET, Path = /api/users
   - Gọi `userService.GetAllUsersAsync()`
   - Tạo `ApiResponse` với data
   - Serialize và gửi vào queue `api.gateway.response`

4. **API Gateway RabbitMQ**:
   - Nhận response từ queue
   - Deserialize thành `ApiResponse`
   - Trả về HTTP response cho Frontend

### 💡 Tại Sao Mỗi Service Cần Consumer Riêng?

Mỗi service (UserService, OrderService, ProductService) có:
- **Queue riêng**: `api.user.request`, `api.order.request`, `api.product.request`
- **Business logic riêng**: Các methods khác nhau
- **DTOs riêng**: UserDto, OrderDto, ProductDto

→ Cần Consumer riêng để:
- Lắng nghe queue của service đó
- Xử lý requests theo logic của service đó
- Gọi đúng service methods

### 🔧 Nếu Không Có RabbitMQConsumerService?

Nếu UserService không có RabbitMQConsumerService:
- API Gateway RabbitMQ gửi request vào queue `api.user.request`
- **Không có ai lắng nghe** → Request bị timeout
- Frontend nhận lỗi 504 Gateway Timeout

### ✅ Tóm Lại

**RabbitMQConsumerService** là "cầu nối" giữa:
- **RabbitMQ Queue** (message-based communication)
- **Business Logic Service** (IUserService, IOrderService, etc.)

Nó đóng vai trò tương tự như **Controller** trong API Gateway thông thường, nhưng:
- Nhận requests từ **Queue** thay vì **HTTP**
- Gửi responses vào **Queue** thay vì **HTTP Response**

Đây là cách hoạt động của **Message-Based Microservices Architecture** với RabbitMQ!

---

## 2. Queue Được Tạo Ở Đâu?

### 📍 Vị Trí Tạo Queue

Queue `api.user.request` được **khai báo (declare)** ở **2 nơi**:

#### 1. **API Gateway RabbitMQ** (Khi gửi request)

**File:** `Microservice.ApiGateway.RabbitMQ/Services/RabbitMQGatewayService.cs`

```csharp
public async Task<ApiResponse> SendRequestAsync(ApiRequest request, string serviceName, ...)
{
    // Lấy queue name từ config
    var routeConfig = _configuration.GetSection($"ServiceRoutes:{serviceName}");
    var requestQueue = routeConfig["Queue"]; // "api.user.request"
    
    // Đảm bảo request queue tồn tại (tạo nếu chưa có)
    _channel.QueueDeclare(
        queue: requestQueue,        // "api.user.request"
        durable: true,              // Queue tồn tại khi RabbitMQ restart
        exclusive: false,           // Nhiều consumers có thể kết nối
        autoDelete: false,          // Không tự xóa khi không có consumer
        arguments: null);
    
    // Gửi message vào queue
    _channel.BasicPublish(
        exchange: "",
        routingKey: requestQueue,   // "api.user.request"
        ...
    );
}
```

**Khi nào:** Mỗi khi API Gateway nhận request và cần gửi đến UserService

#### 2. **UserService Consumer** (Khi khởi động)

**File:** `Microservice.Services.UserService/Services/RabbitMQConsumerService.cs`

```csharp
public RabbitMQConsumerService(...)
{
    // ...
    _connection = factory.CreateConnection();
    _channel = _connection.CreateModel();
    
    // Declare request queue (tạo nếu chưa có)
    _channel.QueueDeclare(
        queue: _requestQueueName,   // "api.user.request"
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null);
    
    _logger.LogInformation("Request queue declared: {QueueName}", _requestQueueName);
}
```

**Khi nào:** Khi UserService khởi động và RabbitMQConsumerService được khởi tạo

### 🔍 Tên Queue Được Định Nghĩa Ở Đâu?

#### 1. **Trong UserService Consumer:**
```csharp
// Microservice.Services.UserService/Services/RabbitMQConsumerService.cs
private readonly string _requestQueueName = "api.user.request";
```

#### 2. **Trong API Gateway Config:**
```json
// Microservice.ApiGateway.RabbitMQ/appsettings.json
{
  "ServiceRoutes": {
    "UserService": {
      "Queue": "api.user.request",      // ← Tên queue ở đây
      "ResponseQueue": "api.user.response",
      "RoutePrefix": "users"
    }
  }
}
```

### ⚙️ Cách Hoạt Động

#### Lần Đầu Tiên:

1. **UserService khởi động trước:**
   - RabbitMQConsumerService khởi tạo
   - Gọi `QueueDeclare("api.user.request", ...)`
   - **Queue được tạo** trong RabbitMQ server

2. **API Gateway gửi request:**
   - Gọi `QueueDeclare("api.user.request", ...)`
   - Queue đã tồn tại → Không tạo mới, chỉ đảm bảo queue tồn tại
   - Gửi message vào queue

#### Nếu API Gateway Khởi Động Trước:

1. **API Gateway gửi request:**
   - Gọi `QueueDeclare("api.user.request", ...)`
   - Queue chưa tồn tại → **Tạo queue mới**
   - Gửi message vào queue (message sẽ chờ trong queue)

2. **UserService khởi động sau:**
   - RabbitMQConsumerService khởi tạo
   - Gọi `QueueDeclare("api.user.request", ...)`
   - Queue đã tồn tại → Không tạo mới
   - Bắt đầu lắng nghe và nhận messages từ queue

### 💡 Tại Sao Cả 2 Nơi Đều Declare Queue?

#### Lý Do:

1. **Idempotent Operation:**
   - `QueueDeclare` là **idempotent** (gọi nhiều lần không ảnh hưởng)
   - Nếu queue đã tồn tại → Không làm gì
   - Nếu queue chưa tồn tại → Tạo mới

2. **Đảm Bảo Queue Tồn Tại:**
   - API Gateway: Đảm bảo queue tồn tại trước khi gửi message
   - Consumer: Đảm bảo queue tồn tại trước khi lắng nghe

3. **Không Phụ Thuộc Thứ Tự Khởi Động:**
   - Không cần quan tâm service nào khởi động trước
   - Queue sẽ được tạo tự động khi cần

### 📊 Luồng Tạo Queue

```
┌─────────────────────────────────────────┐
│  RabbitMQ Server                        │
│                                         │
│  ┌─────────────────────┐               │
│  │ api.user.request    │  ← Queue      │
│  │ (Queue)             │               │
│  └─────────────────────┘               │
│         ↑              ↑                │
│         │              │                │
└─────────┼──────────────┼────────────────┘
          │              │
    ┌─────┴─────┐  ┌────┴────┐
    │ API       │  │ User    │
    │ Gateway   │  │ Service │
    │           │  │         │
    │ Declare   │  │ Declare │
    │ Queue     │  │ Queue   │
    └───────────┘  └─────────┘
```

### 🎯 Tóm Lại

**Queue `api.user.request` được tạo:**
- ✅ **Tên queue**: Định nghĩa trong `appsettings.json` của API Gateway và hardcode trong Consumer Service
- ✅ **Tạo queue**: Bởi cả API Gateway và Consumer Service (ai gọi `QueueDeclare` trước thì tạo)
- ✅ **Vị trí**: Trong RabbitMQ Server (47.130.33.106:5672)
- ✅ **Khi nào**: 
  - Khi API Gateway gửi request đầu tiên
  - HOẶC khi UserService Consumer khởi động

**Lưu ý:** Queue chỉ được tạo **1 lần** trong RabbitMQ server, dù có nhiều nơi gọi `QueueDeclare`.

---

## 3. Load Balancing với Multiple Instances

### 🎯 Câu Hỏi

Giả sử bạn có **2 UserService instances** đang chạy:
- UserService Instance 1 (Port 5001)
- UserService Instance 2 (Port 5002 - khác process)

**API Gateway RabbitMQ sẽ xử lý như thế nào để phân phối requests cho cả 2 instances?**

### ✅ Câu Trả Lời: RabbitMQ Tự Động Load Balancing!

#### Cách Hoạt Động

RabbitMQ sử dụng **Work Queue Pattern** với **Round-Robin Distribution**:

```
API Gateway RabbitMQ
        ↓
   [api.user.request Queue]
        ↓
    ┌───┴───┐
    ↓       ↓
UserService 1  UserService 2
(Consumer 1)   (Consumer 2)
```

**RabbitMQ tự động phân phối messages theo round-robin:**
- Request 1 → UserService Instance 1
- Request 2 → UserService Instance 2
- Request 3 → UserService Instance 1
- Request 4 → UserService Instance 2
- ...

### 🔄 Luồng Hoạt Động Chi Tiết

#### Khi có 2 UserService Instances:

1. **Khởi động 2 UserService instances:**
   ```powershell
   # Terminal 1
   cd Microservice.Services.UserService
   dotnet run --urls "http://localhost:5001"
   
   # Terminal 2 (instance khác)
   cd Microservice.Services.UserService
   dotnet run --urls "http://localhost:5002"
   ```

2. **Cả 2 instances đều:**
   - Kết nối đến cùng RabbitMQ server
   - Lắng nghe cùng queue: `api.user.request`
   - Cả 2 đều có `RabbitMQConsumerService` đang chạy

3. **Khi API Gateway gửi requests:**
   ```
   Request 1 → Queue → UserService Instance 1 nhận
   Request 2 → Queue → UserService Instance 2 nhận
   Request 3 → Queue → UserService Instance 1 nhận
   Request 4 → Queue → UserService Instance 2 nhận
   ```

4. **RabbitMQ đảm bảo:**
   - Mỗi message chỉ được gửi cho **1 consumer**
   - Phân phối đều giữa các consumers
   - Nếu 1 instance chết, messages sẽ được gửi cho instance còn lại

### 📊 Ví Dụ Cụ Thể

#### Scenario: 10 requests đến `/api/users`

```
Request 1 → Queue → UserService Instance 1 (xử lý)
Request 2 → Queue → UserService Instance 2 (xử lý)
Request 3 → Queue → UserService Instance 1 (xử lý)
Request 4 → Queue → UserService Instance 2 (xử lý)
Request 5 → Queue → UserService Instance 1 (xử lý)
Request 6 → Queue → UserService Instance 2 (xử lý)
Request 7 → Queue → UserService Instance 1 (xử lý)
Request 8 → Queue → UserService Instance 2 (xử lý)
Request 9 → Queue → UserService Instance 1 (xử lý)
Request 10 → Queue → UserService Instance 2 (xử lý)
```

**Kết quả:** Mỗi instance xử lý 5 requests (50/50)

### ⚙️ Tối Ưu Hóa: Prefetch Count

Để tối ưu performance, đã cấu hình **prefetch count** trong Consumer Services:

```csharp
// Trong RabbitMQConsumerService constructor
_channel.BasicQos(
    prefetchSize: 0,      // Không giới hạn size
    prefetchCount: 1,     // Chỉ nhận 1 message chưa ack tại một thời điểm
    global: false         // Áp dụng cho consumer này
);
```

**Lợi ích:**
- Đảm bảo phân phối đều (fair dispatch)
- Tránh tình trạng 1 instance nhận quá nhiều messages trong khi instance khác rảnh

### 🎨 Kiến Trúc Tổng Quan

```
                    ┌─────────────────────┐
                    │  API Gateway        │
                    │  RabbitMQ           │
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │  api.user.request    │
                    │      Queue           │
                    └──────────┬──────────┘
                               │
                ┌──────────────┴──────────────┐
                │                             │
        ┌───────▼────────┐          ┌───────▼────────┐
        │ UserService     │          │ UserService     │
        │ Instance 1      │          │ Instance 2      │
        │ (Consumer 1)    │          │ (Consumer 2)    │
        │                 │          │                 │
        │ Port: 5001      │          │ Port: 5002      │
        │ Process: PID1   │          │ Process: PID2   │
        └─────────────────┘          └─────────────────┘
```

### 🔍 Kiểm Tra Load Balancing

#### 1. Xem logs của mỗi instance:

**UserService Instance 1:**
```
info: Received request: GET /api/users, CorrelationId: abc-123
info: Received request: GET /api/users, CorrelationId: def-456
```

**UserService Instance 2:**
```
info: Received request: GET /api/users, CorrelationId: ghi-789
info: Received request: GET /api/users, CorrelationId: jkl-012
```

#### 2. Sử dụng RabbitMQ Management UI:
- Truy cập: `http://47.130.33.106:15672`
- Xem queue `api.user.request`
- Kiểm tra số lượng consumers đang kết nối
- Xem message rate distribution

### 💡 Lợi Ích

1. **Tự động Load Balancing**: Không cần cấu hình thêm
2. **High Availability**: Nếu 1 instance chết, instance còn lại vẫn hoạt động
3. **Scalability**: Dễ dàng thêm/bớt instances
4. **Fair Distribution**: RabbitMQ đảm bảo phân phối đều

### ⚠️ Lưu Ý

1. **Database Connection**: Mỗi instance cần connection string riêng hoặc dùng connection pooling
2. **State Management**: Không nên lưu state trong memory (dùng shared cache như Redis)
3. **Logging**: Nên có correlation ID để trace requests qua các instances
4. **Health Checks**: Mỗi instance nên có health check endpoint

### 🚀 Cách Test

1. **Khởi động 2 UserService instances:**
   ```powershell
   # Terminal 1
   cd Microservice.Services.UserService
   dotnet run --urls "http://localhost:5001"
   
   # Terminal 2
   cd Microservice.Services.UserService
   dotnet run --urls "http://localhost:5002"
   ```

2. **Gửi nhiều requests:**
   ```powershell
   for ($i=1; $i -le 10; $i++) {
       Invoke-WebRequest -Uri "http://localhost:5010/api/users" -Method GET
   }
   ```

3. **Kiểm tra logs** của cả 2 instances để thấy requests được phân phối

### 📝 Tóm Lại

**RabbitMQ tự động load balance** khi có nhiều consumers lắng nghe cùng một queue:
- ✅ Round-robin distribution
- ✅ Fair dispatch
- ✅ High availability
- ✅ Không cần cấu hình thêm

Chỉ cần khởi động nhiều instances của cùng service, tất cả sẽ tự động lắng nghe cùng queue và nhận requests!

---

## 4. Hướng Dẫn Restart Service

### Vấn đề

API Gateway RabbitMQ gửi request đến queue `api.user.request` nhưng không nhận được response, dẫn đến timeout 504.

### Nguyên nhân

Service chưa được **RESTART** sau khi thêm RabbitMQ Consumer Service. Consumer chỉ khởi động khi service start.

### Giải pháp

#### Bước 1: Dừng Service hiện tại
1. Tìm terminal/console đang chạy Service (UserService, OrderService, ProductService)
2. Nhấn `Ctrl+C` để dừng service

#### Bước 2: Khởi động lại Service
```powershell
# Ví dụ với UserService
cd Microservice.Services.UserService
dotnet run
```

#### Bước 3: Kiểm tra logs
Khi Service khởi động, bạn **PHẢI** thấy các logs sau:

```
info: Connecting to RabbitMQ at 47.130.33.106:5672...
info: RabbitMQ connection established successfully
info: QoS configured: prefetchCount=1 for fair dispatch
info: Request queue declared: api.user.request
info: Response queue declared: api.gateway.response
info: RabbitMQ Consumer Service initialized successfully. Ready to listen on queue: api.user.request
info: Starting RabbitMQ Consumer Service...
info: Successfully started consuming messages from queue: api.user.request. Consumer is ready to receive requests.
```

#### Bước 4: Nếu không thấy logs trên
Có thể có lỗi:
- **RabbitMQ không kết nối được**: Kiểm tra RabbitMQ server có đang chạy không (47.130.33.106:5672)
- **Lỗi khác**: Xem logs chi tiết để biết lỗi cụ thể

#### Bước 5: Test lại API
Sau khi Service khởi động thành công với consumer, test lại API từ frontend.

### Lưu ý quan trọng

- **Mỗi khi thêm/sửa consumer service, PHẢI restart service đó**
- Consumer service chạy như một `IHostedService`, tự động start khi service khởi động
- Nếu không restart, consumer sẽ không chạy và requests sẽ bị timeout

### Danh Sách Services Cần Restart

Sau khi thêm RabbitMQ Consumer Service, cần restart các services sau:
- ✅ UserService
- ✅ OrderService
- ✅ ProductService
- ✅ API Gateway RabbitMQ (nếu có thay đổi)

---

## 5. Queue `order.created` - Event Queue

### 🎯 Queue `order.created` Là Gì?

Queue `order.created` là một **Event Queue** trong kiến trúc **Event-Driven Architecture (EDA)**.

#### Mục Đích

Khi một đơn hàng mới được tạo, OrderService sẽ **publish một event** vào queue `order.created` để thông báo cho các services khác biết rằng có đơn hàng mới.

### 📍 Nơi Publish Event

**File:** `Microservice.Services.OrderService/Services/OrderService.cs`

```csharp
// Sau khi tạo đơn hàng thành công
var orderCreatedEvent = new MessageEvent
{
    EventType = "OrderCreated",
    ServiceName = "OrderService",
    Data = new
    {
        OrderId = order.Id,
        UserId = order.UserId,
        TotalAmount = order.TotalAmount,
        OrderItems = order.OrderItems.Select(oi => new
        {
            ProductId = oi.ProductId,
            Quantity = oi.Quantity
        })
    }
};

_messagePublisher.Publish(orderCreatedEvent, "order.created");
```

**Khi nào:** Mỗi khi có đơn hàng mới được tạo thành công

### 🔄 Cách Hoạt Động

#### Luồng Event:

```
OrderService (Tạo đơn hàng)
        ↓
   Publish Event
        ↓
[order.created Queue]
        ↓
   (Chờ Consumer)
```

#### Event Data Structure:

```json
{
  "eventType": "OrderCreated",
  "serviceName": "OrderService",
  "timestamp": "2025-12-30T19:30:00Z",
  "data": {
    "orderId": 1,
    "userId": 1,
    "totalAmount": 500000,
    "orderItems": [
      {
        "productId": 1,
        "quantity": 2
      }
    ]
  }
}
```

### 💡 Mục Đích Sử Dụng

Queue `order.created` có thể được sử dụng bởi các services khác để:

#### 1. **Notification Service** (Gửi thông báo)
- Gửi email xác nhận đơn hàng cho khách hàng
- Gửi SMS thông báo
- Push notification

#### 2. **Analytics Service** (Phân tích dữ liệu)
- Thống kê số lượng đơn hàng
- Phân tích doanh thu
- Báo cáo theo thời gian

#### 3. **Inventory Service** (Quản lý tồn kho)
- Cập nhật tồn kho (hiện tại đã làm trực tiếp qua HTTP)
- Có thể dùng như backup mechanism

#### 4. **Logging/Auditing Service**
- Ghi log events vào database
- Audit trail cho compliance
- Lưu vào MongoDB (đã có sẵn)

#### 5. **Payment Service** (Thanh toán)
- Xử lý thanh toán khi có đơn hàng mới
- Tạo invoice

#### 6. **Shipping Service** (Vận chuyển)
- Tạo đơn vận chuyển
- Thông báo cho bộ phận logistics

### ⚠️ Tình Trạng Hiện Tại

Theo RabbitMQ Management UI, bạn có **3 messages** trong queue `order.created`:
- Có nghĩa là đã có 3 đơn hàng được tạo
- **Chưa có consumer nào lắng nghe** queue này
- Messages đang chờ trong queue

### 🔧 Nếu Muốn Sử Dụng Queue Này

#### Option 1: Tạo Consumer Service

Có thể tạo một service riêng để consume events từ `order.created`:

```csharp
// Ví dụ: NotificationService
public class OrderEventConsumer : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Lắng nghe queue order.created
        _channel.BasicConsume(
            queue: "order.created",
            autoAck: false,
            consumer: consumer);
    }
    
    private void HandleOrderCreated(MessageEvent event)
    {
        // Gửi email cho khách hàng
        // Hoặc cập nhật analytics
        // Hoặc log vào database
    }
}
```

#### Option 2: Xóa Queue (Nếu Không Dùng)

Nếu không cần sử dụng event này, có thể:
- Xóa code publish event trong OrderService
- Hoặc để đó (không ảnh hưởng gì, chỉ tốn một chút memory)

### 📊 So Sánh Với Request Queue

| Khía Cạnh | `api.order.request` | `order.created` |
|-----------|---------------------|-----------------|
| **Mục đích** | Request/Response (API calls) | Event notification |
| **Consumer** | RabbitMQConsumerService | Chưa có (có thể tạo) |
| **Pattern** | Request-Reply | Publish-Subscribe |
| **Sử dụng** | API Gateway → OrderService | OrderService → Other Services |
| **Đồng bộ** | Có (đợi response) | Không (fire and forget) |

### 🎯 Tóm Lại

**Queue `order.created`:**
- ✅ **Mục đích**: Event notification khi có đơn hàng mới
- ✅ **Publish từ**: OrderService khi tạo đơn hàng
- ✅ **Sử dụng cho**: Notification, Analytics, Logging, etc.
- ⚠️ **Hiện tại**: Chưa có consumer, messages đang chờ trong queue
- 💡 **Có thể**: Tạo consumer service để xử lý events hoặc xóa nếu không dùng

Đây là pattern **Event-Driven Architecture** - cho phép các services giao tiếp bất đồng bộ thông qua events!

---

## 📚 Tài Liệu Tham Khảo

- RabbitMQ Documentation: https://www.rabbitmq.com/documentation.html
- Work Queue Pattern: https://www.rabbitmq.com/tutorials/tutorial-two-dotnet.html
- Fair Dispatch: https://www.rabbitmq.com/consumer-prefetch.html
- Event-Driven Architecture: https://www.rabbitmq.com/tutorials/tutorial-three-dotnet.html

---

**Tài liệu này tổng hợp tất cả các giải thích về API Gateway RabbitMQ trong dự án.**

