import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

const API_BASE_URL = 'http://localhost:5010/api';

export interface UserAddress {
  id: number;
  fullName: string;
  phoneNumber: string;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  isDefault: boolean;
}

export interface User {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  isActive: boolean;
  role: string;
  avatarUrl?: string;
  addresses: UserAddress[];
  createdAt: string;
}

export interface CreateUser {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  role?: string;
}

export interface CreateUserAddress {
  fullName: string;
  phoneNumber: string;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  isDefault?: boolean;
}

export interface ProductReview {
  id: number;
  productId: number;
  userId: number;
  userName: string;
  rating: number;
  comment: string;
  isVerifiedPurchase: boolean;
  createdAt: string;
}

export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  discountPrice?: number;
  hasDiscount: boolean;
  discountStartDate?: string;
  discountEndDate?: string;
  stock: number;
  category: string;
  imageUrl?: string;
  isAvailable: boolean;
  tags: string[];
  averageRating: number;
  reviewCount: number;
  createdAt: string;
}

export interface CreateProduct {
  name: string;
  description: string;
  price: number;
  stock: number;
  category: string;
  imageUrl?: string;
  tags?: string[];
}

export interface ProductSearch {
  searchTerm?: string;
  category?: string;
  tags?: string[];
  minPrice?: number;
  maxPrice?: number;
  minRating?: number;
  inStock?: boolean;
  hasDiscount?: boolean;
  sortBy?: string;
  sortOrder?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateProductReview {
  userId: number;
  userName: string;
  rating: number;
  comment: string;
  isVerifiedPurchase?: boolean;
}

export interface OrderItem {
  id: number;
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  subTotal: number;
}

export interface OrderStatusHistory {
  id: number;
  orderId: number;
  status: string;
  notes?: string;
  changedBy?: string;
  createdAt: string;
}

export interface Order {
  id: number;
  userId: number;
  totalAmount: number;
  status: string;
  shippingAddress: string;
  orderItems: OrderItem[];
  paymentMethod?: string;
  paymentStatus?: string;
  paymentTransactionId?: string;
  paymentDate?: string;
  shippingCarrier?: string;
  trackingNumber?: string;
  shippedDate?: string;
  deliveredDate?: string;
  notes?: string;
  createdAt: string;
}

export interface CreateOrder {
  userId: number;
  shippingAddress: string;
  orderItems: { productId: number; quantity: number }[];
}

export interface PaymentInfo {
  paymentMethod: string;
  paymentStatus: string;
  paymentTransactionId?: string;
}

export interface ShippingInfo {
  shippingCarrier: string;
  trackingNumber: string;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  constructor(private http: HttpClient) {}

  // User APIs
  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${API_BASE_URL}/users`);
  }

  getUser(id: number): Observable<User> {
    return this.http.get<User>(`${API_BASE_URL}/users/${id}`);
  }

  createUser(user: CreateUser): Observable<User> {
    return this.http.post<User>(`${API_BASE_URL}/users`, user);
  }

  updateUser(id: number, user: Partial<User>): Observable<User> {
    return this.http.put<User>(`${API_BASE_URL}/users/${id}`, user);
  }

  deleteUser(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/users/${id}`);
  }

  // User Address APIs
  getUserAddresses(userId: number): Observable<UserAddress[]> {
    return this.http.get<UserAddress[]>(`${API_BASE_URL}/users/${userId}/addresses`);
  }

  addUserAddress(userId: number, address: CreateUserAddress): Observable<UserAddress> {
    return this.http.post<UserAddress>(`${API_BASE_URL}/users/${userId}/addresses`, address);
  }

  updateUserAddress(userId: number, addressId: number, address: Partial<CreateUserAddress>): Observable<UserAddress> {
    return this.http.put<UserAddress>(`${API_BASE_URL}/users/${userId}/addresses/${addressId}`, address);
  }

  deleteUserAddress(userId: number, addressId: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/users/${userId}/addresses/${addressId}`);
  }

  // Product APIs
  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(`${API_BASE_URL}/products`);
  }

  getProduct(id: number): Observable<Product> {
    return this.http.get<Product>(`${API_BASE_URL}/products/${id}`);
  }

  getProductsByCategory(category: string): Observable<Product[]> {
    return this.http.get<Product[]>(`${API_BASE_URL}/products/category/${category}`);
  }

  createProduct(product: CreateProduct): Observable<Product> {
    return this.http.post<Product>(`${API_BASE_URL}/products`, product);
  }

  updateProduct(id: number, product: Partial<Product>): Observable<Product> {
    return this.http.put<Product>(`${API_BASE_URL}/products/${id}`, product);
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/products/${id}`);
  }

  // Product Review APIs
  getProductReviews(productId: number): Observable<ProductReview[]> {
    return this.http.get<ProductReview[]>(`${API_BASE_URL}/products/${productId}/reviews`);
  }

  addProductReview(productId: number, review: CreateProductReview): Observable<ProductReview> {
    return this.http.post<ProductReview>(`${API_BASE_URL}/products/${productId}/reviews`, review);
  }

  updateProductReview(productId: number, reviewId: number, review: Partial<CreateProductReview>): Observable<ProductReview> {
    return this.http.put<ProductReview>(`${API_BASE_URL}/products/${productId}/reviews/${reviewId}`, review);
  }

  deleteProductReview(productId: number, reviewId: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/products/${productId}/reviews/${reviewId}`);
  }

  // Product Search
  searchProducts(search: ProductSearch): Observable<Product[]> {
    return this.http.post<Product[]>(`${API_BASE_URL}/products/search`, search);
  }

  // Product Discount
  setProductDiscount(productId: number, discount: { discountPrice: number; discountStartDate?: string; discountEndDate?: string }): Observable<Product> {
    return this.http.post<Product>(`${API_BASE_URL}/products/${productId}/discount`, discount);
  }

  // Product Tags
  addProductTags(productId: number, tags: string[]): Observable<Product> {
    return this.http.post<Product>(`${API_BASE_URL}/products/${productId}/tags`, { tags });
  }

  removeProductTag(productId: number, tagName: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/products/${productId}/tags/${encodeURIComponent(tagName)}`);
  }

  // Order APIs
  getOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(`${API_BASE_URL}/orders`);
  }

  getOrder(id: number): Observable<Order> {
    return this.http.get<Order>(`${API_BASE_URL}/orders/${id}`);
  }

  getOrdersByUser(userId: number): Observable<Order[]> {
    return this.http.get<Order[]>(`${API_BASE_URL}/orders/user/${userId}`);
  }

  createOrder(order: CreateOrder): Observable<Order> {
    return this.http.post<Order>(`${API_BASE_URL}/orders`, order);
  }

  updateOrderStatus(id: number, status: string): Observable<Order> {
    return this.http.put<Order>(`${API_BASE_URL}/orders/${id}/status`, { status });
  }

  deleteOrder(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/orders/${id}`);
  }

  // Order Status History
  getOrderStatusHistory(orderId: number): Observable<OrderStatusHistory[]> {
    return this.http.get<OrderStatusHistory[]>(`${API_BASE_URL}/orders/${orderId}/history`);
  }

  // Order Payment
  updateOrderPayment(orderId: number, payment: PaymentInfo): Observable<Order> {
    return this.http.put<Order>(`${API_BASE_URL}/orders/${orderId}/payment`, payment);
  }

  // Order Shipping
  updateOrderShipping(orderId: number, shipping: ShippingInfo): Observable<Order> {
    return this.http.put<Order>(`${API_BASE_URL}/orders/${orderId}/shipping`, shipping);
  }
}

