import { Component, HostListener, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { Logo } from '../shared/ui/logo';
import { CartService } from '../core/services/cart.service';

interface NavGroup {
  label: string;
  items: { label: string; link: string }[];
}

const NAV_GROUPS: NavGroup[] = [
  {
    label: 'Shop',
    items: [
      { label: 'Airguns', link: '/airguns' },
      { label: 'Pellets', link: '/pellets' },
      { label: 'Accessories', link: '/accessories' },
      { label: 'Bundles', link: '/bundles' },
    ],
  },
  {
    label: 'Learn',
    items: [
      { label: 'All Guides', link: '/learn' },
      { label: 'Live Webinar', link: '/webinar' },
    ],
  },
];

/**
 * The customer-facing header — a proper ONE77 identity (real logo, brand
 * typography/accent) grouping discovery into "Shop"/"Learn" rather than
 * six equal flat links, per the storefront visual-direction brief. Never
 * rendered on /admin/* routes (see app.html/app.ts) — Admin has its own
 * shell and must not carry a second navigation bar.
 */
@Component({
  selector: 'app-header',
  imports: [RouterLink, RouterLinkActive, Logo],
  template: `
    <header class="site-header" [class.scrolled]="scrolled()">
      <div class="bar">
        <a class="brand" routerLink="/" (click)="closeMenu()">
          <app-logo variant="symbol" tone="white" class="brand-mark brand-mark-compact" />
          <app-logo variant="full" tone="white" class="brand-mark brand-mark-full" />
        </a>

        <nav class="primary-nav" aria-label="Main">
          @for (group of groups; track group.label) {
            <div class="nav-group" (mouseenter)="openGroup(group.label)" (mouseleave)="closeGroup()">
              <button
                type="button"
                class="nav-group-trigger"
                [class.open]="openGroupLabel() === group.label"
                [attr.aria-expanded]="openGroupLabel() === group.label"
                [attr.aria-controls]="'nav-dropdown-' + group.label"
                (click)="toggleGroup(group.label)"
              >
                {{ group.label }}
              </button>
              @if (openGroupLabel() === group.label) {
                <div class="nav-dropdown" [id]="'nav-dropdown-' + group.label">
                  @for (item of group.items; track item.link) {
                    <a [routerLink]="item.link" routerLinkActive="active" (click)="closeGroup()">{{ item.label }}</a>
                  }
                </div>
              }
            </div>
          }
        </nav>

        <div class="actions">
          <a routerLink="/webinar" class="reserve-cta">Free Q&amp;A</a>
          <button type="button" class="cart-btn" (click)="cart.open()" aria-label="Open cart">
            <span class="cart-icon" aria-hidden="true">🛒</span>
            @if (cart.itemCount() > 0) {
              <span class="cart-badge">{{ cart.itemCount() }}</span>
            }
          </button>
          <button
            type="button"
            class="nav-toggle"
            [attr.aria-expanded]="menuOpen()"
            aria-controls="mobile-nav"
            (click)="toggleMenu()"
          >
            <span class="visually-hidden">Menu</span>
            <span class="bars" aria-hidden="true"></span>
          </button>
        </div>
      </div>
    </header>

    @if (menuOpen()) {
      <div class="mobile-nav" id="mobile-nav">
        @for (group of groups; track group.label) {
          <div class="mobile-group">
            <p class="eyebrow">{{ group.label }}</p>
            @for (item of group.items; track item.link) {
              <a [routerLink]="item.link" routerLinkActive="active" (click)="closeMenu()">{{ item.label }}</a>
            }
          </div>
        }
        <a routerLink="/webinar" class="reserve-cta mobile-reserve" (click)="closeMenu()">Reserve a free Q&amp;A seat</a>
      </div>
    }
  `,
  styles: [
    `
      .site-header {
        position: sticky;
        top: 0;
        z-index: 100;
        background: rgba(10, 10, 10, 0.88);
        backdrop-filter: blur(10px);
        border-bottom: 1px solid transparent;
        transition: border-color var(--transition-base), background var(--transition-base);
      }
      .site-header.scrolled {
        border-bottom-color: var(--color-border-soft);
      }
      .bar {
        max-width: var(--container-width);
        margin-inline: auto;
        padding-inline: var(--container-padding-inline);
        display: flex;
        align-items: center;
        justify-content: space-between;
        height: 60px;
        gap: var(--space-5);
      }
      .brand {
        display: flex;
        align-items: center;
        height: 26px;
        flex-shrink: 0;
      }
      .brand-mark {
        height: 100%;
      }
      .brand-mark-compact {
        display: block;
      }
      .brand-mark-full {
        display: none;
      }
      @media (min-width: 560px) {
        .brand-mark-compact {
          display: none;
        }
        .brand-mark-full {
          display: block;
        }
      }

      .primary-nav {
        display: none;
        align-items: center;
        height: 100%;
        gap: var(--space-2);
      }
      @media (min-width: 900px) {
        .primary-nav {
          display: flex;
        }
      }
      .nav-group {
        position: relative;
        height: 100%;
        display: flex;
        align-items: center;
      }
     .nav-group-trigger {
  background: none;
  border: none;
  cursor: pointer;
  color: #cccccc;
  font-family: var(--font-family-heading);
  font-weight: var(--font-weight-semibold);
  font-size: 0.8125rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  padding: var(--space-2) var(--space-3);
}
.nav-group-trigger:hover,
.nav-group-trigger.open {
  color: #ffffff;
}
      .nav-dropdown {
        position: absolute;
        top: 100%;
        left: 0;
        min-width: 200px;
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        box-shadow: var(--shadow-md);
        padding: var(--space-2) 0;
      }
      .nav-dropdown a {
        display: block;
        padding: var(--space-3) var(--space-4);
        text-decoration: none;
        color: var(--color-text-muted);
        font-size: 0.9375rem;
      }
      .nav-dropdown a:hover,
      .nav-dropdown a.active {
        color: var(--color-text);
        background: rgba(255, 255, 255, 0.04);
      }

      .actions {
        display: flex;
        align-items: center;
        gap: var(--space-3);
        flex-shrink: 0;
      }
      .cart-btn {
  position: relative;
  background: none;
  border: none;
  cursor: pointer;
  color: #ffffff;
  font-size: 1.1rem;
  padding: var(--space-2);
  line-height: 1;
  flex-shrink: 0;
}
      .cart-badge {
        position: absolute;
        top: -2px;
        right: -2px;
        background: var(--color-accent);
        color: var(--color-accent-contrast);
        font-size: 0.625rem;
        font-weight: var(--font-weight-bold);
        min-width: 16px;
        height: 16px;
        border-radius: 999px;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 0 3px;
      }
      .reserve-cta {
        display: none;
        text-decoration: none;
        background: var(--color-accent);
        color: var(--color-accent-contrast);
        font-family: var(--font-family-heading);
        font-weight: var(--font-weight-semibold);
        font-size: 0.75rem;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        padding: var(--space-2) var(--space-4);
        border-radius: var(--radius-sm);
        white-space: nowrap;
      }
      .reserve-cta:hover {
        background: var(--color-accent-strong);
      }
      @media (min-width: 640px) {
        .reserve-cta {
          display: inline-block;
        }
      }

      .nav-toggle {
        background: none;
        border: 1px solid var(--color-border);
        border-radius: var(--radius-sm);
        width: 40px;
        height: 40px;
        cursor: pointer;
        flex-shrink: 0;
      }
      @media (min-width: 900px) {
        .nav-toggle {
          display: none;
        }
      }
      .bars,
      .bars::before,
      .bars::after {
        display: block;
        width: 18px;
        height: 2px;
        background: var(--color-text);
        margin: 0 auto;
        position: relative;
      }
      .bars::before {
        content: '';
        top: -6px;
      }
      .bars::after {
        content: '';
        top: 4px;
      }

      .mobile-nav {
        position: fixed;
        inset: 60px 0 0 0;
        z-index: 99;
        background: var(--color-bg);
        overflow-y: auto;
        padding: var(--space-6) var(--container-padding-inline) var(--space-8);
      }
      @media (min-width: 900px) {
        .mobile-nav {
          display: none;
        }
      }
      .mobile-group {
        padding-bottom: var(--space-6);
        margin-bottom: var(--space-6);
        border-bottom: 1px solid var(--color-border-soft);
      }
      .mobile-group .eyebrow {
        margin: 0 0 var(--space-3);
      }
      .mobile-group a {
        display: block;
        padding: var(--space-3) 0;
        text-decoration: none;
        color: var(--color-text);
        font-family: var(--font-family-heading);
        font-size: var(--font-size-lg);
        font-weight: var(--font-weight-semibold);
      }
      .mobile-group a.active {
        color: var(--color-accent);
      }
      .mobile-reserve {
        display: block;
        text-align: center;
        margin-top: var(--space-4);
      }
    `,
  ],
})
export class Header {
  protected cart = inject(CartService);
  protected groups = NAV_GROUPS;
  protected menuOpen = signal(false);
  protected scrolled = signal(false);
  protected openGroupLabel = signal<string | null>(null);

  @HostListener('window:scroll')
  protected onScroll(): void {
    this.scrolled.set(window.scrollY > 4);
  }

  protected toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }

  protected openGroup(label: string): void {
    this.openGroupLabel.set(label);
  }

  protected closeGroup(): void {
    this.openGroupLabel.set(null);
  }

  protected toggleGroup(label: string): void {
    this.openGroupLabel.update((current) => (current === label ? null : label));
  }
}