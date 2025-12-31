# 📋 Hướng Dẫn Viết Báo Cáo Dự Án Microservice

Tài liệu này hướng dẫn cách viết báo cáo dự án theo cấu trúc chuẩn, giúp trình bày đầy đủ và chuyên nghiệp về hệ thống E-Commerce Backend theo kiến trúc Microservice.

---

## 📑 Cấu Trúc Báo Cáo

Báo cáo được chia thành 3 phần chính:

1. **PHẦN I: PHÂN TÍCH YÊU CẦU** - Xác định bài toán và tiêu chuẩn hệ thống
2. **PHẦN II: LIỆT KÊ TÍNH NĂNG & TỔNG HỢP HỆ THỐNG** - Mô tả hiện trạng và thông số kỹ thuật
3. **PHẦN III: THIẾT KẾ PHẦN MỀM** - Chi tiết kỹ thuật và kiến trúc

---

# PHẦN I: PHÂN TÍCH YÊU CẦU

## 1.1. Bài Toán Đặt Ra

### Bối Cảnh

Trong thời đại thương mại điện tử phát triển mạnh mẽ, các hệ thống E-Commerce truyền thống gặp nhiều thách thức:

- **Vấn đề Monolithic:** Hệ thống lớn, khó bảo trì, một lỗi có thể ảnh hưởng toàn bộ
- **Khó Scale:** Không thể scale từng phần riêng biệt, phải scale toàn bộ hệ thống
- **Rủi ro cao:** Một service lỗi có thể làm sập cả hệ thống
- **Phát triển chậm:** Nhiều team làm việc trên cùng một codebase, dễ conflict
- **Technology lock-in:** Khó thay đổi công nghệ cho từng phần

### Thách Thức Hiện Tại

1. **Xử lý đồng thời:** Hệ thống cần xử lý nhiều requests cùng lúc từ nhiều users
2. **Tính nhất quán dữ liệu:** Đảm bảo dữ liệu đồng bộ giữa các services
3. **Xử lý bất đồng bộ:** Một số tác vụ như tạo đơn hàng cần xử lý async để không block
4. **Logging và Monitoring:** Cần theo dõi hoạt động của từng service
5. **Deployment:** Cần deploy độc lập từng service

### Lý Do Cần Xây Dựng Hệ Thống

- **Kiến trúc Microservice:** Chia nhỏ hệ thống thành các services độc lập, dễ quản lý
- **Trung gian tin cậy:** API Gateway đóng vai trò single entry point, đảm bảo an toàn
- **Xử lý bất đồng bộ:** Sử dụng RabbitMQ để xử lý events, không block main flow
- **Database per Service:** Mỗi service có database riêng, đảm bảo tính độc lập
- **Scalability:** Có thể scale từng service theo nhu cầu thực tế

---

## 1.2. Yêu Cầu Chức Năng

### Actor (Tác Nhân)

Hệ thống có các actor chính:

1. **Customer (Khách hàng)** - Người dùng cuối sử dụng hệ thống
2. **Admin (Quản trị viên)** - Quản lý hệ thống
3. **System (Hệ thống)** - Các service tự động xử lý

### Module 1: Quản Lý Người Dùng (User Service)

#### UC-000: Đăng Nhập

- **Actor:** Customer
- **Input:** 
  - Username (hoặc Email), Password
- **Output:** 
  - JWT Token
  - Refresh Token
  - User object với thông tin đầy đủ
  - ExpiresAt (thời gian hết hạn token)
  - Status code 200 (OK) hoặc 401 (Unauthorized)
- **Business Rules:**
  - Validate username/email và password
  - Password được hash bằng BCrypt, so sánh với PasswordHash trong database
  - Nếu đúng, generate JWT token với claims: UserId, Username, Email, Role
  - Token có thời gian hết hạn (mặc định 60 phút)
  - Generate refresh token (GUID)
  - Log vào MongoDB
  - Trả về token và user info (không bao gồm password hash)

#### UC-001: Đăng Ký Tài Khoản

- **Actor:** Customer
- **Input:** 
  - Username, Email, Password
  - FirstName, LastName, PhoneNumber (optional)
- **Output:** 
  - JWT Token (tự động đăng nhập sau khi đăng ký)
  - Refresh Token
  - User object với Id và thông tin đầy đủ
  - ExpiresAt
  - Status code 201 (Created) hoặc 400 (Bad Request)
- **Business Rules:**
  - Username phải unique
  - Email phải unique và đúng format
  - Password phải được hash bằng BCrypt trước khi lưu
  - Tự động set IsActive = true, Role = "Customer"
  - Tự động tạo CreatedAt = DateTime.UtcNow
  - Sau khi tạo user thành công, tự động đăng nhập và trả về JWT token
  - Log vào MongoDB

#### UC-002: Xem Danh Sách Users

- **Actor:** Admin, System
- **Input:** 
  - Query parameters: page, pageSize, search (optional)
- **Output:** 
  - Danh sách users (pagination)
  - Total count
- **Business Rules:**
  - Chỉ trả về users có IsDeleted = false
  - Mặc định pageSize = 10, page = 1
  - Có thể search theo username, email

#### UC-003: Xem Chi Tiết User

- **Actor:** Customer (chính mình), Admin
- **Input:** 
  - UserId (int)
- **Output:** 
  - User object đầy đủ thông tin
  - Status 200 hoặc 404 (Not Found)
- **Business Rules:**
  - Không trả về password hash
  - Customer chỉ xem được thông tin của chính mình (cần authentication)

#### UC-004: Cập Nhật Thông Tin User

- **Actor:** Customer (chính mình), Admin
- **Input:** 
  - UserId, các field cần update (FirstName, LastName, PhoneNumber, etc.)
- **Output:** 
  - User object đã được update
  - Status 200 hoặc 404
- **Business Rules:**
  - Không cho phép update Username, Email (cần chức năng riêng)
  - Tự động set UpdatedAt = DateTime.UtcNow
  - Log action vào MongoDB

#### UC-005: Xóa User (Soft Delete)

- **Actor:** Admin
- **Input:** 
  - UserId
- **Output:** 
  - Status 200 (OK) hoặc 404
- **Business Rules:**
  - Không xóa thật, chỉ set IsDeleted = true
  - Cập nhật UpdatedAt
  - Log vào MongoDB

#### UC-006: Quản Lý Địa Chỉ Người Dùng

- **Actor:** Customer (chính mình), Admin
- **Input:** 
  - UserId, Address information (FullName, PhoneNumber, Street, City, State, PostalCode, Country, IsDefault)
- **Output:** 
  - UserAddress object
  - Status 200, 201, hoặc 404
- **Business Rules:**
  - Customer chỉ quản lý địa chỉ của chính mình
  - Có thể có nhiều địa chỉ, nhưng chỉ một địa chỉ có IsDefault = true
  - Khi thêm địa chỉ mới với IsDefault = true, tự động set các địa chỉ khác thành false
  - Log vào MongoDB

---

### Module 2: Quản Lý Sản Phẩm (Product Service)

#### UC-007: Xem Danh Sách Sản Phẩm

- **Actor:** Customer, Admin
- **Input:** 
  - Query parameters: page, pageSize, category (optional), search (optional)
- **Output:** 
  - Danh sách products với pagination
  - Total count
- **Business Rules:**
  - Chỉ trả về products có IsAvailable = true và IsDeleted = false
  - Có thể filter theo category
  - Có thể search theo tên, description

#### UC-008: Xem Chi Tiết Sản Phẩm

- **Actor:** Customer, Admin
- **Input:** 
  - ProductId
- **Output:** 
  - Product object đầy đủ (bao gồm reviews, tags)
  - Status 200 hoặc 404
- **Business Rules:**
  - Bao gồm thông tin reviews và ratings
  - Tính average rating từ reviews

#### UC-009: Tạo Sản Phẩm Mới

- **Actor:** Admin
- **Input:** 
  - Name, Description, Price, Stock, Category, ImageUrl (optional)
- **Output:** 
  - Product object với Id
  - Status 201 hoặc 400
- **Business Rules:**
  - Name không được trống
  - Price phải > 0
  - Stock >= 0
  - Category phải có giá trị
  - Tự động set IsAvailable = true
  - Log vào MongoDB

#### UC-010: Cập Nhật Sản Phẩm

- **Actor:** Admin
- **Input:** 
  - ProductId, các field cần update
- **Output:** 
  - Product object đã update
  - Status 200 hoặc 404
- **Business Rules:**
  - Có thể update tất cả fields trừ Id
  - Nếu Stock = 0, tự động set IsAvailable = false
  - Log vào MongoDB

#### UC-011: Cập Nhật Tồn Kho

