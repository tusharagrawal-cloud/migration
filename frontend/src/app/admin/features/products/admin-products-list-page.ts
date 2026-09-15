import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ShopifyProductService } from '../../../core/services/shopify-product.service';
import { ShopifyProductSummary } from '../../../core/models/shopify-product.model';
import { AdminEnrichmentService } from '../../core/services/admin-enrichment.service';
import { AdminCard } from '../../shared/admin-card';
import { AdminLoading, AdminEmpty, AdminError } from '../../shared/admin-states';

interface ProductRow {
  product: ShopifyProductSummary;
  hasDetails: boolean;
}

type LoadState = 'loading' | 'loaded' | 'error';

/**
 * "Choose Product → Edit ONE77 Details → Save" starts here (Milestone 11,
 * Section 7 / ADMIN_V1_USABILITY_CONTRACT.md). Every product Shopify would
 * offer is listed by name — never a Shopify ID — with a plain indicator of
 * whether its ONE77 Details are set up yet.
 *
 * "Add product" does not create a new Shopify product — products are
 * sourced from Shopify and appear here automatically once synced. The
 * action instead opens a picker scoped to products that still need ONE77
 * details, so a merchandiser can jump straight to configuring the newest
 * unconfigured item without scrolling the whole catalog.
 */
@Component({
  selector: 'app-admin-products-list-page',
  imports: [RouterLink, AdminCard, AdminLoading, AdminEmpty, AdminError],
  template: `
    <div class="page-head">
      <div>
        <h1>Products</h1>
        <p class="lede">Choose a product to edit its ONE77 details and specifications.</p>
      </div>
      @if (state() === 'loaded' && rows().length > 0) {
        <button type="button" class="admin-btn admin-btn-primary" (click)="openPicker()">
          + Add product
        </button>
      }
    </div>

    @switch (state()) {
      @case ('loading') {
        <app-admin-loading label="Loading products…" />
      }
      @case ('error') {
        <app-admin-error message="Unable to load products. Please try again." (retry)="load()" />
      }
      @case ('loaded') {
        @if (rows().length === 0) {
          <app-admin-empty heading="No products yet" />
        } @else {
          <div class="grid">
            @for (row of rows(); track row.product.id) {
              <a [routerLink]="['/admin/products', row.product.handle]" class="tile">
                <app-admin-card>
                  @if (row.product.imageUrl) {
                    <img [src]="row.product.imageUrl" [alt]="row.product.title" class="thumb" />
                  } @else {
                    <div class="thumb thumb-placeholder"></div>
                  }
                  <p class="title">{{ row.product.title }}</p>
                  <p class="meta">{{ row.product.vendor }} · {{ categoryLabel(row.product.category) }}</p>
                  @if (row.hasDetails) {
                    <span class="admin-badge admin-badge-success">ONE77 Details set</span>
                  } @else {
                    <span class="admin-badge admin-badge-warning">Needs ONE77 Details</span>
                  }
                </app-admin-card>
              </a>
            }
          </div>
        }
      }
    }

    @if (pickerOpen()) {
      <div class="picker-overlay" (click)="closePicker()">
        <div class="picker-modal" (click)="$event.stopPropagation()">
          <div class="picker-head">
            <h2>Add product</h2>
            <button type="button" class="picker-close" (click)="closePicker()" aria-label="Close">&times;</button>
          </div>

          <p class="picker-hint">
            Products come from Shopify automatically. Pick one below to set up its ONE77 details.
          </p>

          <input
            type="text"
            class="picker-search"
            placeholder="Search unconfigured products…"
            [value]="pickerQuery()"
            (input)="pickerQuery.set($any($event.target).value)"
          />

          @if (unconfiguredRows().length === 0) {
            <p class="picker-empty">Every product already has ONE77 details set.</p>
          } @else if (filteredPickerRows().length === 0) {
            <p class="picker-empty">No matches for "{{ pickerQuery() }}".</p>
          } @else {
            <ul class="picker-list">
              @for (row of filteredPickerRows(); track row.product.id) {
                <li>
                  <a [routerLink]="['/admin/products', row.product.handle]" class="picker-row" (click)="closePicker()">
                    @if (row.product.imageUrl) {
                      <img [src]="row.product.imageUrl" [alt]="row.product.title" class="picker-thumb" />
                    } @else {
                      <div class="picker-thumb picker-thumb-placeholder"></div>
                    }
                    <span class="picker-row-text">
                      <span class="picker-row-title">{{ row.product.title }}</span>
                      <span class="picker-row-meta">{{ row.product.vendor }} · {{ categoryLabel(row.product.category) }}</span>
                    </span>
                    <span class="admin-badge admin-badge-warning">Needs details</span>
                  </a>
                </li>
              }
            </ul>
          }
        </div>
      </div>
    }
  `,
  styles: [
    `
      .page-head {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: var(--admin-space-4);
        margin-bottom: var(--admin-space-6);
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
        grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
        gap: var(--admin-space-4);
      }
      .tile {
        text-decoration: none;
        color: inherit;
      }
      .thumb {
        width: 100%;
        aspect-ratio: 1;
        object-fit: cover;
        border-radius: var(--admin-radius-sm);
        margin-bottom: var(--admin-space-2);
        display: block;
      }
      .thumb-placeholder {
        background: var(--admin-border);
      }
      .title {
        margin: 0;
        font-weight: 600;
      }
      .meta {
        margin: var(--admin-space-1) 0 var(--admin-space-3);
        color: var(--admin-text-muted);
        font-size: 0.8125rem;
      }

      .picker-overlay {
        position: fixed;
        inset: 0;
        background: rgba(0, 0, 0, 0.45);
        display: flex;
        align-items: center;
        justify-content: center;
        padding: var(--admin-space-4);
        z-index: 100;
      }
      .picker-modal {
        background: var(--admin-surface, #fff);
        border-radius: var(--admin-radius-sm);
        width: 100%;
        max-width: 480px;
        max-height: 80vh;
        display: flex;
        flex-direction: column;
        padding: var(--admin-space-5);
        gap: var(--admin-space-3);
      }
      .picker-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
      }
      .picker-head h2 {
        margin: 0;
        font-size: 1.125rem;
      }
      .picker-close {
        border: none;
        background: none;
        font-size: 1.25rem;
        line-height: 1;
        cursor: pointer;
        color: var(--admin-text-muted);
        padding: var(--admin-space-1);
      }
      .picker-hint {
        margin: 0;
        color: var(--admin-text-muted);
        font-size: 0.8125rem;
      }
      .picker-search {
        border: 1px solid var(--admin-border);
        border-radius: var(--admin-radius-sm);
        padding: var(--admin-space-2) var(--admin-space-3);
        font-size: 0.875rem;
      }
      .picker-empty {
        color: var(--admin-text-muted);
        font-size: 0.875rem;
        margin: var(--admin-space-3) 0;
      }
      .picker-list {
        list-style: none;
        margin: 0;
        padding: 0;
        overflow-y: auto;
        display: flex;
        flex-direction: column;
        gap: var(--admin-space-1);
      }
      .picker-row {
        display: flex;
        align-items: center;
        gap: var(--admin-space-3);
        padding: var(--admin-space-2);
        border-radius: var(--admin-radius-sm);
        text-decoration: none;
        color: inherit;
      }
      .picker-row:hover {
        background: var(--admin-border);
      }
      .picker-thumb {
        width: 40px;
        height: 40px;
        border-radius: var(--admin-radius-sm);
        object-fit: cover;
        flex-shrink: 0;
      }
      .picker-thumb-placeholder {
        background: var(--admin-border);
      }
      .picker-row-text {
        display: flex;
        flex-direction: column;
        min-width: 0;
        flex: 1;
      }
      .picker-row-title {
        font-weight: 600;
        font-size: 0.875rem;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }
      .picker-row-meta {
        font-size: 0.75rem;
        color: var(--admin-text-muted);
      }
    `,
  ],
})
export class AdminProductsListPage implements OnInit {
  private readonly shopifyProducts = inject(ShopifyProductService);
  private readonly enrichmentService = inject(AdminEnrichmentService);
  private readonly router = inject(Router);

