import { Component, OnInit, inject, signal } from '@angular/core';
import { forkJoin, map, of, switchMap } from 'rxjs';
import { BundleService } from '../../core/services/bundle.service';
import { ShopifyProductService } from '../../core/services/shopify-product.service';
import { PublicBundle } from '../../core/models/bundle.models';
import { displayProductTitle } from '../../core/services/product-display';
import { PageContainer } from '../../shared/ui/page-container';
import { LoadingState } from '../../shared/ui/loading-state';
import { EmptyState } from '../../shared/ui/empty-state';
import { ErrorState } from '../../shared/ui/error-state';
import { ProductImageSlot } from '../../shared/ui/product-image-slot';

type LoadState = 'loading' | 'loaded' | 'empty' | 'error';

interface DisplayItem {
  name: string;
  imageUrl: string | null;
  isPrimary: boolean;
}

interface DisplayBundle {
  bundle: PublicBundle;
  primary: DisplayItem | null;
  otherItems: DisplayItem[];
}

/**
 * Curated setups — never a commerce entity. No bundle price, discount, or
 * savings figure exists or is calculated here; each constituent product is
 * priced individually once Shopify is connected.
 */
@Component({
  selector: 'app-bundles-page',
  imports: [PageContainer, LoadingState, EmptyState, ErrorState, ProductImageSlot],
  template: `
    <app-page-container>
      <div class="intro">
        <p class="eyebrow">Ready-to-shoot kits</p>
        <h1>Curated bundles</h1>
        <p class="lede">
          Groupings our team put together — a matched airgun, pellets and accessories. Each product is priced
          individually once Shopify is connected; there's no separate bundle price.
        </p>
      </div>

      @switch (state()) {
        @case ('loading') {
          <app-loading-state label="Loading bundles…" />
        }
        @case ('error') {
          <app-error-state message="We couldn't load bundles right now." (retry)="load()" />
        }
        @case ('empty') {
          <app-empty-state heading="No bundles yet" body="Check back soon." />
        }
        @case ('loaded') {
          <div class="grid">
            @for (entry of bundles(); track entry.bundle.id) {
              <div class="bundle-card">
                @if (entry.primary; as primary) {
                  <app-product-image-slot [imageUrl]="primary.imageUrl" [alt]="primary.name" class="bundle-image" />
                }
                <div class="body">
                  <p class="name">{{ entry.bundle.name }}</p>
                  @if (entry.bundle.tagline) {
                    <p class="tagline">{{ entry.bundle.tagline }}</p>
                  }
                  @if (entry.primary; as primary) {
                    <p class="primary-item">{{ primary.name }}</p>
                  }
                  @if (entry.otherItems.length > 0) {
                    <p class="includes-label">Includes</p>
                    <ul class="items">
                      @for (item of entry.otherItems; track item.name) {
                        <li>{{ item.name }}</li>
                      }
                    </ul>
                  }
                </div>
              </div>
            }
          </div>
        }
      }
    </app-page-container>
  `,
  styles: [
    `
      .intro {
        max-width: 60ch;
        margin-bottom: var(--space-7);
      }
      .eyebrow {
        font-size: 0.75rem;
        letter-spacing: 0.18em;
        text-transform: uppercase;
        font-weight: var(--font-weight-bold);
        color: var(--color-accent);
        margin: 0 0 var(--space-2);
      }
      h1 {
        font-family: var(--font-family-heading);
        font-size: var(--font-size-2xl);
        margin: 0 0 var(--space-3);
      }
      .lede {
        color: var(--color-text-muted);
        margin: 0;
      }
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
        gap: var(--space-5);
      }
      .bundle-card {
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-md);
        overflow: hidden;
      }
      .bundle-image {
        --slot-aspect: 16 / 10;
        border-radius: 0;
        border-width: 0 0 1px;
      }
      .body {
        padding: var(--space-5);
      }
      .name {
        margin: 0;
        font-family: var(--font-family-heading);
        font-size: var(--font-size-md);
        font-weight: var(--font-weight-semibold);
      }
      .tagline {
        margin: var(--space-2) 0 0;
        color: var(--color-text-muted);
        font-size: var(--font-size-sm);
      }
      .primary-item {
        margin: var(--space-4) 0 0;
        padding-top: var(--space-4);
        border-top: 1px solid var(--color-border-soft);
        font-weight: var(--font-weight-medium);
      }
      .includes-label {
        margin: var(--space-3) 0 var(--space-1);
        font-size: 0.6875rem;
        letter-spacing: 0.1em;
        text-transform: uppercase;
        color: var(--color-text-faint);
      }
      .items {
        margin: 0;
        padding-left: var(--space-5);
        color: var(--color-text-muted);
        font-size: var(--font-size-sm);
      }
    `,
  ],
})
export class BundlesPage implements OnInit {
  private readonly bundleService = inject(BundleService);
  private readonly shopifyProducts = inject(ShopifyProductService);

  protected state = signal<LoadState>('loading');
  protected bundles = signal<DisplayBundle[]>([]);

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    this.bundleService
      .getBundles()
      .pipe(
        switchMap((bundles) => {
          if (!bundles.length) {
            return of([] as DisplayBundle[]);
          }
          const withItems = bundles.map((bundle) => {
            const sortedItems = bundle.items.slice().sort((a, b) => a.sort_order - b.sort_order);
            const items$ = sortedItems.length
              ? forkJoin(
                  sortedItems.map((item) =>
                    this.shopifyProducts.getById(item.shopify_product_id).pipe(
                      map(
                        (p): DisplayItem => ({
                          name: p ? displayProductTitle(p.title) : 'Product',
                          imageUrl: p?.imageUrl ?? null,
                          isPrimary: item.item_role === 'Primary',
                        }),
                      ),
                    ),
                  ),
                )
              : of([] as DisplayItem[]);
            return items$.pipe(
              map((items): DisplayBundle => {
                const primary = items.find((i) => i.isPrimary) ?? null;
                const otherItems = items.filter((i) => !i.isPrimary);
                return { bundle, primary, otherItems };
              }),
            );
          });
          return forkJoin(withItems);
        }),
      )
      .subscribe({
        next: (displayBundles) => {
          this.bundles.set(displayBundles);
          this.state.set(displayBundles.length ? 'loaded' : 'empty');
        },
        error: () => this.state.set('error'),
      });
  }
}