- **Actor:** System (từ Order Service), Admin
- **Input:** 
  - ProductId, Quantity (số lượng thay đổi, có thể âm)
- **Output:** 
  - Product object với Stock mới
  - Status 200 hoặc 404 hoặc 400 (nếu Stock < 0)
- **Business Rules:**
  - Stock mới = Stock cũ + Quantity
  - Nếu Stock < 0, trả về lỗi
  - Nếu Stock = 0, set IsAvailable = false
  - Log vào MongoDB

#### UC-012: Xóa Sản Phẩm

- **Actor:** Admin
- **Input:** 
  - ProductId
- **Output:** 
  - Status 200 hoặc 404
- **Business Rules:**
  - Soft delete (IsDeleted = true)
  - Không cho phép xóa nếu có orders đang sử dụng (cần check từ Order Service)

---

### Module 3: Quản Lý Đơn Hàng (Order Service)

#### UC-013: Tạo Đơn Hàng Mới

- **Actor:** Customer
- **Input:** 
  - UserId, ShippingAddress
  - OrderItems: [{ProductId, Quantity}]
  - PaymentMethod (optional)
- **Output:** 
  - Order object với Id
  - Status 201 hoặc 400
- **Business Rules:**
  - Validate UserId tồn tại (gọi User Service)
  - Validate ProductId và Stock đủ (gọi Product Service)
  - Tính TotalAmount = sum(UnitPrice * Quantity) của tất cả items
  - Tự động set Status = "Pending"
  - Tự động set PaymentStatus = "Pending"
  - Tạo OrderItems với ProductName, UnitPrice từ Product Service
  - **Publish event "order.created" vào RabbitMQ**
  - Log vào MongoDB
  - **Xử lý bất đồng bộ:** Sau khi tạo order, publish event để các service khác xử lý (ví dụ: gửi email, cập nhật inventory)

#### UC-014: Xem Danh Sách Đơn Hàng

- **Actor:** Admin
- **Input:** 
  - Query parameters: page, pageSize, status (optional)
- **Output:** 
  - Danh sách orders với pagination
  - Total count
- **Business Rules:**
  - Chỉ trả về orders có IsDeleted = false
  - Có thể filter theo status
  - Sắp xếp theo CreatedAt DESC

#### UC-015: Xem Đơn Hàng Theo User

- **Actor:** Customer (chính mình), Admin
- **Input:** 
  - UserId
- **Output:** 
  - Danh sách orders của user đó
  - Status 200 hoặc 404 (nếu user không tồn tại)
- **Business Rules:**
  - Validate UserId tồn tại
  - Customer chỉ xem được orders của chính mình

#### UC-016: Cập Nhật Trạng Thái Đơn Hàng

- **Actor:** Admin, System
- **Input:** 
  - OrderId, Status mới, Notes (optional)
- **Output:** 
  - Order object đã update
  - Status 200 hoặc 404 hoặc 400 (nếu status không hợp lệ)
- **Business Rules:**
  - Status phải hợp lệ: Pending → Processing → Shipped → Delivered
  - Hoặc có thể Cancelled từ bất kỳ trạng thái nào
  - Tự động cập nhật ShippedDate nếu status = "Shipped"
  - Tự động cập nhật DeliveredDate nếu status = "Delivered"
  - **Publish event "order.status.updated" vào RabbitMQ**
  - Lưu vào StatusHistory
  - Log vào MongoDB

#### UC-017: Xóa Đơn Hàng

- **Actor:** Admin
- **Input:** 
  - OrderId
- **Output:** 
  - Status 200 hoặc 404
- **Business Rules:**
  - Soft delete (IsDeleted = true)
  - Chỉ cho phép xóa nếu status = "Cancelled" hoặc "Pending"

---

### Module 4: API Gateway

#### UC-018: Điều Hướng Requests

- **Actor:** System
- **Input:** 
  - HTTP Request từ Client
- **Output:** 
  - HTTP Response từ Microservice tương ứng
- **Business Rules:**
  - Route `/api/auth/*` → User Service (port 5001) - Authentication endpoints
  - Route `/api/users/*` → User Service (port 5001) - User management endpoints
  - Route `/api/products/*` → Product Service (port 5002)
  - Route `/api/orders/*` → Order Service (port 5003)
  - Xử lý CORS
  - Load balancing nếu có nhiều instances
  - Timeout handling

---

## 1.3. Yêu Cầu Phi Chức Năng

### Hiệu Năng (Performance)

1. **Thời gian phản hồi:**
   - API Gateway: < 50ms (chỉ routing, không xử lý logic)
   - User Service: < 200ms cho các operations đơn giản
   - Product Service: < 200ms cho read operations
   - Order Service: < 500ms cho create order (bao gồm validation từ services khác)

2. **Khả năng chịu tải:**
   - Hỗ trợ ít nhất 100 concurrent users
   - Mỗi service có thể xử lý ít nhất 1000 requests/phút
   - Database connection pool: tối thiểu 10 connections

3. **Throughput:**
   - API Gateway: ít nhất 5000 requests/phút
   - Mỗi microservice: ít nhất 2000 requests/phút

### Khả Năng Mở Rộng (Scalability)

1. **Scale Horizontal:**
   - Mỗi service có thể chạy nhiều instances
   - API Gateway hỗ trợ load balancing
   - Stateless services (không lưu session state)

2. **Tách Biệt Database:**
   - Mỗi service có database riêng (Database per Service pattern)
   - Không có shared database giữa các services
   - Có thể scale database độc lập

3. **Message Queue:**
   - RabbitMQ hỗ trợ multiple consumers
   - Có thể scale consumers để xử lý nhiều messages hơn

### Độ Tin Cậy (Reliability)

1. **Tính Toàn Vẹn Dữ Liệu (ACID):**
   - Mỗi database transaction đảm bảo ACID
   - Order Service: Khi tạo order, phải đảm bảo:
     - Order được tạo thành công
     - OrderItems được tạo thành công
     - Nếu có lỗi, rollback toàn bộ

2. **Cơ Chế Retry:**
   - Khi gọi service khác bị lỗi, retry tối đa 3 lần
   - Exponential backoff: 1s, 2s, 4s
   - Nếu vẫn lỗi sau 3 lần, trả về lỗi cho client

3. **Không Mất Tin Nhắn:**
   - RabbitMQ sử dụng durable queues
   - Messages được acknowledge sau khi xử lý xong
   - Nếu consumer crash, message sẽ được redeliver

4. **Fault Tolerance:**
   - Nếu một service down, các service khác vẫn hoạt động
   - API Gateway có thể handle service unavailable (trả về 503)

### Bảo Mật & Vận Hành

1. **Xác Thực:**
   - ✅ **JWT (JSON Web Token)** - Đã được triển khai
     - Token được generate sau khi đăng nhập thành công
     - Token chứa claims: UserId, Username, Email, Role
     - Token có thời gian hết hạn (mặc định 60 phút)
     - Refresh token được generate để làm mới token
     - Token được gửi trong header: `Authorization: Bearer <token>`
   - ✅ Password được hash bằng BCrypt (cost factor = 12)
   - ✅ Không lưu plain password
   - ✅ Frontend tự động thêm token vào tất cả API requests qua HTTP Interceptor

2. **Log Tập Trung:**
   - Tất cả services log vào MongoDB
   - Format: Timestamp, Service Name, Level, Message, Exception (nếu có)
   - Có thể query và phân tích logs

3. **Deploy bằng Docker:**
   - Mỗi service có Dockerfile riêng
   - Sử dụng Docker Compose để orchestrate
   - Multi-stage build để giảm image size

4. **Monitoring:**
   - Health check endpoints cho mỗi service
   - Có thể monitor resource usage (CPU, Memory)
   - Log errors và exceptions

5. **CORS:**
   - Cấu hình CORS trong API Gateway
   - Chỉ cho phép requests từ frontend domain

---

# PHẦN II: LIỆT KÊ TÍNH NĂNG & TỔNG HỢP HỆ THỐNG

## 2.1. Mô Tả Chi Tiết Các Dịch Vụ (Services)

### Service 1: User Service

**Mục đích:** Quản lý thông tin người dùng, authentication, authorization

**Port:** 5001

**Database:** `userservice_db` (PostgreSQL)

**MongoDB Collection:** `microservice_users.user_logs`

**API Endpoints:**

