import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { CreateUser, UserAddress, CreateUserAddress, ApiService } from '../../services/api.service';

@Component({
  selector: 'app-user-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatTabsModule,
    MatChipsModule,
    MatIconModule
  ],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Chỉnh Sửa User' : 'Thêm User Mới' }}</h2>
    <mat-dialog-content style="max-height: 70vh; overflow-y: auto;">
      <mat-tab-group *ngIf="data && userId">
        <mat-tab label="Thông Tin">
          <form #userForm="ngForm" style="padding: 20px;">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Username</mat-label>
              <input matInput [(ngModel)]="user.username" name="username" required [disabled]="!!data">
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Email</mat-label>
              <input matInput type="email" [(ngModel)]="user.email" name="email" required [disabled]="!!data">
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width" *ngIf="!data">
              <mat-label>Password</mat-label>
              <input matInput type="password" [(ngModel)]="user.password" name="password" required>
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Họ</mat-label>
              <input matInput [(ngModel)]="user.firstName" name="firstName" required>
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Tên</mat-label>
              <input matInput [(ngModel)]="user.lastName" name="lastName" required>
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Số điện thoại</mat-label>
              <input matInput [(ngModel)]="user.phoneNumber" name="phoneNumber">
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Vai trò</mat-label>
              <mat-select [(ngModel)]="user.role" name="role">
                <mat-option value="Customer">Customer</mat-option>
                <mat-option value="Admin">Admin</mat-option>
                <mat-option value="Manager">Manager</mat-option>
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Avatar URL</mat-label>
              <input matInput [(ngModel)]="avatarUrl" name="avatarUrl" placeholder="https://...">
            </mat-form-field>
            <div *ngIf="avatarUrl" style="margin-bottom: 10px;">
              <img [src]="avatarUrl" alt="Avatar" style="width: 100px; height: 100px; border-radius: 50%; object-fit: cover;">
            </div>
          </form>
        </mat-tab>
        <mat-tab label="Địa Chỉ" *ngIf="data && userId">
          <div style="padding: 20px;">
            <button mat-raised-button color="primary" (click)="openAddAddressDialog()" style="margin-bottom: 20px;">
              <mat-icon>add</mat-icon>
              Thêm Địa Chỉ
            </button>
            <div *ngFor="let address of addresses" style="margin-bottom: 15px; padding: 15px; border: 1px solid #ddd; border-radius: 4px;">
              <div style="display: flex; justify-content: space-between; align-items: start;">
                <div>
                  <strong>{{ address.fullName }}</strong>
                  <span *ngIf="address.isDefault" style="margin-left: 10px; color: #1976d2; font-size: 12px;">(Mặc định)</span>
                  <p style="margin: 5px 0;">{{ address.phoneNumber }}</p>
                  <p style="margin: 5px 0; color: #666;">{{ address.street }}, {{ address.city }}, {{ address.state }} {{ address.postalCode }}, {{ address.country }}</p>
                </div>
                <div>
                  <button mat-icon-button (click)="editAddress(address)" color="primary">
                    <mat-icon>edit</mat-icon>
                  </button>
                  <button mat-icon-button (click)="deleteAddress(address.id)" color="warn">
                    <mat-icon>delete</mat-icon>
                  </button>
                </div>
              </div>
            </div>
            <p *ngIf="addresses.length === 0" style="color: #999; text-align: center; padding: 20px;">Chưa có địa chỉ nào</p>
          </div>
        </mat-tab>
      </mat-tab-group>
      <form #userForm="ngForm" *ngIf="!data || !userId">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Username</mat-label>
          <input matInput [(ngModel)]="user.username" name="username" required>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Email</mat-label>
          <input matInput type="email" [(ngModel)]="user.email" name="email" required>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Password</mat-label>
          <input matInput type="password" [(ngModel)]="user.password" name="password" required>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Họ</mat-label>
          <input matInput [(ngModel)]="user.firstName" name="firstName" required>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Tên</mat-label>
          <input matInput [(ngModel)]="user.lastName" name="lastName" required>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Số điện thoại</mat-label>
          <input matInput [(ngModel)]="user.phoneNumber" name="phoneNumber">
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Vai trò</mat-label>
          <mat-select [(ngModel)]="user.role" name="role">
            <mat-option value="Customer">Customer</mat-option>
            <mat-option value="Admin">Admin</mat-option>
            <mat-option value="Manager">Manager</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Avatar URL</mat-label>
          <input matInput [(ngModel)]="avatarUrl" name="avatarUrl" placeholder="https://...">
        </mat-form-field>
        <div *ngIf="avatarUrl" style="margin-bottom: 10px;">
          <img [src]="avatarUrl" alt="Avatar" style="width: 100px; height: 100px; border-radius: 50%; object-fit: cover;">
        </div>
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
      min-width: 400px;
      padding: 20px;
    }
    .full-width {
      width: 100%;
      margin-bottom: 10px;
    }
    mat-form-field {
      display: block;
    }
  `]
})
export class UserDialogComponent implements OnInit {
  user: CreateUser & { role?: string } = {
    username: '',
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    phoneNumber: '',
    role: 'Customer'
  };
  avatarUrl: string = '';
  addresses: UserAddress[] = [];
  userId?: number;

  constructor(
    public dialogRef: MatDialogRef<UserDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private apiService: ApiService
  ) {
    if (data) {
      if (data.id) {
        // Đây là User object từ API
        this.userId = data.id;
        this.user = {
          username: data.username,
          email: data.email,
          password: '', // Không cần password khi edit
          firstName: data.firstName,
          lastName: data.lastName,
          phoneNumber: data.phoneNumber,
          role: data.role || 'Customer'
        };
        this.avatarUrl = data.avatarUrl || '';
        this.addresses = data.addresses || [];
      } else {
        // Đây là CreateUser
        this.user = { ...data, role: data.role || 'Customer' };
      }
    }
  }

  ngOnInit() {
    if (this.userId) {
      this.loadAddresses();
    }
  }

  loadAddresses() {
    if (this.userId) {
      this.apiService.getUserAddresses(this.userId).subscribe({
        next: (addresses) => {
          this.addresses = addresses;
        },
        error: (err) => {
          console.error('Error loading addresses:', err);
        }
      });
    }
  }

  openAddAddressDialog() {
    // TODO: Implement address dialog
    alert('Tính năng thêm địa chỉ sẽ được triển khai');
  }

  editAddress(address: UserAddress) {
    // TODO: Implement edit address dialog
    alert('Tính năng chỉnh sửa địa chỉ sẽ được triển khai');
  }

  deleteAddress(addressId: number) {
    if (confirm('Bạn có chắc muốn xóa địa chỉ này?')) {
      if (this.userId) {
        this.apiService.deleteUserAddress(this.userId, addressId).subscribe({
          next: () => {
            this.loadAddresses();
          },
          error: (err) => {
            console.error('Error deleting address:', err);
            alert('Lỗi khi xóa địa chỉ');
          }
        });
      }
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.isFormValid()) {
      const result: any = { ...this.user };
      if (this.avatarUrl) {
        result.avatarUrl = this.avatarUrl;
      }
      this.dialogRef.close(result);
    }
  }

  isFormValid(): boolean {
    return !!(
      this.user.username &&
      this.user.email &&
      this.user.firstName &&
      this.user.lastName &&
      (this.data?.id || this.user.password) // Password chỉ bắt buộc khi tạo mới
    );
  }
}

