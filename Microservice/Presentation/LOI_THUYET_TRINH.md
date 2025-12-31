# 🎤 Lời Thuyết Trình Chi Tiết - Dự Án Microservice

## 📊 SLIDE 1: Giới Thiệu Dự Án (30 giây)

**Lời nói:**

> "Xin chào thầy và các bạn. Hôm nay tôi sẽ trình bày về dự án E-Commerce Backend được xây dựng theo kiến trúc Microservice.
> 
> Dự án này được phát triển nhằm giải quyết các vấn đề của kiến trúc monolithic truyền thống, như khó scale, khó bảo trì, và rủi ro khi một phần lỗi ảnh hưởng toàn hệ thống.
> 
> Chúng em sử dụng .NET 8.0 cho backend, Angular cho frontend, và các công nghệ hiện đại như PostgreSQL, MongoDB, và RabbitMQ."

---

## 🏗️ SLIDE 2: Kiến Trúc Tổng Quan (1 phút)

**Lời nói:**

> "Đây là kiến trúc tổng thể của hệ thống. 
> 
> Ở tầng trên cùng, chúng ta có Frontend Angular chạy trên port 4200, đây là giao diện người dùng.
> 
> Frontend gửi tất cả requests đến API Gateway RabbitMQ trên port 5010. API Gateway này sử dụng RabbitMQ message queue để điều hướng requests đến các microservices tương ứng.
> 
> Chúng ta có 3 microservices chính:
> - User Service trên port 5001, quản lý người dùng
> - Product Service trên port 5002, quản lý sản phẩm
> - Order Service trên port 5003, quản lý đơn hàng
> 
> Mỗi service có RabbitMQConsumerService để nhận requests từ RabbitMQ queue và xử lý.
> 
> Mỗi service có database PostgreSQL riêng, đảm bảo tính độc lập.
> 
> Ngoài ra, chúng ta còn có MongoDB để lưu logs và events, và RabbitMQ vừa là message queue cho API Gateway, vừa là event bus cho các services."

**Hành động:** Chỉ vào sơ đồ và giải thích từng thành phần

---

## 👥 SLIDE 3: User Service (30 giây)

**Lời nói:**

> "User Service là microservice đầu tiên, chịu trách nhiệm quản lý người dùng.
> 
> Service này cung cấp các chức năng cơ bản như đăng ký tài khoản, xem danh sách users, xem chi tiết, cập nhật thông tin, và xóa user.
> 
> Tất cả dữ liệu được lưu trong database PostgreSQL riêng tên là userservice_db.
> 
> Các hoạt động của service được log vào MongoDB để theo dõi và phân tích."

**Hành động:** Có thể mở Swagger UI để show endpoints

---

## 📦 SLIDE 4: Product Service (30 giây)

**Lời nói:**

> "Product Service quản lý toàn bộ thông tin về sản phẩm.
> 
> Service này cho phép thêm, sửa, xóa sản phẩm, tìm kiếm theo category, và quan trọng nhất là quản lý tồn kho.
> 
> Database riêng là productservice_db, và tương tự như User Service, logs được lưu vào MongoDB."

**Hành động:** Show danh sách sản phẩm trên Frontend

---

## 🛒 SLIDE 5: Order Service (1 phút)

**Lời nói:**

> "Order Service là service phức tạp nhất, quản lý đơn hàng.
> 
> Khi tạo đơn hàng, service này không chỉ lưu thông tin đơn hàng vào database riêng, mà còn phải tích hợp với Product Service để:
> - Kiểm tra tồn kho có đủ không
> - Lấy giá sản phẩm
> - Tự động trừ tồn kho sau khi tạo đơn thành công
> 
> Đây là ví dụ điển hình về giao tiếp giữa các microservices.
> 
> Ngoài ra, Order Service còn publish events lên RabbitMQ như 'order.created' và 'order.status.updated', cho phép các service khác subscribe và xử lý."

**Hành động:** Giải thích luồng tạo đơn hàng

---

## 🚪 SLIDE 6: API Gateway RabbitMQ (1 phút)

**Lời nói:**

