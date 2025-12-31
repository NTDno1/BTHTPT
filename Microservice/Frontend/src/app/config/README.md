# Cấu Hình Môi Trường

## File `environment.ts`

File này tập trung tất cả các URL và cấu hình của ứng dụng. Khi build cho production, chỉ cần thay đổi các giá trị trong file này.

## Cách Sử Dụng

### 1. Development (Local)
File hiện tại đã được cấu hình cho môi trường local:
```typescript
apiGatewayUrl: 'http://localhost:5010'
userServiceUrl1: 'http://localhost:5001'
// ...
```

### 2. Production
Khi build cho production, chỉ cần thay đổi các URL:

```typescript
export const environment = {
  apiGatewayUrl: 'http://103.82.26.211:5010',
  apiGatewayApiUrl: 'http://103.82.26.211:5010/api',
  
  userServiceUrl1: 'http://103.82.26.211:5001',
  userServiceUrl2: 'http://103.82.26.211:5004',
  userServiceApiUrl: 'http://103.82.26.211:5001/api',
  
  // ... các URL khác
};
```

### 3. Build
Sau khi thay đổi, chạy build như bình thường:
```bash
npm run build
```

## Các File Đã Sử Dụng Environment

- `services/api.service.ts` - Sử dụng `apiGatewayApiUrl`
- `services/auth.service.ts` - Sử dụng `userServiceApiUrl`
- `components/home/home.component.ts` - Hiển thị tất cả các URL

## Lưu ý

- Tất cả các URL đã được tập trung vào file này
- Không cần tìm kiếm và thay thế trong nhiều file
- Chỉ cần sửa một nơi khi chuyển môi trường

