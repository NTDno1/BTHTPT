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
          <button mat-raised-button color="primary" (click)="openCreateDialog()">
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
            <td mat-cell *matCellDef="let product">{{ product.price | number }} VNĐ</td>
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
              <button mat-icon-button color="primary" (click)="editProduct(product)">
                <mat-icon>edit</mat-icon>
              </button>
              <button mat-icon-button color="warn" (click)="deleteProduct(product.id)">
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
  `]
})
export class ProductsComponent implements OnInit {
  products: Product[] = [];
  displayedColumns: string[] = ['id', 'name', 'category', 'price', 'stock', 'isAvailable', 'actions'];
  selectedCategory: string = '';

  constructor(
    private apiService: ApiService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {}

  ngOnInit() {
    this.loadProducts();
  }

  loadProducts() {
    this.apiService.getProducts().subscribe({
      next: (data) => {
        this.products = data;
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
          this.products = data;
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

