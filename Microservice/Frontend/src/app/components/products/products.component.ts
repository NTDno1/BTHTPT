import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { ApiService, Product, CreateProduct } from '../../services/api.service';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { ProductDialogComponent } from './product-dialog.component';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-products',
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
    MatChipsModule
  ],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>Quản Lý Sản Phẩm</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <div class="actions">
          <button 
            mat-raised-button 
            color="primary" 
            (click)="openCreateDialog()"
            [disabled]="!authService.isAdmin()">
            <mat-icon>add</mat-icon>
            Thêm Sản Phẩm Mới
          </button>
        </div>
        <div class="filters">
          <mat-form-field>
            <mat-label>Category</mat-label>
            <mat-select [(ngModel)]="selectedCategory" (selectionChange)="filterByCategory()">
              <mat-option value="">Tất cả</mat-option>
              <mat-option value="Electronics">Electronics</mat-option>
              <mat-option value="Clothing">Clothing</mat-option>
              <mat-option value="Books">Books</mat-option>
            </mat-select>
          </mat-form-field>
          <button mat-button (click)="loadProducts()">
            <mat-icon>refresh</mat-icon>
            Refresh
          </button>
        </div>

        <table mat-table [dataSource]="products" class="mat-elevation-z8">
          <ng-container matColumnDef="id">
            <th mat-header-cell *matHeaderCellDef>ID</th>
            <td mat-cell *matCellDef="let product">{{ product.id }}</td>
          </ng-container>

          <ng-container matColumnDef="name">
            <th mat-header-cell *matHeaderCellDef>Tên Sản Phẩm</th>
            <td mat-cell *matCellDef="let product">{{ product.name }}</td>
          </ng-container>

          <ng-container matColumnDef="category">
            <th mat-header-cell *matHeaderCellDef>Category</th>
            <td mat-cell *matCellDef="let product">
              <mat-chip>{{ product.category }}</mat-chip>
            </td>
          </ng-container>

          <ng-container matColumnDef="price">
            <th mat-header-cell *matHeaderCellDef>Giá</th>
            <td mat-cell *matCellDef="let product">
              <div *ngIf="product.hasDiscount" style="text-decoration: line-through; color: #999; font-size: 0.9em;">
                {{ product.price | number }} VNĐ
              </div>
              <div [style.color]="product.hasDiscount ? '#d32f2f' : 'inherit'" [style.font-weight]="product.hasDiscount ? 'bold' : 'normal'">
                {{ (product.discountPrice || product.price) | number }} VNĐ
              </div>
              <span *ngIf="product.hasDiscount" class="discount-badge">
                -{{ ((product.price - (product.discountPrice || 0)) / product.price * 100).toFixed(0) }}%
              </span>
            </td>
          </ng-container>

          <ng-container matColumnDef="rating">
            <th mat-header-cell *matHeaderCellDef>Đánh Giá</th>
            <td mat-cell *matCellDef="let product">
              <div style="display: flex; align-items: center; gap: 5px;">
                <mat-icon *ngFor="let i of [1,2,3,4,5]" 
                          [style.color]="i <= (product.averageRating || 0) ? '#ffc107' : '#ddd'"
                          style="font-size: 18px; width: 18px; height: 18px;">
                  {{ i <= (product.averageRating || 0) ? 'star' : 'star_border' }}
                </mat-icon>
                <span style="margin-left: 5px; font-size: 0.9em; color: #666;">
                  {{ (product.averageRating || 0).toFixed(1) }} ({{ product.reviewCount || 0 }})
                </span>
              </div>
            </td>
          </ng-container>

          <ng-container matColumnDef="tags">
            <th mat-header-cell *matHeaderCellDef>Tags</th>
            <td mat-cell *matCellDef="let product">
              <div style="display: flex; flex-wrap: wrap; gap: 5px;">
                <mat-chip *ngFor="let tag of product.tags" style="font-size: 0.8em;">{{ tag }}</mat-chip>
              </div>
            </td>
          </ng-container>

          <ng-container matColumnDef="stock">
            <th mat-header-cell *matHeaderCellDef>Tồn Kho</th>
            <td mat-cell *matCellDef="let product">
              <span [class]="product.stock > 0 ? 'in-stock' : 'out-of-stock'">
                {{ product.stock }}
              </span>
            </td>
          </ng-container>

          <ng-container matColumnDef="isAvailable">
            <th mat-header-cell *matHeaderCellDef>Trạng Thái</th>
            <td mat-cell *matCellDef="let product">
              <span [class]="product.isAvailable ? 'available' : 'unavailable'">
                {{ product.isAvailable ? 'Có sẵn' : 'Hết hàng' }}
              </span>
            </td>
          </ng-container>

          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef>Thao Tác</th>
            <td mat-cell *matCellDef="let product">
              <button 
                mat-icon-button 
                color="primary" 
                (click)="editProduct(product)"
                *ngIf="authService.isAdmin()">
                <mat-icon>edit</mat-icon>
              </button>
              <button 
                mat-icon-button 
                color="warn" 
                (click)="deleteProduct(product.id)"
                *ngIf="authService.isAdmin()">
                <mat-icon>delete</mat-icon>
              </button>
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
          <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
        </table>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .actions {
      margin-bottom: 20px;
      display: flex;
      gap: 10px;
    }
    .filters {
      display: flex;
      gap: 20px;
      margin-bottom: 20px;
      align-items: center;
    }
    table {
      width: 100%;
    }
    .in-stock {
      color: green;
      font-weight: bold;
    }
    .out-of-stock {
      color: red;
      font-weight: bold;
    }
    .available {
      color: green;
    }
    .unavailable {
      color: red;
    }
    .discount-badge {
      display: inline-block;
      background: #d32f2f;
      color: white;
      padding: 2px 6px;
      border-radius: 3px;
      font-size: 0.75em;
      font-weight: bold;
      margin-left: 5px;
    }
  `]
})
export class ProductsComponent implements OnInit {
  products: Product[] = [];
  displayedColumns: string[] = ['id', 'name', 'category', 'price', 'rating', 'tags', 'stock', 'isAvailable', 'actions'];
  selectedCategory: string = '';

  constructor(
    private apiService: ApiService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog,
    public authService: AuthService, // public để dùng trong template
    private router: Router
  ) {}

  ngOnInit() {
    // Kiểm tra authentication
    if (!this.authService.isAuthenticated()) {
      this.snackBar.open('Vui lòng đăng nhập để xem sản phẩm', 'Đóng', { duration: 3000 });
      this.router.navigate(['/login']);
      return;
    }
    this.loadProducts();
  }

  loadProducts() {
    this.apiService.getProducts().subscribe({
      next: (data) => {
        // Đảm bảo các fields mới có giá trị mặc định
        this.products = data.map(p => ({
          ...p,
          discountPrice: p.discountPrice || undefined,
          hasDiscount: p.hasDiscount || false,
          tags: p.tags || [],
          averageRating: p.averageRating || 0,
          reviewCount: p.reviewCount || 0
        }));
      },
      error: (err) => {
        this.snackBar.open('Lỗi khi tải danh sách sản phẩm', 'Đóng', { duration: 3000 });
        console.error(err);
      }
    });
  }

  filterByCategory() {
    if (this.selectedCategory) {
      this.apiService.getProductsByCategory(this.selectedCategory).subscribe({
        next: (data) => {
          // Đảm bảo các fields mới có giá trị mặc định
          this.products = data.map(p => ({
            ...p,
            discountPrice: p.discountPrice || undefined,
            hasDiscount: p.hasDiscount || false,
            tags: p.tags || [],
            averageRating: p.averageRating || 0,
            reviewCount: p.reviewCount || 0
          }));
        },
        error: (err) => {
          this.snackBar.open('Lỗi khi lọc sản phẩm', 'Đóng', { duration: 3000 });
          console.error(err);
        }
      });
    } else {
      this.loadProducts();
    }
  }

  openCreateDialog() {
    // Kiểm tra authentication trước khi mở dialog
    if (!this.authService.isAuthenticated()) {
      this.snackBar.open('Vui lòng đăng nhập để tạo sản phẩm', 'Đóng', { duration: 3000 });
      this.router.navigate(['/login']);
      return;
    }

    // Kiểm tra role - chỉ Admin mới được tạo sản phẩm
    if (!this.authService.isAdmin()) {
      this.snackBar.open('Chỉ Admin mới có quyền tạo sản phẩm', 'Đóng', { duration: 3000 });
      return;
    }

    const dialogRef = this.dialog.open(ProductDialogComponent, {
      width: '600px',
      data: null
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.apiService.createProduct(result).subscribe({
          next: () => {
            this.snackBar.open('Tạo sản phẩm thành công', 'Đóng', { duration: 2000 });
            this.loadProducts();
          },
          error: (err) => {
            this.snackBar.open('Lỗi khi tạo sản phẩm: ' + (err.error?.message || 'Unknown error'), 'Đóng', { duration: 3000 });
            console.error(err);
          }
        });
      }
    });
  }

  editProduct(product: Product) {
    // Kiểm tra authentication
    if (!this.authService.isAuthenticated()) {
      this.snackBar.open('Vui lòng đăng nhập để chỉnh sửa sản phẩm', 'Đóng', { duration: 3000 });
      this.router.navigate(['/login']);
      return;
    }

    // Kiểm tra role - chỉ Admin mới được sửa sản phẩm
    if (!this.authService.isAdmin()) {
      this.snackBar.open('Chỉ Admin mới có quyền chỉnh sửa sản phẩm', 'Đóng', { duration: 3000 });
      return;
    }

    const productData: CreateProduct = {
      name: product.name,
      description: product.description,
      price: product.price,
      stock: product.stock,
      category: product.category,
      imageUrl: product.imageUrl
    };

    const dialogRef = this.dialog.open(ProductDialogComponent, {
      width: '600px',
      data: productData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.apiService.updateProduct(product.id, result).subscribe({
          next: () => {
            this.snackBar.open('Cập nhật sản phẩm thành công', 'Đóng', { duration: 2000 });
            this.loadProducts();
          },
          error: (err) => {
            this.snackBar.open('Lỗi khi cập nhật sản phẩm: ' + (err.error?.message || 'Unknown error'), 'Đóng', { duration: 3000 });
            console.error(err);
          }
        });
      }
    });
  }

  deleteProduct(id: number) {
    // Kiểm tra authentication
    if (!this.authService.isAuthenticated()) {
      this.snackBar.open('Vui lòng đăng nhập để xóa sản phẩm', 'Đóng', { duration: 3000 });
      this.router.navigate(['/login']);
      return;
    }

    // Kiểm tra role - chỉ Admin mới được xóa sản phẩm
    if (!this.authService.isAdmin()) {
      this.snackBar.open('Chỉ Admin mới có quyền xóa sản phẩm', 'Đóng', { duration: 3000 });
      return;
    }

    if (confirm('Bạn có chắc muốn xóa sản phẩm này?')) {
      this.apiService.deleteProduct(id).subscribe({
        next: () => {
          this.snackBar.open('Xóa sản phẩm thành công', 'Đóng', { duration: 2000 });
          this.loadProducts();
        },
        error: (err) => {
          this.snackBar.open('Lỗi khi xóa sản phẩm', 'Đóng', { duration: 3000 });
          console.error(err);
        }
      });
    }
  }
}