| Method | Endpoint | Mô Tả | Input | Output |
|--------|----------|-------|-------|--------|
| **Authentication** |
| POST | `/api/auth/login` | Đăng nhập | Body: {username, password} | LoginResponseDto (Token, RefreshToken, User, ExpiresAt) |
| POST | `/api/auth/register` | Đăng ký | Body: CreateUserDto | LoginResponseDto (tự động đăng nhập) |
| **User Management** |
| GET | `/api/users` | Danh sách users | Query: page, pageSize, search | List<UserDto>, TotalCount |
| GET | `/api/users/{id}` | Chi tiết user | Path: id | UserDto |
| POST | `/api/users` | Tạo user mới | Body: CreateUserRequest | UserDto, Status 201 |
| PUT | `/api/users/{id}` | Cập nhật user | Path: id, Body: UpdateUserRequest | UserDto |
| DELETE | `/api/users/{id}` | Xóa user (soft) | Path: id | Status 200 |
| **User Addresses** |
| GET | `/api/users/{userId}/addresses` | Danh sách địa chỉ | Path: userId | List<UserAddressDto> |
| POST | `/api/users/{userId}/addresses` | Thêm địa chỉ | Path: userId, Body: CreateUserAddressDto | UserAddressDto, Status 201 |
| PUT | `/api/users/{userId}/addresses/{addressId}` | Cập nhật địa chỉ | Path: userId, addressId, Body: UpdateUserAddressDto | UserAddressDto |
| DELETE | `/api/users/{userId}/addresses/{addressId}` | Xóa địa chỉ | Path: userId, addressId | Status 200 |

**Background Services:**
- **RabbitMQConsumerService:** Lắng nghe events từ RabbitMQ (có thể mở rộng để xử lý user-related events)

**Dependencies:**
- PostgreSQL (userservice_db)
- MongoDB (logging)
- Microservice.Common (BaseEntity, shared models)
- JWT Authentication (Microsoft.AspNetCore.Authentication.JwtBearer)
- System.IdentityModel.Tokens.Jwt

---

### Service 2: Product Service

**Mục đích:** Quản lý sản phẩm, tồn kho, categories

**Port:** 5002

**Database:** `productservice_db` (PostgreSQL)

**MongoDB Collection:** `microservice_products.product_logs`

**API Endpoints:**

| Method | Endpoint | Mô Tả | Input | Output |
|--------|----------|-------|-------|--------|
| GET | `/api/products` | Danh sách products | Query: page, pageSize, category, search | List<ProductDto>, TotalCount |
| GET | `/api/products/{id}` | Chi tiết product | Path: id | ProductDto |
| GET | `/api/products/category/{category}` | Lọc theo category | Path: category | List<ProductDto> |
| POST | `/api/products` | Tạo product mới | Body: CreateProductRequest | ProductDto, Status 201 |
| PUT | `/api/products/{id}` | Cập nhật product | Path: id, Body: UpdateProductRequest | ProductDto |
| PATCH | `/api/products/{id}/stock` | Cập nhật stock | Path: id, Body: {quantity: int} | ProductDto |
| DELETE | `/api/products/{id}` | Xóa product (soft) | Path: id | Status 200 |

**Background Services:**
- Không có background services (có thể mở rộng để sync inventory)

**Dependencies:**
- PostgreSQL (productservice_db)
- MongoDB (logging)
- Microservice.Common

---

### Service 3: Order Service

**Mục đích:** Quản lý đơn hàng, xử lý orders, tích hợp với các services khác

**Port:** 5003

**Database:** `orderservice_db` (PostgreSQL)

**MongoDB Collection:** `microservice_orders.order_events`

**RabbitMQ:**
- **Publishes:** 
  - `order.created` - Khi tạo order mới
  - `order.status.updated` - Khi cập nhật status
- **Consumes:** (có thể mở rộng)

**API Endpoints:**

| Method | Endpoint | Mô Tả | Input | Output |
|--------|----------|-------|-------|--------|
| GET | `/api/orders` | Danh sách orders | Query: page, pageSize, status | List<OrderDto>, TotalCount |
| GET | `/api/orders/{id}` | Chi tiết order | Path: id | OrderDto |
| GET | `/api/orders/user/{userId}` | Orders của user | Path: userId | List<OrderDto> |
| POST | `/api/orders` | Tạo order mới | Body: CreateOrderRequest | OrderDto, Status 201 |
| PUT | `/api/orders/{id}/status` | Cập nhật status | Path: id, Body: {status: string} | OrderDto |
| DELETE | `/api/orders/{id}` | Xóa order (soft) | Path: id | Status 200 |

**Background Services:**
- **RabbitMQService:** Publish events vào RabbitMQ
- **RabbitMQConsumerService:** Consume events từ RabbitMQ (có thể mở rộng)

**Dependencies:**
- PostgreSQL (orderservice_db)
- MongoDB (logging)
- RabbitMQ (message queue)
- User Service (HTTP call để validate user)
- Product Service (HTTP call để validate product và stock)
- Microservice.Common

---

### Service 4: API Gateway (Ocelot)

**Mục đích:** Single entry point, điều hướng requests, load balancing

**Port:** 5000

**Database:** Không có (stateless)

**API Endpoints:**

| Method | Endpoint | Routes To | Mô Tả |
|--------|----------|-----------|-------|
| GET | `/swagger` | - | Swagger UI tổng hợp |
| GET | `/health` | - | Health check |
| * | `/api/auth/*` | User Service:5001 | Route authentication requests (login, register) |
| * | `/api/users/*` | User Service:5001 | Route tất cả user requests |
| * | `/api/products/*` | Product Service:5002 | Route tất cả product requests |
| * | `/api/orders/*` | Order Service:5003 | Route tất cả order requests |

**Background Services:**
- Không có

**Dependencies:**
- Ocelot library
- Các microservices (để route)

**Cấu hình:** File `ocelot.json` định nghĩa routes

---

### Service 5: API Gateway RabbitMQ (Optional)

**Mục đích:** API Gateway sử dụng RabbitMQ để điều hướng requests bất đồng bộ

**Port:** 5010

**RabbitMQ:**
- **Publishes:** Requests từ client vào queues
- **Consumes:** Responses từ services

**API Endpoints:**

| Method | Endpoint | Mô Tả |
|--------|----------|-------|
| POST | `/api/gateway/users` | Gửi request vào RabbitMQ queue cho User Service |
| POST | `/api/gateway/products` | Gửi request vào RabbitMQ queue cho Product Service |
| POST | `/api/gateway/orders` | Gửi request vào RabbitMQ queue cho Order Service |

**Background Services:**
- **RabbitMQService:** Publish/Consume messages

---

## 2.2. Các Bảng Tổng Hợp

### Bảng 1: Tổng Quan Hệ Thống

| Thành Phần | Số Lượng | Mô Tả |
|------------|----------|-------|
| **Microservices** | 4 | User Service, Product Service, Order Service, API Gateway |
| **API Gateway** | 1 | Ocelot Gateway (có thể thêm RabbitMQ Gateway) |
| **Databases** | 3 | userservice_db, productservice_db, orderservice_db (PostgreSQL) |
| **Message Queues** | 1 | RabbitMQ (external server) |
| **Logging DB** | 1 | MongoDB Atlas (3 collections) |
| **Frontend** | 1 | Angular application |

---

### Bảng 2: Technology Stack

| Category | Technology | Version | Mục Đích |
|----------|------------|---------|----------|
| **Backend Framework** | .NET | 8.0 | Framework chính cho tất cả services |
| **ORM** | Entity Framework Core | 8.0 | Truy cập database |
| **Database** | PostgreSQL | Latest | Relational database cho mỗi service |
| **NoSQL Database** | MongoDB | Atlas | Logging và events storage |
| **Message Queue** | RabbitMQ | 3.x | Asynchronous communication |
| **API Gateway** | Ocelot | Latest | Routing và load balancing |
| **Frontend** | Angular | 17+ | Single Page Application |
| **UI Library** | Angular Material | Latest | UI components |
| **Containerization** | Docker | Latest | Containerization |
| **Orchestration** | Docker Compose | Latest | Multi-container management |
| **API Documentation** | Swagger | Latest | API documentation |
| **Password Hashing** | BCrypt | Latest | Security |
| **Authentication** | JWT | Latest | JWT token generation và validation |
| **Frontend Auth** | Angular Guards | 17+ | Route protection |
| **HTTP Interceptor** | Angular | 17+ | Auto-add JWT token to requests |

---

### Bảng 3: Phân Loại Chức Năng & Pattern

