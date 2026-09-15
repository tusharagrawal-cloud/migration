import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { AdminMatchService } from '../../core/services/admin-match.service';
import { MatchAirgunSummaryItem } from '../../core/models/admin-match.models';
import { ShopifyProductService } from '../../../core/services/shopify-product.service';
import { AdminCard } from '../../shared/admin-card';
import { AdminLoading, AdminEmpty, AdminError } from '../../shared/admin-states';

type LoadState = 'loading' | 'loaded' | 'error';

const COMPLETENESS_LABEL: Record<string, string> = {
  complete: 'Complete',
  needs_review: 'Needs Review',
  no_matches: 'No Matches',
};

const COMPLETENESS_BADGE_CLASS: Record<string, string> = {
  complete: 'admin-badge-success',
  needs_review: 'admin-badge-warning',
  no_matches: 'admin-badge',
};

@Component({
  selector: 'app-admin-match-list-page',
  imports: [RouterLink, AdminCard, AdminLoading, AdminEmpty, AdminError],
  template: `
    <h1>Matches</h1>
    <p class="lede">Choose an airgun to manage its matched pellets and accessories.</p>

    <p class="legend">
      <span class="admin-badge admin-badge-success">Complete</span> pellet and accessory recommendations are set ·
      <span class="admin-badge admin-badge-warning">Needs Review</span> still missing something ·
      <span class="admin-badge">No Matches</span> nothing linked yet
    </p>

    <input
      type="text"
      class="admin-field"
      style="max-width: 320px; margin-bottom: var(--admin-space-5);"
      placeholder="Search airguns by name…"
      [value]="query()"
      (input)="query.set($any($event.target).value)"
    />

    @switch (state()) {
      @case ('loading') {
        <app-admin-loading label="Loading airguns…" />
      }
      @case ('error') {
        <app-admin-error message="Unable to load airguns. Please try again." (retry)="load()" />
      }
      @case ('loaded') {
        @if (filteredAirguns().length === 0) {
          <app-admin-empty heading="No airguns match" />
        } @else {
          <div class="grid">
            @for (item of filteredAirguns(); track item.shopify_product_id) {
              <a [routerLink]="['/admin/match', item.shopify_product_id]" class="tile">
                <app-admin-card>
                  <p class="title">{{ productName(item.shopify_product_id) }}</p>
                  <p class="meta">{{ item.calibre || '—' }} · {{ item.powerplant_type || '—' }}</p>
                  <p class="counts">
                    Pellets: <strong>{{ item.counts.rec_pellets }}</strong> rec · {{ item.counts.compat_pellets }} compat &nbsp;|&nbsp;
                    Accessories: <strong>{{ item.counts.rec_acc }}</strong> rec · {{ item.counts.compat_acc }} compat
                  </p>
                  <span class="admin-badge" [class]="badgeClass(item.counts.state)">{{ stateLabel(item.counts.state) }}</span>
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
      h1 {
        font-size: 1.5rem;
        margin-bottom: var(--admin-space-2);
      }
      .lede {
        color: var(--admin-text-muted);
        margin-bottom: var(--admin-space-3);
      }
      .legend {
        font-size: 0.8125rem;
        color: var(--admin-text-muted);
        margin-bottom: var(--admin-space-5);
        display: flex;
        gap: var(--admin-space-2);
        align-items: center;
        flex-wrap: wrap;
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
      .meta {
        margin: var(--admin-space-1) 0;
        color: var(--admin-text-muted);
        font-size: 0.8125rem;
      }
      .counts {
        margin: var(--admin-space-2) 0;
        font-size: 0.8125rem;
      }
    `,
  ],
})
export class AdminMatchListPage implements OnInit {
  private readonly matchService = inject(AdminMatchService);
  private readonly shopifyProducts = inject(ShopifyProductService);

  protected state = signal<LoadState>('loading');
  protected airguns = signal<MatchAirgunSummaryItem[]>([]);
  protected names = signal<Map<string, string>>(new Map());
  protected query = signal('');

  protected filteredAirguns = computed(() => {
    const q = this.query().trim().toLowerCase();
    if (!q) {
      return this.airguns();
    }
    return this.airguns().filter((a) => this.productName(a.shopify_product_id).toLowerCase().includes(q));
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    this.matchService
      .getAirguns()
      .pipe(
        switchMap((res) => {
          if (res.items.length === 0) {
            return of({ items: res.items, names: new Map<string, string>() });
          }
          return forkJoin(
            res.items.map((item) =>
              this.shopifyProducts.getById(item.shopify_product_id).pipe(
                map((product) => [item.shopify_product_id, product?.title ?? 'Unnamed product'] as const),
                catchError(() => of([item.shopify_product_id, 'Unnamed product'] as const)),
              ),
            ),
          ).pipe(map((pairs) => ({ items: res.items, names: new Map(pairs) })));
        }),
      )
      .subscribe({
        next: ({ items, names }) => {
          this.airguns.set(items);
          this.names.set(names);
          this.state.set('loaded');
        },
        error: () => this.state.set('error'),
      });
  }

  protected stateLabel(state: string): string {
    return COMPLETENESS_LABEL[state] ?? state;
  }

  protected badgeClass(state: string): string {
    return COMPLETENESS_BADGE_CLASS[state] ?? 'admin-badge';
  }

  protected productName(shopifyProductId: string): string {
    return this.names().get(shopifyProductId) ?? 'Unnamed product';
  }
}
