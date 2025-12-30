import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { CreateProduct } from '../../services/api.service';

@Component({
  selector: 'app-product-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule
  ],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Chỉnh Sửa Sản Phẩm' : 'Thêm Sản Phẩm Mới' }}</h2>
    <mat-dialog-content>
      <form #productForm="ngForm">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Tên Sản Phẩm</mat-label>
          <input matInput [(ngModel)]="product.name" name="name" required>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Mô Tả</mat-label>
          <textarea matInput [(ngModel)]="product.description" name="description" rows="3" required></textarea>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Category</mat-label>
          <mat-select [(ngModel)]="product.category" name="category" required>
            <mat-option value="Electronics">Electronics</mat-option>
            <mat-option value="Clothing">Clothing</mat-option>
            <mat-option value="Books">Books</mat-option>
            <mat-option value="Food">Food</mat-option>
            <mat-option value="Sports">Sports</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Giá (VNĐ)</mat-label>
          <input matInput type="number" [(ngModel)]="product.price" name="price" min="0" step="0.01" required>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Tồn Kho</mat-label>
          <input matInput type="number" [(ngModel)]="product.stock" name="stock" min="0" required>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>URL Hình Ảnh</mat-label>
          <input matInput [(ngModel)]="product.imageUrl" name="imageUrl" placeholder="https://example.com/image.jpg">
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Hủy</button>
      <button mat-raised-button color="primary" (click)="onSave()" [disabled]="!isFormValid()">
        {{ data ? 'Cập Nhật' : 'Tạo Mới' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content {
      min-width: 500px;
      padding: 20px;
    }
    .full-width {
      width: 100%;
      margin-bottom: 10px;
    }
    mat-form-field {
      display: block;
    }
    textarea {
      resize: vertical;
    }
  `]
})
export class ProductDialogComponent {
  product: CreateProduct = {
    name: '',
    description: '',
    price: 0,
    stock: 0,
    category: 'Electronics',
    imageUrl: ''
  };

  constructor(
    public dialogRef: MatDialogRef<ProductDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreateProduct | null
  ) {
    if (data) {
      this.product = { ...data };
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.isFormValid()) {
      this.dialogRef.close(this.product);
    }
  }

  isFormValid(): boolean {
    return !!(
      this.product.name &&
      this.product.description &&
      this.product.category &&
      this.product.price >= 0 &&
      this.product.stock >= 0
    );
  }
}

