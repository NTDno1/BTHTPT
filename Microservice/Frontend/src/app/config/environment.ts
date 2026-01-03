/**
 * Cấu hình môi trường - Tập trung tất cả các URL và cấu hình
 * 
 * Khi build cho production, chỉ cần thay đổi các giá trị trong file này
 */

// Base URL - Chỉ cần thay đổi ở đây khi deploy sang server khác
// const BASE_URL = 'http://103.82.26.211';
const BASE_URL = 'http://localhost';

// Ports cho các services
const PORTS = {
  apiGateway: 5010,
  userService1: 5001,
  userService2: 5004,
  productService1: 5002,
  productService2: 5006,
  orderService1: 5003,
  orderService2: 5007
};

export const environment = {
  // API Gateway URL (RabbitMQ Gateway)
  apiGatewayUrl: `${BASE_URL}:${PORTS.apiGateway}`,
  apiGatewayApiUrl: `${BASE_URL}:${PORTS.apiGateway}/api`,

  // User Service URLs (Load Balanced)
  userServiceUrl1: `${BASE_URL}:${PORTS.userService1}`,
  userServiceUrl2: `${BASE_URL}:${PORTS.userService2}`,
  userServiceApiUrl: `${BASE_URL}:${PORTS.userService1}/api`, // Dùng cho Auth Service

  // Product Service URLs (Load Balanced)
  productServiceUrl1: `${BASE_URL}:${PORTS.productService1}`,
  productServiceUrl2: `${BASE_URL}:${PORTS.productService2}`,

  // Order Service URLs (Load Balanced)
  orderServiceUrl1: `${BASE_URL}:${PORTS.orderService1}`,
  orderServiceUrl2: `${BASE_URL}:${PORTS.orderService2}`,

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