| Chức Năng | Pattern | Mô Tả | Service |
|-----------|---------|-------|---------|
| **Đăng ký user** | Synchronous | HTTP POST, trả về ngay kết quả | User Service |
| **Xem danh sách users** | Synchronous | HTTP GET, trả về ngay | User Service |
| **Tạo sản phẩm** | Synchronous | HTTP POST, trả về ngay | Product Service |
| **Cập nhật stock** | Synchronous | HTTP PATCH, trả về ngay | Product Service |
| **Tạo đơn hàng** | **Hybrid** | HTTP POST (sync) + RabbitMQ event (async) | Order Service |
| **Cập nhật status order** | **Hybrid** | HTTP PUT (sync) + RabbitMQ event (async) | Order Service |
| **Logging** | Asynchronous | Tất cả services log vào MongoDB async | All Services |
| **Event publishing** | Asynchronous | Order Service publish events vào RabbitMQ | Order Service |

**Giải thích:**
- **Synchronous:** Client gửi request và chờ response ngay
- **Asynchronous:** Xử lý trong background, không block main flow
- **Hybrid:** Kết hợp cả sync (trả response ngay) và async (publish event)

---

### Bảng 4: Database Schema Summary

#### userservice_db (PostgreSQL)

| Bảng | Mục Đích | Các Trường Chính |
|------|----------|------------------|
| **Users** | Lưu thông tin người dùng | Id, Username, Email, PasswordHash, FirstName, LastName, PhoneNumber, Role, AvatarUrl, IsActive, CreatedAt, UpdatedAt, IsDeleted |
| **UserAddresses** | Địa chỉ của user | Id, UserId, FullName, PhoneNumber, Street, City, State, PostalCode, Country, IsDefault |

#### productservice_db (PostgreSQL)

| Bảng | Mục Đích | Các Trường Chính |
|------|----------|------------------|
| **Products** | Lưu thông tin sản phẩm | Id, Name, Description, Price, Stock, Category, ImageUrl, IsAvailable, DiscountPrice, DiscountStartDate, DiscountEndDate, CreatedAt, UpdatedAt, IsDeleted |
| **ProductReviews** | Đánh giá sản phẩm | Id, ProductId, UserId, UserName, Rating, Comment, IsVerifiedPurchase |
| **ProductTags** | Tags của sản phẩm | Id, ProductId, TagName |

#### orderservice_db (PostgreSQL)

| Bảng | Mục Đích | Các Trường Chính |
|------|----------|------------------|
| **Orders** | Lưu thông tin đơn hàng | Id, UserId, TotalAmount, Status, ShippingAddress, PaymentMethod, PaymentStatus, PaymentTransactionId, PaymentDate, ShippingCarrier, TrackingNumber, ShippedDate, DeliveredDate, Notes, CreatedAt, UpdatedAt, IsDeleted |
| **OrderItems** | Chi tiết items trong order | Id, OrderId, ProductId, ProductName, Quantity, UnitPrice, SubTotal |
| **OrderStatusHistory** | Lịch sử thay đổi status | Id, OrderId, Status, Notes, ChangedBy, CreatedAt |

#### MongoDB Collections

| Database | Collection | Mục Đích |
|---------|-----------|----------|
| **microservice_users** | user_logs | Logs của User Service |
| **microservice_products** | product_logs | Logs của Product Service |
| **microservice_orders** | order_events | Events và logs của Order Service |

---

### Bảng 5: Port Mapping

| Service | Port | Protocol | Mô Tả |
|---------|------|----------|-------|
| **API Gateway** | 5000 | HTTP | Entry point chính |
| **User Service** | 5001 | HTTP | User management |
| **Product Service** | 5002 | HTTP | Product management |
| **Order Service** | 5003 | HTTP | Order management |
| **API Gateway RabbitMQ** | 5010 | HTTP | RabbitMQ-based gateway |
| **Frontend** | 4200 | HTTP | Angular dev server |
| **PostgreSQL** | 5432 | TCP | Database (external) |
| **RabbitMQ** | 5672 | TCP | Message queue (external) |
| **RabbitMQ Management** | 15672 | HTTP | RabbitMQ web UI (external) |
| **MongoDB** | 27017 | TCP | MongoDB (Atlas, external) |

**Lưu ý:** Các ports 5000-5010 được expose ra ngoài, các ports khác chỉ internal hoặc external server.

---

# PHẦN III: THIẾT KẾ PHẦN MỀM

## 3.1. Thiết Kế Tổng Thể

### 3.1.1. Kiến Trúc Layers

Hệ thống được chia thành các tầng (layers) như sau:

```
┌─────────────────────────────────────────────────────────┐
│              PRESENTATION LAYER                          │
│  - Angular Frontend (Port 4200)                         │
│  - Swagger UI (Port 5000, 5001, 5002, 5003)             │
└──────────────────┬──────────────────────────────────────┘
                   │ HTTP/REST
                   ▼
┌─────────────────────────────────────────────────────────┐
│              GATEWAY LAYER                               │
│  - API Gateway (Ocelot) - Port 5000                     │
│  - API Gateway RabbitMQ - Port 5010 (Optional)          │
│  - Routing, Load Balancing, CORS                        │
└──────────────────┬──────────────────────────────────────┘
                   │ HTTP/REST
        ┌──────────┴──────────┬──────────────┐
        │                     │              │
        ▼                     ▼              ▼
┌──────────────┐    ┌──────────────┐  ┌──────────────┐
│ SERVICE LAYER    │ SERVICE LAYER │  │ SERVICE LAYER│
│ User Service     │ Product       │  │ Order        │
│ (Port 5001)      │ Service       │  │ Service      │
│                  │ (Port 5002)   │  │ (Port 5003)  │
│ - Controllers    │ - Controllers │  │ - Controllers│
│ - Services       │ - Services    │  │ - Services   │
│ - Repositories   │ - Repositories│  │ - Repositories│
└──────┬──────────┘ └──────┬───────┘ └──────┬───────┘
       │                  │                │
       │                  │                │
       ▼                  ▼                ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ DATA LAYER   │  │ DATA LAYER    │  │ DATA LAYER   │
│ PostgreSQL   │  │ PostgreSQL    │  │ PostgreSQL   │
│ userservice_ │  │ productservice│  │ orderservice │
│    _db       │  │    _db        │  │    _db       │
└──────────────┘  └──────────────┘  └──────────────┘
       │                  │                │
       └──────────────────┴────────────────┘
                          │
        ┌─────────────────┴─────────────────┐
        │                                   │
        ▼                                   ▼
┌──────────────┐                  ┌──────────────┐
│ MESSAGE QUEUE│                  │ LOGGING      │
│ LAYER        │                  │ LAYER       │
│ RabbitMQ     │                  │ MongoDB     │
│ - Queues     │                  │ - Collections│
│ - Exchanges  │                  │ - Logs      │
└──────────────┘                  └──────────────┘
```

**Luồng Dữ Liệu:**

1. **Client Request Flow:**
   ```
   Frontend → API Gateway → Microservice → PostgreSQL
                                      ↓
                                  MongoDB (log)
   ```

2. **Event-Driven Flow:**
   ```
   Order Service → RabbitMQ Queue
                      ↓
              [Other Services consume]
                      ↓
                  MongoDB (log event)
   ```

3. **Service-to-Service Communication:**
   ```
   Order Service → HTTP Call → User Service
   Order Service → HTTP Call → Product Service
   ```

---

### 3.1.2. Design Patterns

#### Pattern 1: Microservices Architecture

**Mô tả:** Chia hệ thống thành các services nhỏ, độc lập

**Lợi ích:**
- Mỗi service có thể phát triển và deploy độc lập
- Dễ scale từng service theo nhu cầu
- Fault isolation: một service lỗi không ảnh hưởng services khác
- Có thể sử dụng công nghệ khác nhau cho từng service

**Áp dụng trong dự án:**
- User Service, Product Service, Order Service là các microservices độc lập

---

#### Pattern 2: Database per Service

**Mô tả:** Mỗi microservice có database riêng

**Lợi ích:**
- Tính độc lập: mỗi service quản lý dữ liệu của mình
- Có thể chọn database phù hợp cho từng service
- Dễ scale database độc lập
- Tránh tight coupling

**Áp dụng trong dự án:**
- `userservice_db` cho User Service
- `productservice_db` cho Product Service
- `orderservice_db` cho Order Service

---

#### Pattern 3: API Gateway

**Mô tả:** Single entry point cho tất cả client requests

**Lợi ích:**
- Client chỉ cần biết một endpoint
- Centralized cross-cutting concerns (CORS, authentication, logging)
- Load balancing
- Hiding internal architecture

**Áp dụng trong dự án:**
- Ocelot Gateway (Port 5000) đóng vai trò API Gateway

---

#### Pattern 4: Event-Driven Architecture

**Mô tả:** Services giao tiếp qua events/messages

**Lợi ích:**
- Loose coupling: services không cần biết nhau trực tiếp
- Asynchronous processing: không block main flow
- Scalability: có thể có nhiều consumers
- Resilience: messages được queue, không mất khi service down

