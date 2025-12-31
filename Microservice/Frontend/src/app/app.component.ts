import { Component, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { CommonModule } from '@angular/common';
import { AuthService, User } from './services/auth.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    CommonModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatSidenavModule,
    MatListModule,
    MatMenuModule,
    MatDividerModule
  ],
  template: `
    <mat-toolbar color="primary">
      <button mat-icon-button (click)="sidenav.toggle()">
        <mat-icon>menu</mat-icon>
      </button>
      <span>Microservice E-Commerce Demo</span>
      <span class="spacer"></span>
      
      @if (currentUser) {
        <button mat-button [matMenuTriggerFor]="userMenu">
          <mat-icon>account_circle</mat-icon>
          <span>{{ currentUser.firstName }} {{ currentUser.lastName }}</span>
          <mat-icon>arrow_drop_down</mat-icon>
        </button>
        <mat-menu #userMenu="matMenu">
          <div class="user-info">
            <div class="user-name">{{ currentUser.firstName }} {{ currentUser.lastName }}</div>
            <div class="user-email">{{ currentUser.email }}</div>
            <div class="user-role">Role: {{ currentUser.role }}</div>
          </div>
          <mat-divider></mat-divider>
          <button mat-menu-item (click)="logout()">
            <mat-icon>logout</mat-icon>
            <span>Đăng xuất</span>
          </button>
        </mat-menu>
      } @else {
        <button mat-button routerLink="/login">
          <mat-icon>login</mat-icon>
          <span>Đăng Nhập</span>
        </button>
        <button mat-button routerLink="/register">
          <mat-icon>person_add</mat-icon>
          <span>Đăng Ký</span>
        </button>
      }
      
      <button mat-button routerLink="/">
        <mat-icon>home</mat-icon>
        Home
      </button>
    </mat-toolbar>

    <mat-sidenav-container>
      <mat-sidenav #sidenav mode="side" opened>
        <mat-nav-list>
          @if (currentUser) {
            <a mat-list-item routerLink="/products" routerLinkActive="active">
              <mat-icon>inventory</mat-icon>
              <span>Products</span>
            </a>
            <a mat-list-item routerLink="/users" routerLinkActive="active">
              <mat-icon>people</mat-icon>
              <span>Users</span>
            </a>
            <a mat-list-item routerLink="/orders" routerLinkActive="active">
              <mat-icon>shopping_cart</mat-icon>
              <span>Orders</span>
            </a>
          }
        </mat-nav-list>
      </mat-sidenav>

      <mat-sidenav-content>
        <div class="container">
          <router-outlet></router-outlet>
        </div>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: [`
    .spacer {
      flex: 1 1 auto;
    }
    mat-sidenav-container {
      height: calc(100vh - 64px);
    }
    mat-sidenav {
      width: 250px;
    }
    .active {
      background-color: rgba(63, 81, 181, 0.1);
    }
    mat-nav-list a {
      display: flex;
      align-items: center;
      gap: 10px;
    }
    .user-info {
      padding: 16px;
      min-width: 200px;
    }
    .user-name {
      font-weight: 500;
      font-size: 16px;
      margin-bottom: 4px;
    }
    .user-email {
      font-size: 14px;
      color: rgba(0, 0, 0, 0.6);
      margin-bottom: 4px;
    }
    .user-role {
      font-size: 12px;
      color: rgba(0, 0, 0, 0.5);
      text-transform: capitalize;
    }
  `]
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'Microservice E-Commerce Demo';
  currentUser: User | null = null;
  private userSubscription?: Subscription;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.userSubscription = this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
  }

  ngOnDestroy(): void {
    this.userSubscription?.unsubscribe();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}

