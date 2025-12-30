import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { CreateUser } from '../../services/api.service';

@Component({
  selector: 'app-user-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule
  ],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Chỉnh Sửa User' : 'Thêm User Mới' }}</h2>
    <mat-dialog-content>
      <form #userForm="ngForm">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Username</mat-label>
          <input matInput [(ngModel)]="user.username" name="username" required>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Email</mat-label>
          <input matInput type="email" [(ngModel)]="user.email" name="email" required>
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
export class UserDialogComponent {
  user: CreateUser = {
    username: '',
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    phoneNumber: ''
  };

  constructor(
    public dialogRef: MatDialogRef<UserDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreateUser | null
  ) {
    if (data) {
      this.user = { ...data };
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.isFormValid()) {
      this.dialogRef.close(this.user);
    }
  }

  isFormValid(): boolean {
    return !!(
      this.user.username &&
      this.user.email &&
      this.user.firstName &&
      this.user.lastName &&
      (this.data || this.user.password) // Password chỉ bắt buộc khi tạo mới
    );
  }
}