**Áp dụng trong dự án:**
- Order Service publish events `order.created`, `order.status.updated` vào RabbitMQ
- Các services khác có thể subscribe để xử lý (ví dụ: gửi email, cập nhật inventory)

---

#### Pattern 5: Repository Pattern

**Mô tả:** Tách biệt data access logic khỏi business logic

**Lợi ích:**
- Dễ test: có thể mock repository
- Dễ thay đổi data source
- Centralized data access logic

**Áp dụng trong dự án:**
- Mỗi service có DbContext (Entity Framework) đóng vai trò repository
- Services sử dụng DbContext để truy cập database

---

## 3.2. Thiết Kế Chi Tiết (Diagrams)

### 3.2.1. ERD (Entity Relationship Diagrams)

#### ERD cho userservice_db

```
┌─────────────────────┐
│       Users         │
├─────────────────────┤
│ PK Id (int)         │
│ Username (string)   │
│ Email (string)      │
│ PasswordHash        │
│ FirstName           │
│ LastName            │
│ PhoneNumber         │
│ Role                │
│ IsActive (bool)     │
│ CreatedAt           │
│ UpdatedAt           │
│ IsDeleted (bool)     │
└──────────┬──────────┘
           │ 1
           │
           │ *
           ▼
┌─────────────────────┐
│   UserAddresses     │
├─────────────────────┤
│ PK Id (int)         │
│ FK UserId (int)     │
│ FullName            │
│ PhoneNumber         │
│ Street              │
│ City                │
│ State               │
│ PostalCode          │
│ Country             │
│ IsDefault (bool)    │
│ CreatedAt           │
│ UpdatedAt           │
│ IsDeleted (bool)     │
└─────────────────────┘
```

**Quan hệ:** User 1 ── * UserAddress (một user có nhiều địa chỉ)

---

#### ERD cho productservice_db

```
┌─────────────────────┐
│      Products       │
├─────────────────────┤
│ PK Id (int)         │
│ Name                │
│ Description         │
│ Price (decimal)     │
│ Stock (int)         │
│ Category            │
│ ImageUrl            │
│ IsAvailable (bool)  │
│ DiscountPrice       │
│ CreatedAt           │
│ UpdatedAt           │
│ IsDeleted (bool)     │
└──────────┬──────────┘
           │ 1
           │
           │ *
           ├──────────────────┐
           │                  │
           ▼                  ▼
┌─────────────────────┐  ┌─────────────────────┐
│  ProductReviews     │  │   ProductTags       │
├─────────────────────┤  ├─────────────────────┤
│ PK Id (int)         │  │ PK Id (int)         │
│ FK ProductId (int)  │  │ FK ProductId (int)  │
│ UserId (int)        │  │ TagName             │
│ UserName            │  │ CreatedAt           │
│ Rating (int)        │  │ UpdatedAt           │
│ Comment             │  │ IsDeleted (bool)     │
│ IsVerifiedPurchase  │  └─────────────────────┘
│ CreatedAt           │
│ UpdatedAt           │
│ IsDeleted (bool)     │
└─────────────────────┘
```

**Quan hệ:**
- Product 1 ── * ProductReview
- Product 1 ── * ProductTag

---

#### ERD cho orderservice_db

```
┌─────────────────────┐
│       Orders        │
├─────────────────────┤
│ PK Id (int)         │
│ UserId (int)        │
│ TotalAmount         │
│ Status              │
│ ShippingAddress     │
│ PaymentMethod       │
│ PaymentStatus       │
│ TrackingNumber      │
│ CreatedAt           │
│ UpdatedAt           │
│ IsDeleted (bool)     │
└──────────┬──────────┘
           │ 1
           │
           │ *
           ├──────────────────┐
           │                  │
           ▼                  ▼
┌─────────────────────┐  ┌─────────────────────┐
│    OrderItems       │  │ OrderStatusHistory   │
├─────────────────────┤  ├─────────────────────┤
│ PK Id (int)         │  │ PK Id (int)         │
│ FK OrderId (int)    │  │ FK OrderId (int)    │
│ ProductId (int)     │  │ Status              │
│ ProductName         │  │ Notes               │
│ Quantity (int)      │  │ ChangedBy           │
│ UnitPrice           │  │ CreatedAt           │
│ SubTotal           │  │ UpdatedAt           │
│ CreatedAt           │  │ IsDeleted (bool)     │
│ UpdatedAt           │  └─────────────────────┘
│ IsDeleted (bool)     │
└─────────────────────┘
```

**Quan hệ:**
- Order 1 ── * OrderItem
- Order 1 ── * OrderStatusHistory

---

### 3.2.2. Sequence Diagrams

#### Sequence Diagram: Tạo Đơn Hàng (Synchronous + Asynchronous)

```
Client          API Gateway      Order Service    User Service    Product Service    RabbitMQ      MongoDB
  │                  │                 │                │                │              │              │
  │  POST /api/      │                 │                │                │              │              │
  │  orders          │                 │                │                │              │              │
  ├─────────────────>│                 │                │                │              │              │
  │                  │  Route request  │                │                │              │              │
  │                  ├────────────────>│                │                │              │              │
  │                  │                 │                │                │              │              │
  │                  │                 │  Validate User │                │              │              │
  │                  │                 ├────────────────>│                │              │              │
  │                  │                 │                │  GET /users/{id}│              │              │
  │                  │                 │                │<────────────────┤              │              │
  │                  │                 │  User exists   │                │              │              │
  │                  │                 │<────────────────┤                │              │              │
  │                  │                 │                │                │              │              │
  │                  │                 │  Validate Products & Stock       │              │              │
  │                  │                 ├─────────────────────────────────>│              │              │
  │                  │                 │                │  GET /products/{id}           │              │
  │                  │                 │                │<────────────────────────────────┤              │
  │                  │                 │  Products valid │                │              │              │
  │                  │                 │<─────────────────────────────────┤              │              │
  │                  │                 │                │                │              │              │
  │                  │                 │  Create Order  │                │              │              │
  │                  │                 │  (Database Transaction)          │              │              │
  │                  │                 │  ┌──────────────────────────────┐│              │              │
  │                  │                 │  │ BEGIN TRANSACTION            ││              │              │
  │                  │                 │  │ INSERT INTO Orders           ││              │              │
  │                  │                 │  │ INSERT INTO OrderItems     ││              │              │
  │                  │                 │  │ COMMIT                        ││              │              │
  │                  │                 │  └──────────────────────────────┘│              │              │
  │                  │                 │                │                │              │              │
  │                  │                 │  Publish Event │                │              │              │
  │                  │                 ├─────────────────────────────────────────────────>│              │
  │                  │                 │                │                │  order.created│              │
  │                  │                 │                │                │              │              │
  │                  │                 │  Log to MongoDB│                │              │              │
  │                  │                 ├─────────────────────────────────────────────────────────────────>│
  │                  │                 │                │                │              │  Log event    │
  │                  │                 │                │                │              │              │
  │                  │                 │  Return Order  │                │              │              │
  │                  │                 │<──────────────────────────────┴──────────────┴──────────────┴─
  │                  │  Response      │                │                │              │              │
  │                  │<────────────────┤                │                │              │              │
  │  Response        │                │                │                │              │              │
  │<─────────────────┤                │                │                │              │              │
  │                  │                │                │                │              │              │
  │                  │                │                │                │              │              │
  │                  │                │                │                │              │              │
  │                  │                │                │                │  [Async]     │              │
  │                  │                │                │                │  Consumer    │              │
  │                  │                │                │                │  processes   │              │
  │                  │                │                │                │  event       │              │
  │                  │                │                │                │<─────────────┤              │
```

**Giải thích:**
1. Client gửi request tạo order qua API Gateway
2. Order Service validate User (HTTP call đồng bộ)
3. Order Service validate Products và Stock (HTTP call đồng bộ)
4. Order Service tạo Order trong database (transaction ACID)
5. Order Service publish event vào RabbitMQ (bất đồng bộ)
6. Order Service log vào MongoDB (bất đồng bộ)
7. Trả về response cho client ngay (không đợi async operations)
8. Consumer xử lý event sau (bất đồng bộ)

---

#### Sequence Diagram: Cập Nhật Trạng Thái Đơn Hàng

