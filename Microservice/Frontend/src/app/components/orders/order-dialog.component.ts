import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { CreateOrder, User, Product } from '../../services/api.service';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-order-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatIconModule
  ],
  template: `
    <h2 mat-dialog-title>Tạo Đơn Hàng Mới</h2>
    <mat-dialog-content>
      <form #orderForm="ngForm">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Người Dùng</mat-label>
          <mat-select [(ngModel)]="order.userId" name="userId" required>
            <mat-option *ngFor="let user of users" [value]="user.id">
              {{ user.username }} ({{ user.firstName }} {{ user.lastName }})
            </mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Địa Chỉ Giao Hàng</mat-label>
          <textarea matInput [(ngModel)]="order.shippingAddress" name="shippingAddress" rows="3" required></textarea>
        </mat-form-field>

        <div class="order-items-section">
          <div class="section-header">
            <h3>Chi Tiết Sản Phẩm</h3>
            <button mat-icon-button color="primary" (click)="addOrderItem()" type="button">
              <mat-icon>add</mat-icon>
            </button>
          </div>

          <div *ngFor="let item of order.orderItems; let i = index" class="order-item">
            <mat-form-field appearance="outline" class="product-field">
              <mat-label>Sản Phẩm</mat-label>
              <mat-select [(ngModel)]="item.productId" [name]="'productId' + i" required>
                <mat-option *ngFor="let product of products" [value]="product.id">
                  {{ product.name }} - {{ product.price | number }} VNĐ (Tồn: {{ product.stock }})
                </mat-option>
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline" class="quantity-field">
              <mat-label>Số Lượng</mat-label>
              <input matInput type="number" [(ngModel)]="item.quantity" [name]="'quantity' + i" min="1" required>
            </mat-form-field>

            <button mat-icon-button color="warn" (click)="removeOrderItem(i)" type="button">
              <mat-icon>delete</mat-icon>
            </button>
          </div>

          <p *ngIf="order.orderItems.length === 0" class="no-items">Chưa có sản phẩm nào. Nhấn nút + để thêm.</p>
        </div>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Hủy</button>
      <button mat-raised-button color="primary" (click)="onSave()" [disabled]="!isFormValid()">
        Tạo Đơn Hàng
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content {
      min-width: 600px;
      max-width: 800px;
      padding: 20px;
      max-height: 70vh;
      overflow-y: auto;
    }
    .full-width {
      width: 100%;
      margin-bottom: 10px;
    }
    mat-form-field {
      display: block;
    }
    .order-items-section {
      margin-top: 20px;
      padding-top: 20px;
      border-top: 1px solid #e0e0e0;
    }
    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 15px;
    }
    .section-header h3 {
      margin: 0;
    }
    .order-item {
      display: flex;
      gap: 10px;
      align-items: flex-start;
      margin-bottom: 15px;
      padding: 10px;
      background-color: #f5f5f5;
      border-radius: 4px;
    }
    .product-field {
      flex: 1;
    }
    .quantity-field {
      width: 120px;
    }
    .no-items {
      color: #666;
      font-style: italic;
      text-align: center;
      padding: 20px;
    }
    textarea {
      resize: vertical;
    }
  `]
})
export class OrderDialogComponent implements OnInit {
  order: CreateOrder = {
    userId: 0,
    shippingAddress: '',
    orderItems: []
  };

  users: User[] = [];
  products: Product[] = [];

  constructor(
    public dialogRef: MatDialogRef<OrderDialogComponent>,
    private apiService: ApiService
  ) {}

  ngOnInit() {
    this.loadUsers();
    this.loadProducts();
    this.addOrderItem(); // Thêm một item mặc định
  }

  loadUsers() {
    this.apiService.getUsers().subscribe({
      next: (data) => {
        this.users = data.filter(u => u.isActive);
      },
      error: (err) => {
        console.error('Error loading users:', err);
      }
    });
  }

  loadProducts() {
    this.apiService.getProducts().subscribe({
      next: (data) => {
        this.products = data.filter(p => p.isAvailable && p.stock > 0);
      },
      error: (err) => {
        console.error('Error loading products:', err);
      }
    });
  }

  addOrderItem() {
    this.order.orderItems.push({
      productId: 0,
      quantity: 1
    });
  }

  removeOrderItem(index: number) {
    this.order.orderItems.splice(index, 1);
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.isFormValid()) {
      // Lọc bỏ các item không hợp lệ
      const validItems = this.order.orderItems.filter(item => item.productId > 0 && item.quantity > 0);
      this.order.orderItems = validItems;
      this.dialogRef.close(this.order);
    }
  }

  isFormValid(): boolean {
    if (!this.order.userId || !this.order.shippingAddress || this.order.orderItems.length === 0) {
      return false;
    }
    
    // Kiểm tra tất cả items đều hợp lệ
    return this.order.orderItems.every(item => 
      item.productId > 0 && 
      item.quantity > 0 &&
      item.quantity <= (this.products.find(p => p.id === item.productId)?.stock || 0)
    );
  }
}

