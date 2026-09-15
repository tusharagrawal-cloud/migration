import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminBundleService } from '../../core/services/admin-bundle.service';
import { AdminBundle } from '../../core/models/admin-bundle.models';
import { AdminCard } from '../../shared/admin-card';
import { AdminLoading, AdminEmpty, AdminError } from '../../shared/admin-states';

type LoadState = 'loading' | 'loaded' | 'error';

@Component({
  selector: 'app-admin-bundles-list-page',
  imports: [RouterLink, AdminCard, AdminLoading, AdminEmpty, AdminError],
  template: `
    <div class="header-row">
      <div>
        <h1>Bundles</h1>
        <p class="lede">Curated product groupings for the storefront.</p>
      </div>
      <a routerLink="/admin/bundles/new" class="admin-btn admin-btn-primary">+ Create bundle</a>
    </div>

    @switch (state()) {
      @case ('loading') {
        <app-admin-loading label="Loading bundles…" />
      }
      @case ('error') {
        <app-admin-error message="Unable to load bundles. Please try again." (retry)="load()" />
      }
      @case ('loaded') {
        @if (bundles().length === 0) {
          <app-admin-empty heading="No bundles yet" body="Create your first bundle to get started." />
        } @else {
          <div class="grid">
            @for (bundle of bundles(); track bundle.id) {
              <a [routerLink]="['/admin/bundles', bundle.id]" class="tile">
                <app-admin-card>
                  <p class="title">{{ bundle.name }}</p>
                  @if (bundle.tagline) {
                    <p class="tagline">{{ bundle.tagline }}</p>
                  }
                  <p class="items-count">{{ bundle.items.length }} product(s)</p>
                  <span class="admin-badge" [class]="statusBadgeClass(bundle.status)">{{ bundle.status }}</span>
                </app-admin-card>
              </a>
            }
          </div>
        }
      }
    }
  `,
  styles: [
    `
      .header-row {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: var(--admin-space-4);
        margin-bottom: var(--admin-space-6);
        flex-wrap: wrap;
      }
      h1 {
        font-size: 1.5rem;
        margin-bottom: var(--admin-space-2);
      }
      .lede {
        color: var(--admin-text-muted);
        margin: 0;
      }
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
        gap: var(--admin-space-4);
      }
      .tile {
        text-decoration: none;
        color: inherit;
      }
      .title {
        margin: 0;
        font-weight: 600;
      }
      .tagline {
        margin: var(--admin-space-1) 0;
        color: var(--admin-text-muted);
        font-size: 0.8125rem;
      }
      .items-count {
        margin: var(--admin-space-2) 0;
        font-size: 0.8125rem;
      }
    `,
  ],
})
export class AdminBundlesListPage implements OnInit {
  private readonly bundleService = inject(AdminBundleService);

  protected state = signal<LoadState>('loading');
  protected bundles = signal<AdminBundle[]>([]);

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    this.bundleService.getAll().subscribe({
      next: (res) => {
        this.bundles.set(res.items);
        this.state.set('loaded');
      },
      error: () => this.state.set('error'),
    });
  }

  protected statusBadgeClass(status: string): string {
    switch (status) {
      case 'Published':
        return 'admin-badge-success';
      case 'Archived':
        return 'admin-badge';
      default:
        return 'admin-badge-warning';
    }
  }
}