```
Client          API Gateway      Order Service    RabbitMQ      MongoDB
  │                  │                 │              │              │
  │  PUT /api/       │                 │              │              │
  │  orders/{id}/    │                 │              │              │
  │  status          │                 │              │              │
  ├─────────────────>│                 │              │              │
  │                  │  Route request  │              │              │
  │                  ├────────────────>│              │              │
  │                  │                 │              │              │
  │                  │                 │  Update Status│              │
  │                  │                 │  (Database)  │              │
  │                  │                 │  ┌──────────┐│              │
  │                  │                 │  │ UPDATE   ││              │
  │                  │                 │  │ Orders   ││              │
  │                  │                 │  │ INSERT   ││              │
  │                  │                 │  │ StatusHistory│          │
  │                  │                 │  │ COMMIT   ││              │
  │                  │                 │  └──────────┘│              │
  │                  │                 │              │              │
  │                  │                 │  Publish Event│              │
  │                  │                 ├──────────────>│              │
  │                  │                 │  order.status.│              │
  │                  │                 │  updated     │              │
  │                  │                 │              │              │
  │                  │                 │  Log to MongoDB│              │
  │                  │                 ├─────────────────────────────>│
  │                  │                 │              │              │
  │                  │                 │  Return Order│              │
  │                  │  Response      │<──────────────┴──────────────┴─
  │                  │<────────────────┤              │              │
  │  Response        │                │              │              │
  │<─────────────────┤                │              │              │
```

---

### 3.2.3. Class Diagrams

#### Class Diagram: User Service

```
┌─────────────────────────────────┐
│      BaseEntity (Abstract)      │
├─────────────────────────────────┤
│ +Id: int                        │
│ +CreatedAt: DateTime            │
│ +UpdatedAt: DateTime?           │
│ +IsDeleted: bool                │
└──────────────┬──────────────────┘
               │
               │ extends
               │
┌──────────────▼──────────────────┐
│           User                  │
├─────────────────────────────────┤
│ +Username: string               │
│ +Email: string                 │
│ +PasswordHash: string           │
│ +FirstName: string              │
│ +LastName: string              │
│ +PhoneNumber: string?           │
│ +Role: string                   │
│ +AvatarUrl: string?             │
│ +IsActive: bool                 │
│ +Addresses: List<UserAddress>    │
└──────────────┬──────────────────┘
               │
               │ 1
               │
               │ *
               │
┌──────────────▼──────────────────┐
│       UserAddress               │
├─────────────────────────────────┤
│ +UserId: int                    │
│ +FullName: string               │
│ +PhoneNumber: string            │
│ +Street: string                 │
│ +City: string                   │
│ +State: string                  │
│ +PostalCode: string             │
│ +Country: string                │
│ +IsDefault: bool                 │
└─────────────────────────────────┘

┌─────────────────────────────────┐
│      UserDbContext             │
├─────────────────────────────────┤
│ +Users: DbSet<User>            │
│ +UserAddresses: DbSet<UserAddress>│
└─────────────────────────────────┘

┌─────────────────────────────────┐
│      UserService                │
├─────────────────────────────────┤
│ -_context: UserDbContext        │
│ +GetAllAsync(): Task<List<User>>│
│ +GetByIdAsync(int): Task<User?> │
│ +CreateAsync(User): Task<User>  │
│ +UpdateAsync(int, User): Task   │
│ +DeleteAsync(int): Task         │
└─────────────────────────────────┘

┌─────────────────────────────────┐
│      UsersController           │
├─────────────────────────────────┤
│ -_userService: UserService     │
│ +GetAll(): Task<IActionResult>  │
│ +GetById(int): Task<IActionResult>│
│ +Create(CreateUserRequest): Task│
│ +Update(int, UpdateUserRequest):│
│ +Delete(int): Task              │
│ +GetAddresses(int): Task        │
│ +AddAddress(int, CreateUserAddressDto): Task│
│ +UpdateAddress(int, int, UpdateUserAddressDto): Task│
│ +DeleteAddress(int, int): Task  │
└─────────────────────────────────┘

┌─────────────────────────────────┐
│      AuthController              │
├─────────────────────────────────┤
│ -_userService: UserService      │
│ -_jwtService: IJwtService       │
│ +Login(LoginRequestDto): Task   │
│ +Register(CreateUserDto): Task  │
└─────────────────────────────────┘

┌─────────────────────────────────┐
│      JwtService                 │
├─────────────────────────────────┤
│ -_configuration: IConfiguration │
│ +GenerateToken(User): string    │
│ +GenerateRefreshToken(): string  │
└─────────────────────────────────┘
```

---

## 3.3. Thiết Kế API

### 3.3.1. Nguyên Tắc RESTful

Hệ thống tuân thủ các nguyên tắc REST:

1. **Resource-based URLs:** `/api/users`, `/api/products`, `/api/orders`
2. **HTTP Methods:**
   - GET: Lấy dữ liệu
   - POST: Tạo mới
   - PUT: Cập nhật toàn bộ
   - PATCH: Cập nhật một phần
   - DELETE: Xóa
3. **Status Codes:**
   - 200 OK: Thành công
   - 201 Created: Tạo mới thành công
   - 400 Bad Request: Request không hợp lệ
   - 404 Not Found: Không tìm thấy
   - 500 Internal Server Error: Lỗi server
4. **JSON Format:** Tất cả requests và responses đều dùng JSON

---

### 3.3.2. Chi Tiết API Endpoints

#### User Service APIs

#### Authentication APIs

**1. POST /api/auth/login**

- **Mô tả:** Đăng nhập và nhận JWT token
- **Request Body:**
  ```json
  {
    "username": "john_doe",
    "password": "password123"
  }
  ```
- **Response:**
  ```json
  {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
    "user": {
      "id": 1,
      "username": "john_doe",
      "email": "john@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "role": "Customer",
      "isActive": true
    },
    "expiresAt": "2024-01-01T11:00:00Z"
  }
  ```
- **Status Codes:** 200 OK, 401 Unauthorized, 400 Bad Request

---

**2. POST /api/auth/register**

- **Mô tả:** Đăng ký tài khoản mới và tự động đăng nhập
- **Request Body:**
  ```json
  {
    "username": "john_doe",
    "email": "john@example.com",
    "password": "password123",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "0123456789"
  }
  ```
- **Response:** LoginResponseDto (giống POST /api/auth/login)
- **Status Codes:** 201 Created, 400 Bad Request

---

#### User Management APIs

**3. GET /api/users**

- **Mô tả:** Lấy danh sách users
- **Query Parameters:**
  - `page` (int, optional): Số trang, mặc định = 1
  - `pageSize` (int, optional): Số items mỗi trang, mặc định = 10
  - `search` (string, optional): Tìm kiếm theo username hoặc email
- **Response:**
  ```json
  {
    "data": [
      {
        "id": 1,
        "username": "john_doe",
        "email": "john@example.com",
        "firstName": "John",
        "lastName": "Doe",
        "role": "Customer",
        "isActive": true
      }
    ],
    "totalCount": 100,
    "page": 1,
    "pageSize": 10
  }
  ```
- **Status Codes:** 200 OK

---

**4. GET /api/users/{id}**

- **Mô tả:** Lấy chi tiết user
- **Path Parameters:**
  - `id` (int): User ID
- **Response:**
  ```json
  {
    "id": 1,
    "username": "john_doe",
    "email": "john@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "0123456789",
    "role": "Customer",
    "isActive": true,
    "addresses": []
  }
  ```
- **Status Codes:** 200 OK, 404 Not Found

---

**5. POST /api/users**

- **Mô tả:** Tạo user mới
- **Request Body:**
  ```json
  {
    "username": "john_doe",
    "email": "john@example.com",
    "password": "password123",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "0123456789"
  }
  ```
- **Response:** User object (giống GET /api/users/{id})
- **Status Codes:** 201 Created, 400 Bad Request

---

**6. PUT /api/users/{id}**

- **Mô tả:** Cập nhật user
- **Path Parameters:**
  - `id` (int): User ID
- **Request Body:**
  ```json
  {
    "firstName": "John Updated",
    "lastName": "Doe Updated",
    "phoneNumber": "0987654321"
  }
  ```
- **Response:** User object đã update
- **Status Codes:** 200 OK, 404 Not Found, 400 Bad Request

---

**7. DELETE /api/users/{id}**

---

#### User Address APIs

**8. GET /api/users/{userId}/addresses**

- **Mô tả:** Lấy danh sách địa chỉ của user
- **Path Parameters:**
  - `userId` (int): User ID
- **Response:** List<UserAddressDto>
- **Status Codes:** 200 OK

---

**9. POST /api/users/{userId}/addresses**

- **Mô tả:** Thêm địa chỉ mới cho user
- **Path Parameters:**
  - `userId` (int): User ID