> "API Gateway RabbitMQ đóng vai trò rất quan trọng trong kiến trúc microservice của chúng em.
> 
> Đây là single entry point cho tất cả requests từ frontend, chạy trên port 5010.
> 
> Khi frontend gửi request, API Gateway không gọi trực tiếp đến service, mà đưa request vào RabbitMQ queue tương ứng. Ví dụ, request đến User Service sẽ được đưa vào queue 'api.user.request'.
> 
> Mỗi microservice có RabbitMQConsumerService riêng, lắng nghe queue của mình. Khi có message, consumer nhận và xử lý, sau đó gửi response lại qua queue 'api.gateway.response'.
> 
> Cách này bất đồng bộ, cho phép load balancing tự nhiên - nếu có nhiều instances của một service, RabbitMQ sẽ phân phối messages đều cho các instances.
> 
> Đây là điểm khác biệt so với API Gateway truyền thống sử dụng HTTP trực tiếp."

**Hành động:** Giải thích luồng request qua RabbitMQ

---

## ⚖️ SLIDE 7: Load Balancing & Scaling (1 phút)

**Lời nói:**

> "Một điểm mạnh của kiến trúc này là khả năng load balancing và scaling tự động.
> 
> Khi chúng ta chạy nhiều containers của cùng một service, ví dụ như user-service và user-service-v2, cả hai đều lắng nghe cùng một RabbitMQ queue là 'api.user.request'.
> 
> Khi có requests đến, RabbitMQ tự động phân phối messages đều cho các consumer instances theo cơ chế round-robin. Request đầu tiên đến user-service, request thứ hai đến user-service-v2, và cứ thế luân phiên.
> 
> Điều này có nghĩa là chúng ta có thể tăng tải xử lý bằng cách đơn giản là chạy thêm containers, mà không cần thay đổi code hay cấu hình gì thêm.
> 
> Nếu một instance bị lỗi, RabbitMQ tự động chuyển messages sang instance còn lại, đảm bảo tính fault tolerance.
> 
> Đây là scaling horizontal - thêm instances thay vì tăng tài nguyên của một instance duy nhất."

**Hành động:** Show Docker containers đang chạy, giải thích cách RabbitMQ phân phối

---

## 🛠️ SLIDE 8: Công Nghệ & Công Cụ (0.5 phút)

**Lời nói:**

> "Về công nghệ, chúng em sử dụng:
> 
> Backend được xây dựng bằng .NET 8.0, framework mới nhất của Microsoft, với Entity Framework Core để làm việc với database.
> 
> Database chính là PostgreSQL, mỗi service có database riêng để đảm bảo tính độc lập.
> 
> MongoDB được dùng để lưu logs và events, phù hợp với dữ liệu không có cấu trúc cố định.
> 
> RabbitMQ đóng vai trò kép: vừa là message broker cho API Gateway, vừa là event bus cho giao tiếp giữa các services.
> 
> Frontend sử dụng Angular 17 với Angular Material để có giao diện đẹp và hiện đại.
> 
> Tất cả được containerize bằng Docker để dễ deploy và scale."

**Hành động:** Liệt kê các công nghệ

---

## 🎬 SLIDE 9: Demo - Tạo User & Product (1.5 phút)

**Lời nói:**

> "Bây giờ tôi sẽ demo các chức năng cơ bản.
> 
> Đầu tiên là tạo user mới. Tôi sẽ mở Frontend, vào tab Users, và tạo một user mới với đầy đủ thông tin.
> 
> [Thực hiện tạo user]
> 
> Như các bạn thấy, user đã được tạo thành công. Bây giờ tôi sẽ tạo một sản phẩm mới.
> 
> [Thực hiện tạo product]
> 
> Sản phẩm đã được thêm vào danh sách. Điểm quan trọng ở đây là User Service và Product Service hoạt động hoàn toàn độc lập, mỗi service có database riêng, không ảnh hưởng lẫn nhau."

**Hành động:** Live demo trên Frontend

---

## 🛒 SLIDE 10: Demo - Tạo Đơn Hàng (1.5 phút)

**Lời nói:**

