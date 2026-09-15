import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminCard } from '../../shared/admin-card';

/**
 * Deliberately light — a starting point, not a KPI dashboard (Milestone 11,
 * Section 6). No new backend aggregation was built to populate this.
 */
@Component({
  selector: 'app-admin-dashboard-page',
  imports: [RouterLink, AdminCard],
  template: `
    <h1>Dashboard</h1>
    <p class="lede">Manage ONE77's homepage, products, matches, bundles, and content.</p>

    <div class="cards">
      <a routerLink="/admin/homepage" class="card-link">
        <app-admin-card>
          <p class="card-title">Homepage</p>
          <p class="card-body">Control the homepage hero photograph.</p>
        </app-admin-card>
      </a>
      <a routerLink="/admin/products" class="card-link">
        <app-admin-card>
          <p class="card-title">Products</p>
          <p class="card-body">Manage each product's ONE77 details and specifications.</p>
        </app-admin-card>
      </a>
      <a routerLink="/admin/match" class="card-link">
        <app-admin-card>
          <p class="card-title">Matches</p>
          <p class="card-body">Choose which pellets and accessories go with each airgun.</p>
        </app-admin-card>
      </a>
      <a routerLink="/admin/bundles" class="card-link">
        <app-admin-card>
          <p class="card-title">Bundles</p>
          <p class="card-body">Curate product groupings for the storefront.</p>
        </app-admin-card>
      </a>
      <a routerLink="/admin/learn" class="card-link">
        <app-admin-card>
          <p class="card-title">Learn</p>
          <p class="card-body">Manage guide categories and articles.</p>
        </app-admin-card>
      </a>
      <a routerLink="/admin/webinars" class="card-link">
        <app-admin-card>
          <p class="card-title">Webinars</p>
          <p class="card-body">Schedule events and view registrations.</p>
        </app-admin-card>
      </a>
    </div>
  `,
  styles: [
    `
      h1 {
        font-size: 1.5rem;
        margin-bottom: var(--admin-space-2);
      }
      .lede {
        color: var(--admin-text-muted);
        margin-bottom: var(--admin-space-6);
      }
      .cards {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
        gap: var(--admin-space-4);
      }
      .card-link {
        text-decoration: none;
        color: inherit;
      }
      .card-title {
        margin: 0;
        font-weight: 600;
        font-size: 1.0625rem;
      }
      .card-body {
        margin: var(--admin-space-2) 0 0;
        color: var(--admin-text-muted);
        font-size: 0.875rem;
      }
    `,
  ],
})
export class AdminDashboardPage {}