- **Request Body:**
  ```json
  {
    "fullName": "John Doe",
    "phoneNumber": "0123456789",
    "street": "123 Main St",
    "city": "Ho Chi Minh",
    "state": "Ho Chi Minh",
    "postalCode": "70000",
    "country": "Vietnam",
    "isDefault": true
  }
  ```
- **Response:** UserAddressDto
- **Status Codes:** 201 Created, 400 Bad Request

---

**10. PUT /api/users/{userId}/addresses/{addressId}**

- **Mô tả:** Cập nhật địa chỉ
- **Path Parameters:**
  - `userId` (int): User ID
  - `addressId` (int): Address ID
- **Request Body:** UpdateUserAddressDto
- **Response:** UserAddressDto
- **Status Codes:** 200 OK, 404 Not Found

---

**11. DELETE /api/users/{userId}/addresses/{addressId}**

- **Mô tả:** Xóa địa chỉ
- **Path Parameters:**
  - `userId` (int): User ID
  - `addressId` (int): Address ID
- **Response:** Không có body
- **Status Codes:** 200 OK, 404 Not Found

---

- **Mô tả:** Xóa user (soft delete)
- **Path Parameters:**
  - `id` (int): User ID
- **Response:** Không có body
- **Status Codes:** 200 OK, 404 Not Found

---

#### Product Service APIs

**1. GET /api/products**

- **Mô tả:** Lấy danh sách products
- **Query Parameters:**
  - `page` (int, optional): Mặc định = 1
  - `pageSize` (int, optional): Mặc định = 10
  - `category` (string, optional): Lọc theo category
  - `search` (string, optional): Tìm kiếm theo tên
- **Response:**
  ```json
  {
    "data": [
      {
        "id": 1,
        "name": "Laptop",
        "description": "High performance laptop",
        "price": 15000000,
        "stock": 10,
        "category": "Electronics",
        "isAvailable": true
      }
    ],
    "totalCount": 50,
    "page": 1,
    "pageSize": 10
  }
  ```
- **Status Codes:** 200 OK

---

**2. GET /api/products/{id}**

- **Mô tả:** Lấy chi tiết product
- **Path Parameters:**
  - `id` (int): Product ID
- **Response:** Product object đầy đủ (bao gồm reviews, tags)
- **Status Codes:** 200 OK, 404 Not Found

---

**3. GET /api/products/category/{category}**

- **Mô tả:** Lọc products theo category
- **Path Parameters:**
  - `category` (string): Category name
- **Response:** List<Product>
- **Status Codes:** 200 OK

---

**4. POST /api/products**

- **Mô tả:** Tạo product mới
- **Request Body:**
  ```json
  {
    "name": "Laptop",
    "description": "High performance laptop",
    "price": 15000000,
    "stock": 10,
    "category": "Electronics",
    "imageUrl": "https://example.com/image.jpg"
  }
  ```
- **Response:** Product object
- **Status Codes:** 201 Created, 400 Bad Request

---

**5. PUT /api/products/{id}**

- **Mô tả:** Cập nhật product
- **Path Parameters:**
  - `id` (int): Product ID
- **Request Body:** Product object (các fields cần update)
- **Response:** Product object đã update
- **Status Codes:** 200 OK, 404 Not Found, 400 Bad Request

---

**6. PATCH /api/products/{id}/stock**

- **Mô tả:** Cập nhật stock
- **Path Parameters:**
  - `id` (int): Product ID
- **Request Body:**
  ```json
  {
    "quantity": -2
  }
  ```
- **Response:** Product object với stock mới
- **Status Codes:** 200 OK, 404 Not Found, 400 Bad Request (nếu stock < 0)

---

**7. DELETE /api/products/{id}**

- **Mô tả:** Xóa product (soft delete)
- **Path Parameters:**
  - `id` (int): Product ID
- **Response:** Không có body
- **Status Codes:** 200 OK, 404 Not Found

---

#### Order Service APIs

**1. GET /api/orders**

- **Mô tả:** Lấy danh sách orders
- **Query Parameters:**
  - `page` (int, optional): Mặc định = 1
  - `pageSize` (int, optional): Mặc định = 10
  - `status` (string, optional): Lọc theo status
- **Response:** List<Order> với pagination
- **Status Codes:** 200 OK

---

**2. GET /api/orders/{id}**

- **Mô tả:** Lấy chi tiết order
- **Path Parameters:**
  - `id` (int): Order ID
- **Response:** Order object đầy đủ (bao gồm OrderItems, StatusHistory)
- **Status Codes:** 200 OK, 404 Not Found

---

**3. GET /api/orders/user/{userId}**

- **Mô tả:** Lấy orders của user
- **Path Parameters:**
  - `userId` (int): User ID
- **Response:** List<Order>
- **Status Codes:** 200 OK, 404 Not Found (nếu user không tồn tại)

---

**4. POST /api/orders**

- **Mô tả:** Tạo order mới
- **Request Body:**
  ```json
  {
    "userId": 1,
    "shippingAddress": "123 Main St, City, State, 12345",
    "orderItems": [
      {
        "productId": 1,
        "quantity": 2
      }
    ],
    "paymentMethod": "CreditCard"
  }
  ```
- **Response:** Order object
- **Status Codes:** 201 Created, 400 Bad Request
- **Business Logic:**
  - Validate userId tồn tại (gọi User Service)
  - Validate productId và stock đủ (gọi Product Service)
  - Tính TotalAmount
  - Tạo Order và OrderItems trong transaction
  - Publish event `order.created` vào RabbitMQ
  - Log vào MongoDB

---

**5. PUT /api/orders/{id}/status**

- **Mô tả:** Cập nhật status order
- **Path Parameters:**
  - `id` (int): Order ID
- **Request Body:**
  ```json
  {
    "status": "Shipped",
    "notes": "Order shipped via DHL"
  }
  ```
- **Response:** Order object đã update
- **Status Codes:** 200 OK, 404 Not Found, 400 Bad Request
- **Business Logic:**
  - Validate status hợp lệ
  - Update Order.Status
  - Insert vào OrderStatusHistory
  - Publish event `order.status.updated` vào RabbitMQ
  - Log vào MongoDB

---

**6. DELETE /api/orders/{id}**

- **Mô tả:** Xóa order (soft delete)
- **Path Parameters:**
  - `id` (int): Order ID
- **Response:** Không có body
- **Status Codes:** 200 OK, 404 Not Found

---

## 3.4. Thiết Kế Message Queue

### 3.4.1. Cấu Hình Queue

**RabbitMQ Server:** External server (47.130.33.106:5672)

**Queues:**

| Queue Name | Mục Đích | Publisher | Consumer |
|------------|----------|-----------|----------|
| `order.created` | Event khi tạo order mới | Order Service | (Có thể mở rộng: Email Service, Inventory Service) |
| `order.status.updated` | Event khi cập nhật status | Order Service | (Có thể mở rộng: Notification Service) |

**Queue Properties:**
- **Durable:** true (queue tồn tại khi RabbitMQ restart)
- **Auto-delete:** false
- **Exclusive:** false

---

### 3.4.2. Message Flow

#### Flow: Tạo Order và Publish Event

```
Order Service                    RabbitMQ                    Consumer (Future)
     │                              │                              │
     │  Create Order                │                              │
     │  (Database)                  │                              │
     │                              │                              │
     │  Publish Event               │                              │
     │  order.created               │                              │
     ├─────────────────────────────>│                              │
     │  {                           │                              │
     │    "eventType": "order.created",│                          │
     │    "orderId": 123,           │                              │
     │    "userId": 1,              │                              │
     │    "totalAmount": 30000000,  │                              │
     │    "timestamp": "2024-01-01T10:00:00Z"│                    │
     │  }                           │                              │
     │                              │  Store in queue              │
     │                              │                              │
     │  Return Response             │                              │
     │  (to client)                 │                              │
     │                              │                              │
     │                              │  [Async]                     │
     │                              │  Consumer processes          │
     │                              ├─────────────────────────────>│
     │                              │  Send email, update inventory│
     │                              │                              │
```

---

### 3.4.3. Cơ Chế Xử Lý Lỗi

**1. Retry Mechanism:**

- Nếu publish message thất bại, retry tối đa 3 lần
- Exponential backoff: 1s, 2s, 4s
- Nếu vẫn thất bại, log lỗi và tiếp tục (không block main flow)

**2. Message Acknowledgment:**

- Consumer phải acknowledge sau khi xử lý xong
- Nếu consumer crash, message sẽ được redeliver
- Nếu xử lý lỗi, có thể reject và requeue

**3. Dead Letter Queue (Có thể mở rộng):**

- Messages không thể xử lý sau nhiều lần retry sẽ được chuyển vào DLQ
- Admin có thể xem và xử lý thủ công

