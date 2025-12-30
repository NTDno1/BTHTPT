# Frontend - Microservice E-Commerce Demo

Frontend Angular để demo hệ thống Microservice.

## 🚀 Cài Đặt và Chạy

### Yêu Cầu:
- Node.js 18+ 
- npm hoặc yarn

### Cài Đặt:

```bash
cd Frontend
npm install
```

### Chạy Development Server:

```bash
npm start
# hoặc
ng serve
```

Frontend sẽ chạy tại: http://localhost:4200

### Build Production:

```bash
npm run build
```

## 📁 Cấu Trúc

```
src/
├── app/
│   ├── components/
│   │   ├── home/          # Trang chủ
│   │   ├── users/         # Quản lý users
│   │   ├── products/      # Quản lý products
│   │   └── orders/        # Quản lý orders
│   ├── services/
│   │   └── api.service.ts # Service gọi API
│   ├── app.component.ts   # Component chính
│   └── app.routes.ts      # Routing
├── index.html
├── main.ts
└── styles.scss
```

## 🔧 Cấu Hình API

API base URL được cấu hình trong `src/app/services/api.service.ts`:

```typescript
const API_BASE_URL = 'http://localhost:5000/api';
```

Nếu API Gateway chạy ở port khác, cập nhật giá trị này.

## 📱 Tính Năng

- ✅ Xem danh sách Users
- ✅ Xem danh sách Products
- ✅ Lọc Products theo category
- ✅ Xem danh sách Orders
- ✅ Cập nhật trạng thái Order
- ✅ Xóa Users và Orders
- ✅ UI với Angular Material

## 🎨 UI Components

Sử dụng Angular Material:
- MatToolbar - Header
- MatSidenav - Sidebar navigation
- MatTable - Tables
- MatCard - Cards
- MatButton, MatIcon - Buttons và Icons
- MatSnackBar - Notifications

