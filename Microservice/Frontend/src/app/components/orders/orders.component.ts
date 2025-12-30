import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { ApiService, Order, CreateOrder, User, Product } from '../../services/api.service';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    FormsModule,
    MatSnackBarModule,
    MatCardModule,
    MatChipsModule,
    MatExpansionModule
  ],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>Quản Lý Đơn Hàng</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <div class="actions">
          <button mat-raised-button color="primary" (click)="openCreateDialog()">
            <mat-icon>add</mat-icon>
            Tạo Đơn Hàng Mới
          </button>
          <button mat-button (click)="loadOrders()">
            <mat-icon>refresh</mat-icon>
            Refresh
          </button>
        </div>

        <div *ngFor="let order of orders" class="order-card">
          <mat-expansion-panel>
            <mat-expansion-panel-header>
              <mat-panel-title>
                Đơn hàng #{{ order.id }} - {{ order.totalAmount | number }} VNĐ
              </mat-panel-title>
              <mat-panel-description>
                <mat-chip [color]="getStatusColor(order.status)">{{ order.status }}</mat-chip>
              </mat-panel-description>
            </mat-expansion-panel-header>
            
            <div class="order-details">
              <p><strong>User ID:</strong> {{ order.userId }}</p>
              <p><strong>Địa chỉ giao hàng:</strong> {{ order.shippingAddress }}</p>
              <p><strong>Ngày tạo:</strong> {{ order.createdAt | date:'short' }}</p>
              
              <h4>Chi tiết sản phẩm:</h4>
              <table mat-table [dataSource]="order.orderItems" class="items-table">
                <ng-container matColumnDef="productName">
                  <th mat-header-cell *matHeaderCellDef>Tên Sản Phẩm</th>
                  <td mat-cell *matCellDef="let item">{{ item.productName }}</td>
                </ng-container>
                <ng-container matColumnDef="quantity">
                  <th mat-header-cell *matHeaderCellDef>Số Lượng</th>
                  <td mat-cell *matCellDef="let item">{{ item.quantity }}</td>
                </ng-container>
                <ng-container matColumnDef="unitPrice">
                  <th mat-header-cell *matHeaderCellDef>Đơn Giá</th>
                  <td mat-cell *matCellDef="let item">{{ item.unitPrice | number }} VNĐ</td>
                </ng-container>
                <ng-container matColumnDef="subTotal">
                  <th mat-header-cell *matHeaderCellDef>Thành Tiền</th>
                  <td mat-cell *matCellDef="let item">{{ item.subTotal | number }} VNĐ</td>
                </ng-container>
                <tr mat-header-row *matHeaderRowDef="itemColumns"></tr>
                <tr mat-row *matRowDef="let row; columns: itemColumns;"></tr>
              </table>

              <div class="order-actions">
                <mat-form-field>
                  <mat-label>Trạng thái</mat-label>
                  <mat-select [(ngModel)]="order.status" (selectionChange)="updateStatus(order.id, order.status)">
                    <mat-option value="Pending">Pending</mat-option>
                    <mat-option value="Processing">Processing</mat-option>
                    <mat-option value="Shipped">Shipped</mat-option>
                    <mat-option value="Delivered">Delivered</mat-option>
                    <mat-option value="Cancelled">Cancelled</mat-option>
                  </mat-select>
                </mat-form-field>
                <button mat-icon-button color="warn" (click)="deleteOrder(order.id)">
                  <mat-icon>delete</mat-icon>
                </button>
              </div>
            </div>
          </mat-expansion-panel>
        </div>

        <div *ngIf="orders.length === 0" class="no-orders">
          <p>Chưa có đơn hàng nào. Hãy tạo đơn hàng mới!</p>
        </div>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .actions {
      margin-bottom: 20px;
      display: flex;
      gap: 10px;
    }
    .order-card {
      margin-bottom: 10px;
    }
    .order-details {
      padding: 10px;
    }
    .order-details p {
      margin: 5px 0;
    }
    .items-table {
      width: 100%;
      margin: 10px 0;
    }
    .order-actions {
      display: flex;
      gap: 10px;
      align-items: center;
      margin-top: 15px;
    }
    .no-orders {
      text-align: center;
      padding: 40px;
      color: #666;
    }
  `]
})
export class OrdersComponent implements OnInit {
  orders: Order[] = [];
  itemColumns: string[] = ['productName', 'quantity', 'unitPrice', 'subTotal'];

  constructor(
    private apiService: ApiService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit() {
    this.loadOrders();
  }

  loadOrders() {
    this.apiService.getOrders().subscribe({
      next: (data) => {
        this.orders = data;
      },
      error: (err) => {
        this.snackBar.open('Lỗi khi tải danh sách đơn hàng', 'Đóng', { duration: 3000 });
        console.error(err);
      }
    });
  }

  openCreateDialog() {
    this.snackBar.open('Sử dụng Swagger UI hoặc API để tạo đơn hàng mới', 'Đóng', { duration: 3000 });
  }

  updateStatus(orderId: number, status: string) {
    this.apiService.updateOrderStatus(orderId, status).subscribe({
      next: () => {
        this.snackBar.open('Cập nhật trạng thái thành công', 'Đóng', { duration: 2000 });
        this.loadOrders();
      },
      error: (err) => {
        this.snackBar.open('Lỗi khi cập nhật trạng thái', 'Đóng', { duration: 3000 });
        console.error(err);
      }
    });
  }

  deleteOrder(id: number) {
    if (confirm('Bạn có chắc muốn xóa đơn hàng này?')) {
      this.apiService.deleteOrder(id).subscribe({
        next: () => {
          this.snackBar.open('Xóa đơn hàng thành công', 'Đóng', { duration: 2000 });
          this.loadOrders();
        },
        error: (err) => {
          this.snackBar.open('Lỗi khi xóa đơn hàng', 'Đóng', { duration: 3000 });
          console.error(err);
        }
      });
    }
  }

  getStatusColor(status: string): any {
    const colors: { [key: string]: string } = {
      'Pending': 'primary',
      'Processing': 'accent',
      'Shipped': 'primary',
      'Delivered': 'primary',
      'Cancelled': 'warn'
    };
    return colors[status] || 'primary';
  }
}