---

## 3.5. Thiết Kế Bảo Mật & Deployment

### 3.5.1. Cơ Chế Xác Thực

**Hiện tại:**
- Password hashing bằng BCrypt (cost factor = 12)
- Không lưu plain password

**Đã triển khai:**
- ✅ JWT (JSON Web Token) cho authentication
- ✅ Refresh tokens (GUID-based)
- ✅ JWT token validation trong User Service
- ✅ Frontend HTTP Interceptor tự động thêm token
- ✅ Angular Guards để bảo vệ routes

**Có thể mở rộng (Future):**
- Role-based authorization (RBAC) - đã có Role trong token, cần implement authorization policies
- Token refresh endpoint
- OAuth 2.0 / OpenID Connect

**Flow xác thực (Đã triển khai):**

```
Client                    API Gateway              User Service
  │                            │                         │
  │  POST /api/auth/login      │                         │
  │  {username, password}      │                         │
  ├───────────────────────────>│                         │
  │                            │  POST /api/auth/login   │
  │                            ├────────────────────────>│
  │                            │                         │  Validate credentials
  │                            │                         │  (BCrypt compare)
  │                            │                         │  Generate JWT Token
  │                            │                         │  Generate Refresh Token
  │                            │  LoginResponseDto        │
  │                            │  {Token, RefreshToken,  │
  │                            │   User, ExpiresAt}       │
  │                            │<────────────────────────┤
  │  LoginResponseDto          │                         │
  │<───────────────────────────┤                         │
  │                            │                         │
  │  [Store token in localStorage]                        │
  │                            │                         │
  │  GET /api/orders           │                         │
  │  Header: Authorization:    │                         │
  │  Bearer <token>             │                         │
  ├───────────────────────────>│                         │
  │                            │  [HTTP Interceptor      │
  │                            │   auto-adds token]      │
  │                            │  Forward request       │
  │                            │  with token            │
  │                            ├────────────────────────>│
  │                            │                         │  Validate JWT
  │                            │                         │  Extract UserId, Role
  │                            │                         │  Process request
  │                            │  Response              │
  │                            │<────────────────────────┤
  │  Response                  │                         │
  │<───────────────────────────┤                         │
```

---

### 3.5.2. Phân Quyền

**Roles:**
- **Customer:** Xem và quản lý orders của chính mình
- **Admin:** Full access, quản lý users, products, orders
- **Manager:** Quản lý products và orders (có thể mở rộng)

**Authorization Rules (Hiện tại):**
- ✅ JWT token được validate trong User Service
- ✅ Token chứa Role claim (Customer, Admin)
- ✅ Frontend sử dụng Angular Guards để bảo vệ routes
- ⏳ Backend authorization policies (có thể mở rộng)

**Authorization Rules (Có thể mở rộng):**
- Customer chỉ có thể xem/sửa thông tin của chính mình
- Admin có thể xem/sửa/xóa tất cả
- Một số endpoints chỉ dành cho Admin (ví dụ: DELETE /api/users/{id})

---

### 3.5.3. Bảo Vệ Dữ Liệu

1. **Password Hashing:**
   - BCrypt với cost factor = 12
   - Salt tự động

2. **Input Validation:**
   - Validate tất cả inputs từ client
   - Sanitize để tránh SQL injection, XSS

3. **CORS:**
   - Chỉ cho phép requests từ frontend domain
   - Cấu hình trong API Gateway

4. **HTTPS (Production):**
   - Sử dụng SSL/TLS certificates
   - Let's Encrypt cho production

---

### 3.5.4. Cách Đóng Gói và Triển Khai

#### Docker

**Dockerfile Structure (Multi-stage build):**

```dockerfile
# Stage 1: Base (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Microservice.Services.UserService/...", "Microservice.Services.UserService/"]
COPY ["Microservice.Common/...", "Microservice.Common/"]
RUN dotnet restore "Microservice.Services.UserService/..."
COPY . .
RUN dotnet build "Microservice.Services.UserService/..." -c Release

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish "Microservice.Services.UserService/..." -c Release -o /app/publish

# Stage 4: Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microservice.Services.UserService.dll"]
```

**Lợi ích:**
- Image size nhỏ (chỉ chứa runtime, không có SDK)
- Build nhanh với layer caching
- Security: không expose source code

---

#### Docker Compose

**File: `docker-compose.yml`**

```yaml
version: '3.8'

services:
  user-service:
    build:
      context: .
      dockerfile: Microservice.Services.UserService/Dockerfile
    container_name: microservice-user-service
    ports:
      - "5001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__PostgreSQL=...
    networks:
      - microservice-network

  # ... các services khác

networks:
  microservice-network:
    driver: bridge
```

**Deploy:**

```bash
# Build và chạy
docker-compose up -d --build

# Xem logs
docker-compose logs -f

# Dừng
docker-compose down
```

---

#### Deployment Process

**1. Development:**
- Chạy local với `dotnet run`
- Hot reload khi code thay đổi

**2. Build:**
- Build Docker images: `docker-compose build`
- Test images locally

**3. Production:**
- Push images lên Docker Registry (nếu có)
- Deploy lên server với `docker-compose up -d`
- Setup Nginx reverse proxy
- Setup SSL certificates
- Monitor với health checks

---

## 📝 Tóm Tắt

Báo cáo này đã trình bày đầy đủ 3 phần:

1. **PHẦN I:** Phân tích yêu cầu, bài toán, chức năng, và yêu cầu phi chức năng
2. **PHẦN II:** Mô tả chi tiết các services, bảng tổng hợp, technology stack
3. **PHẦN III:** Thiết kế phần mềm với diagrams, API design, message queue, bảo mật, và deployment

Tài liệu này có thể được sử dụng làm template cho báo cáo dự án, giúp trình bày một cách chuyên nghiệp và đầy đủ về hệ thống Microservice E-Commerce Backend.

---

---

## 3.6. Frontend Authentication

### 3.6.1. Angular Authentication Service

**File:** `Frontend/src/app/services/auth.service.ts`

**Chức năng:**
- Login, Register, Logout
- Lưu JWT token và user info vào localStorage
- Kiểm tra token expiry
- Observable để theo dõi user hiện tại
- Auto-logout khi token hết hạn

### 3.6.2. HTTP Interceptor

**File:** `Frontend/src/app/interceptors/auth.interceptor.ts`

**Chức năng:**
- Tự động thêm JWT token vào HTTP headers
- Format: `Authorization: Bearer <token>`
- Áp dụng cho tất cả HTTP requests

### 3.6.3. Route Guards

**File:** `Frontend/src/app/guards/auth.guard.ts`

**Chức năng:**
- Bảo vệ routes cần authentication
- Redirect đến `/login` nếu chưa đăng nhập
- Áp dụng cho: `/users`, `/orders`

### 3.6.4. Login/Register Components

**Files:**
- `Frontend/src/app/components/auth/login.component.ts`
- `Frontend/src/app/components/auth/register.component.ts`

**Chức năng:**
- Reactive forms với validation
- Hiển thị/ẩn password
- Error handling và thông báo
- Tự động redirect sau khi đăng nhập/đăng ký thành công

---

## 📝 Tóm Tắt

Báo cáo này đã trình bày đầy đủ 3 phần:

1. **PHẦN I:** Phân tích yêu cầu, bài toán, chức năng (bao gồm authentication), và yêu cầu phi chức năng
2. **PHẦN II:** Mô tả chi tiết các services (bao gồm Auth endpoints), bảng tổng hợp, technology stack (bao gồm JWT)
3. **PHẦN III:** Thiết kế phần mềm với diagrams, API design (bao gồm Auth APIs), message queue, bảo mật (JWT đã triển khai), deployment, và Frontend authentication

**Các tính năng đã triển khai:**
- ✅ JWT Authentication (Backend + Frontend)
- ✅ User Login/Register với JWT token
- ✅ HTTP Interceptor tự động thêm token
- ✅ Route Guards bảo vệ routes
- ✅ User Addresses management
- ✅ Database schema tự động migration
- ✅ Frontend authentication UI

Tài liệu này có thể được sử dụng làm template cho báo cáo dự án, giúp trình bày một cách chuyên nghiệp và đầy đủ về hệ thống Microservice E-Commerce Backend.

---

**Lưu ý:** Khi viết báo cáo thực tế, cần:
- Thêm các diagrams thực tế (vẽ bằng công cụ như Draw.io, Lucidchart)
- Thêm screenshots của Swagger UI, MongoDB logs, RabbitMQ management
- Thêm screenshots của Frontend login/register pages
- Thêm kết quả test performance
- Thêm phần đánh giá, kết luận, và hướng phát triển

