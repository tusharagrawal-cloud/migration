import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AdminAuthService } from '../core/services/admin-auth.service';

/**
 * Shallow, obvious navigation — Dashboard, Homepage, Products, Matches,
 * Bundles, Learn, Webinars, and a clear Logout. No nested menus, no settings
 * architecture (Milestone 11, Section 4).
 */
@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="admin-root admin-shell">
      <aside class="admin-sidebar" [class.open]="menuOpen()">
        <div class="admin-brand">ONE77 Admin</div>
        <nav class="admin-nav" aria-label="Admin navigation">
          <a routerLink="/admin/dashboard" routerLinkActive="active" (click)="closeMenu()">Dashboard</a>
          <a routerLink="/admin/homepage" routerLinkActive="active" (click)="closeMenu()">Homepage</a>
          <a routerLink="/admin/products" routerLinkActive="active" (click)="closeMenu()">Products</a>
          <a routerLink="/admin/match" routerLinkActive="active" (click)="closeMenu()">Matches</a>
          <a routerLink="/admin/bundles" routerLinkActive="active" (click)="closeMenu()">Bundles</a>
          <a routerLink="/admin/learn" routerLinkActive="active" (click)="closeMenu()">Learn</a>
          <a routerLink="/admin/webinars" routerLinkActive="active" (click)="closeMenu()">Webinars</a>
        </nav>
      </aside>

      <div class="admin-body">
        <header class="admin-topbar">
          <button type="button" class="admin-menu-toggle" (click)="toggleMenu()" [attr.aria-expanded]="menuOpen()" aria-controls="admin-nav">
            <span class="visually-hidden">Menu</span>
            <span aria-hidden="true">☰</span>
          </button>
          <div class="admin-topbar-spacer"></div>
          @if (authService.currentAdmin(); as admin) {
            <span class="admin-current-user">{{ admin.name }}</span>
          }
          <button type="button" class="admin-btn admin-btn-secondary admin-btn-sm" (click)="logout()">Log out</button>
        </header>

        <main class="admin-content">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styles: [
    `
      .admin-shell {
        display: flex;
        min-height: 100vh;
      }
      .admin-sidebar {
        width: 220px;
        flex-shrink: 0;
        background: var(--admin-surface);
        border-right: 1px solid var(--admin-border);
        padding: var(--admin-space-5) 0;
      }
      .admin-brand {
        font-weight: 700;
        font-size: 1.0625rem;
        padding: 0 var(--admin-space-5) var(--admin-space-5);
      }
      .admin-nav {
        display: flex;
        flex-direction: column;
      }
      .admin-nav a {
        padding: var(--admin-space-3) var(--admin-space-5);
        text-decoration: none;
        color: var(--admin-text-muted);
        font-size: 0.9375rem;
        font-weight: 500;
        border-left: 3px solid transparent;
      }
      .admin-nav a:hover {
        background: var(--admin-surface-hover);
        color: var(--admin-text);
      }
      .admin-nav a.active {
        color: var(--admin-accent);
        border-left-color: var(--admin-accent);
        background: var(--admin-surface-hover);
      }
      .admin-body {
        flex: 1;
        min-width: 0;
        display: flex;
        flex-direction: column;
      }
      .admin-topbar {
        display: flex;
        align-items: center;
        gap: var(--admin-space-4);
        padding: var(--admin-space-4) var(--admin-space-6);
        background: var(--admin-surface);
        border-bottom: 1px solid var(--admin-border);
      }
      .admin-topbar-spacer {
        flex: 1;
      }
      .admin-current-user {
        font-size: 0.875rem;
        color: var(--admin-text-muted);
      }
      .admin-menu-toggle {
        display: none;
        background: none;
        border: 1px solid var(--admin-border);
        border-radius: var(--admin-radius-sm);
        width: 36px;
        height: 36px;
        cursor: pointer;
        font-size: 1rem;
      }
      .admin-content {
        flex: 1;
        padding: var(--admin-space-6);
      }

      @media (max-width: 860px) {
        .admin-sidebar {
          position: fixed;
          inset: 0 auto 0 0;
          z-index: 100;
          transform: translateX(-100%);
          transition: transform 160ms ease;
        }
        .admin-sidebar.open {
          transform: translateX(0);
        }
        .admin-menu-toggle {
          display: block;
        }
        .admin-content {
          padding: var(--admin-space-4);
        }
      }
    `,
  ],
})
export class AdminShell {
  protected readonly authService = inject(AdminAuthService);
  private readonly router = inject(Router);
  protected readonly menuOpen = signal(false);

  protected toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }

  protected logout(): void {
    this.authService.logout();
    this.router.navigate(['/admin/login']);
  }
}