  protected state = signal<LoadState>('loading');
  protected rows = signal<ProductRow[]>([]);

  protected pickerOpen = signal(false);
  protected pickerQuery = signal('');

  protected unconfiguredRows = computed(() => this.rows().filter((r) => !r.hasDetails));
  protected filteredPickerRows = computed(() => {
    const query = this.pickerQuery().trim().toLowerCase();
    const source = this.unconfiguredRows();
    if (!query) {
      return source;
    }
    return source.filter((row) => row.product.title.toLowerCase().includes(query));
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    forkJoin({
      airguns: this.shopifyProducts.getByCategory('airgun'),
      pellets: this.shopifyProducts.getByCategory('pellet'),
      accessories: this.shopifyProducts.getByCategory('accessory'),
      enrichment: this.enrichmentService.getAll(),
    }).subscribe({
      next: ({ airguns, pellets, accessories, enrichment }) => {
        const withDetails = new Set(enrichment.items.map((e) => e.shopify_product_id));
        const allProducts = [...airguns, ...pellets, ...accessories];
        this.rows.set(allProducts.map((product) => ({ product, hasDetails: withDetails.has(product.id) })));
        this.state.set('loaded');
      },
      error: () => this.state.set('error'),
    });
  }

  protected openPicker(): void {
    this.pickerQuery.set('');
    this.pickerOpen.set(true);
  }

  protected closePicker(): void {
    this.pickerOpen.set(false);
  }

  protected categoryLabel(category: string): string {
    switch (category) {
      case 'airgun':
        return 'Airgun';
      case 'pellet':
        return 'Pellet';
      case 'accessory':
        return 'Accessory';
      default:
        return category;
    }
  }
}