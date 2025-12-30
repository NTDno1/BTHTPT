import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { ApiService, User, CreateUser } from '../../services/api.service';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { UserDialogComponent } from './user-dialog.component';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    FormsModule,
    MatSnackBarModule,
    MatCardModule
  ],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>Quản Lý Người Dùng</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <div class="actions">
          <button mat-raised-button color="primary" (click)="openCreateDialog()">
            <mat-icon>add</mat-icon>
            Thêm User Mới
          </button>
          <button mat-button (click)="loadUsers()">
            <mat-icon>refresh</mat-icon>
            Refresh
          </button>
        </div>

        <table mat-table [dataSource]="users" class="mat-elevation-z8">
          <ng-container matColumnDef="id">
            <th mat-header-cell *matHeaderCellDef>ID</th>
            <td mat-cell *matCellDef="let user">{{ user.id }}</td>
          </ng-container>

          <ng-container matColumnDef="username">
            <th mat-header-cell *matHeaderCellDef>Username</th>
            <td mat-cell *matCellDef="let user">{{ user.username }}</td>
          </ng-container>

          <ng-container matColumnDef="email">
            <th mat-header-cell *matHeaderCellDef>Email</th>
            <td mat-cell *matCellDef="let user">{{ user.email }}</td>
          </ng-container>

          <ng-container matColumnDef="fullName">
            <th mat-header-cell *matHeaderCellDef>Họ Tên</th>
            <td mat-cell *matCellDef="let user">{{ user.firstName }} {{ user.lastName }}</td>
          </ng-container>

          <ng-container matColumnDef="isActive">
            <th mat-header-cell *matHeaderCellDef>Trạng Thái</th>
            <td mat-cell *matCellDef="let user">
              <span [class]="user.isActive ? 'active' : 'inactive'">
                {{ user.isActive ? 'Hoạt động' : 'Không hoạt động' }}
              </span>
            </td>
          </ng-container>

          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef>Thao Tác</th>
            <td mat-cell *matCellDef="let user">
              <button mat-icon-button color="primary" (click)="editUser(user)">
                <mat-icon>edit</mat-icon>
              </button>
              <button mat-icon-button color="warn" (click)="deleteUser(user.id)">
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
    table {
      width: 100%;
    }
    .active {
      color: green;
      font-weight: bold;
    }
    .inactive {
      color: red;
      font-weight: bold;
    }
  `]
})
export class UsersComponent implements OnInit {
  users: User[] = [];
  displayedColumns: string[] = ['id', 'username', 'email', 'fullName', 'isActive', 'actions'];

  constructor(
    private apiService: ApiService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {}

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.apiService.getUsers().subscribe({
      next: (data) => {
        this.users = data;
      },
      error: (err) => {
        this.snackBar.open('Lỗi khi tải danh sách users', 'Đóng', { duration: 3000 });
        console.error(err);
      }
    });
  }

  openCreateDialog() {
    const dialogRef = this.dialog.open(UserDialogComponent, {
      width: '500px',
      data: null
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.apiService.createUser(result).subscribe({
          next: () => {
            this.snackBar.open('Tạo user thành công', 'Đóng', { duration: 2000 });
            this.loadUsers();
          },
          error: (err) => {
            this.snackBar.open('Lỗi khi tạo user: ' + (err.error?.message || 'Unknown error'), 'Đóng', { duration: 3000 });
            console.error(err);
          }
        });
      }
    });
  }

  editUser(user: User) {
    const userData: CreateUser = {
      username: user.username,
      email: user.email,
      password: '', // Không cần password khi edit
      firstName: user.firstName,
      lastName: user.lastName,
      phoneNumber: user.phoneNumber
    };

    const dialogRef = this.dialog.open(UserDialogComponent, {
      width: '500px',
      data: userData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        // Loại bỏ password nếu không có thay đổi
        const updateData: Partial<User> = {
          username: result.username,
          email: result.email,
          firstName: result.firstName,
          lastName: result.lastName,
          phoneNumber: result.phoneNumber
        };
        
        this.apiService.updateUser(user.id, updateData).subscribe({
          next: () => {
            this.snackBar.open('Cập nhật user thành công', 'Đóng', { duration: 2000 });
            this.loadUsers();
          },
          error: (err) => {
            this.snackBar.open('Lỗi khi cập nhật user: ' + (err.error?.message || 'Unknown error'), 'Đóng', { duration: 3000 });
            console.error(err);
          }
        });
      }
    });
  }

  deleteUser(id: number) {
    if (confirm('Bạn có chắc muốn xóa user này?')) {
      this.apiService.deleteUser(id).subscribe({
        next: () => {
          this.snackBar.open('Xóa user thành công', 'Đóng', { duration: 2000 });
          this.loadUsers();
        },
        error: (err) => {
          this.snackBar.open('Lỗi khi xóa user', 'Đóng', { duration: 3000 });
          console.error(err);
        }
      });
    }
  }
}

