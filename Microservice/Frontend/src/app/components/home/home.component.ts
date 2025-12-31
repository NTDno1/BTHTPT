import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';
import { environment } from '../../config/environment';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule, RouterModule],
  template: `
    <div class="home-container">
      <h1>Welcome to Microservice E-Commerce Demo</h1>
      <p class="subtitle">Hệ thống minh họa kiến trúc Microservice với .NET 8.0</p>

      <div class="cards-grid">
        <mat-card class="feature-card">
          <mat-card-header>
            <mat-icon mat-card-avatar>people</mat-icon>
            <mat-card-title>User Service</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <p>Quản lý người dùng với CRUD operations đầy đủ</p>
            <ul>
              <li>Đăng ký tài khoản</li>
              <li>Xem danh sách users</li>
              <li>Cập nhật thông tin</li>
            </ul>
          </mat-card-content>
          <mat-card-actions>
            <button mat-raised-button color="primary" routerLink="/users">
              <mat-icon>arrow_forward</mat-icon>
              Quản lý Users
            </button>
          </mat-card-actions>
        </mat-card>

        <mat-card class="feature-card">
          <mat-card-header>
            <mat-icon mat-card-avatar>inventory</mat-icon>
            <mat-card-title>Product Service</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <p>Quản lý sản phẩm và tồn kho</p>
            <ul>
              <li>Thêm/sửa/xóa sản phẩm</li>
              <li>Tìm kiếm theo category</li>
              <li>Quản lý stock</li>
            </ul>
          </mat-card-content>
          <mat-card-actions>
            <button mat-raised-button color="primary" routerLink="/products">
              <mat-icon>arrow_forward</mat-icon>
              Quản lý Products
            </button>
          </mat-card-actions>
        </mat-card>

        <mat-card class="feature-card">
          <mat-card-header>
            <mat-icon mat-card-avatar>shopping_cart</mat-icon>
            <mat-card-title>Order Service</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <p>Quản lý đơn hàng với RabbitMQ</p>
            <ul>
              <li>Tạo đơn hàng mới</li>
              <li>Theo dõi trạng thái</li>
              <li>Event-driven với RabbitMQ</li>
            </ul>
          </mat-card-content>
          <mat-card-actions>
            <button mat-raised-button color="primary" routerLink="/orders">
              <mat-icon>arrow_forward</mat-icon>
              Quản lý Orders
            </button>
          </mat-card-actions>
        </mat-card>
      </div>

      <mat-card class="info-card">
        <mat-card-header>
          <mat-card-title>Kiến Trúc Hệ Thống</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <p><strong>API Gateway RabbitMQ:</strong> {{ apiGatewayUrl }}</p>
          <p><strong>User Service:</strong> {{ userServiceUrl1 }}, {{ userServiceUrl2 }}</p>
          <p><strong>Product Service:</strong> {{ productServiceUrl1 }}, {{ productServiceUrl2 }}</p>
          <p><strong>Order Service:</strong> {{ orderServiceUrl1 }}, {{ orderServiceUrl2 }}</p>
          <p><strong>Database:</strong> {{ databaseInfo }}</p>
          <p><strong>Message Queue:</strong> RabbitMQ ({{ rabbitMQInfo }})</p>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .home-container {
      padding: 20px;
    }
    h1 {
      text-align: center;
      color: #3f51b5;
      margin-bottom: 10px;
    }
    .subtitle {
      text-align: center;
      color: #666;
      margin-bottom: 30px;
    }
    .cards-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 20px;
      margin-bottom: 30px;
    }
    .feature-card {
      height: 100%;
    }
    .feature-card ul {
      margin: 10px 0;
      padding-left: 20px;
    }
    .feature-card li {
      margin: 5px 0;
    }
    .info-card {
      margin-top: 20px;
    }
    .info-card p {
      margin: 8px 0;
    }
  `]
})
export class HomeComponent {
  // Expose environment variables to template
  apiGatewayUrl = environment.apiGatewayUrl;
  userServiceUrl1 = environment.userServiceUrl1;
  userServiceUrl2 = environment.userServiceUrl2;
  productServiceUrl1 = environment.productServiceUrl1;
  productServiceUrl2 = environment.productServiceUrl2;
  orderServiceUrl1 = environment.orderServiceUrl1;
  orderServiceUrl2 = environment.orderServiceUrl2;
  databaseInfo = `${environment.database.userService}, ${environment.database.productService}, ${environment.database.orderService}`;
  rabbitMQInfo = `${environment.rabbitMQ.host}:${environment.rabbitMQ.port}`;
}