> "Bây giờ là phần quan trọng nhất - tạo đơn hàng.
> 
> Khi tôi tạo đơn hàng, hệ thống sẽ:
> 1. Gọi Product Service để lấy thông tin sản phẩm và kiểm tra tồn kho
> 2. Tính toán tổng tiền dựa trên giá và số lượng
> 3. Tạo đơn hàng trong Order Service
> 4. Tự động trừ tồn kho trong Product Service
> 5. Publish event 'order.created' lên RabbitMQ
> 
> [Thực hiện tạo order]
> 
> Như các bạn thấy, đơn hàng đã được tạo thành công. Tôi sẽ mở RabbitMQ Management UI để show event đã được publish.
> 
> [Show RabbitMQ queue với message]
> 
> Đây là ví dụ điển hình về giao tiếp giữa các microservices và sử dụng message queue."

**Hành động:** Live demo tạo order và show RabbitMQ

---

## 🔄 SLIDE 11: Giao Tiếp Giữa Services (1 phút)

**Lời nói:**

> "Có 2 cách giao tiếp chính trong hệ thống:
> 
> Cách thứ nhất là giữa các microservices với nhau, sử dụng Synchronous HTTP/REST. Như trong demo vừa rồi, Order Service gọi trực tiếp Product Service qua HTTP để lấy thông tin sản phẩm và cập nhật stock. Cách này đơn giản, nhưng tạo dependency trực tiếp.
> 
> Cách thứ hai là qua API Gateway RabbitMQ, sử dụng Asynchronous message queue. Tất cả requests từ frontend đều qua RabbitMQ queue. Mỗi service có RabbitMQConsumerService để nhận và xử lý messages. Cách này cho phép load balancing tự nhiên và xử lý bất đồng bộ.
> 
> Ngoài ra, Order Service còn publish events như 'order.created' lên RabbitMQ, các service khác có thể subscribe và xử lý. Đây là event-driven architecture."

**Hành động:** Giải thích sơ đồ

---

## ✅ SLIDE 12: Ưu Điểm & Thách Thức (1 phút)

**Lời nói:**

> "Kiến trúc Microservice có nhiều ưu điểm:
> 
> Mỗi service có thể scale độc lập. Ví dụ, nếu Order Service bị quá tải, chúng ta chỉ cần scale Order Service, không cần scale toàn bộ hệ thống.
> 
> Dễ bảo trì và phát triển vì mỗi service nhỏ, dễ hiểu.
> 
> Tách biệt database tránh conflict và cho phép chọn công nghệ phù hợp cho từng service.
> 
> Fault isolation - một service lỗi không ảnh hưởng toàn hệ thống.
> 
> Tuy nhiên, cũng có thách thức:
> 
> Phức tạp hơn monolithic, cần quản lý nhiều services, distributed transactions khó xử lý, và cần infrastructure như RabbitMQ, MongoDB."

**Hành động:** So sánh với monolithic

---

## 🚀 SLIDE 13: Kết Luận & Hướng Phát Triển (30 giây)

**Lời nói:**

> "Tóm lại, chúng em đã xây dựng thành công hệ thống E-Commerce theo kiến trúc Microservice với 3 microservices chính và API Gateway RabbitMQ, tích hợp RabbitMQ cho async communication, và Frontend Angular.
> 
> Hướng phát triển tiếp theo bao gồm thêm Authentication & Authorization, Service Discovery, Monitoring & Logging, và deploy lên cloud.
> 
> Cảm ơn thầy và các bạn đã lắng nghe. Em xin mời các câu hỏi."

**Hành động:** Kết thúc và mời Q&A

---

## 💡 Tips Khi Thuyết Trình

1. **Giọng nói:** Rõ ràng, tự tin, không quá nhanh
2. **Body language:** Chỉ vào slide, giao tiếp bằng mắt với khán giả
3. **Timing:** Chú ý thời gian, đừng quá 10 phút
4. **Demo:** Chuẩn bị sẵn, test trước, có backup plan nếu lỗi
5. **Q&A:** Chuẩn bị trước các câu hỏi có thể được hỏi:
   - Tại sao chọn Microservice thay vì Monolithic?
   - Làm thế nào xử lý distributed transactions?
   - Cách scale services như thế nào?
   - So sánh API Gateway Ocelot và RabbitMQ?

