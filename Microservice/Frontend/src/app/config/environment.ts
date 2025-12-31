/**
 * Cấu hình môi trường - Tập trung tất cả các URL và cấu hình
 * 
 * Khi build cho production, chỉ cần thay đổi các giá trị trong file này
 */

export const environment = {
  // API Gateway URL (RabbitMQ Gateway)
  apiGatewayUrl: 'http://103.82.26.211:5010',
  apiGatewayApiUrl: 'http://103.82.26.211:5010/api',

  // User Service URLs (Load Balanced)
  userServiceUrl1: 'http://103.82.26.211:5001',
  userServiceUrl2: 'http://103.82.26.211:5004',
  userServiceApiUrl: 'http://103.82.26.211:5001/api', // Dùng cho Auth Service

  // Product Service URLs (Load Balanced)
  productServiceUrl1: 'http://103.82.26.211:5002',
  productServiceUrl2: 'http://103.82.26.211:5006',

  // Order Service URLs (Load Balanced)
  orderServiceUrl1: 'http://103.82.26.211:5003',
  orderServiceUrl2: 'http://103.82.26.211:5007',

  // Database Info (for display only)
  database: {
    userService: 'PostgreSQL (userservice_db)',
    productService: 'PostgreSQL (productservice_db)',
    orderService: 'PostgreSQL (orderservice_db)'
  },

  // Message Queue Info
  rabbitMQ: {
    host: '47.130.33.106',
    port: 5672
  }
};

