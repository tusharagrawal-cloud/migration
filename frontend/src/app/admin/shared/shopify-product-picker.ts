import { Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ShopifyProductService } from '../../core/services/shopify-product.service';
import { ShopifyProductSummary } from '../../core/models/shopify-product.model';

/**
 * The one place in Admin where a human finds a product — by name, never by
 * typing a Shopify ID. Backed today by the isolated dev-fixture
 * ShopifyProductService (Milestone 8); swapping in a real Shopify product
 * search later means replacing that one service's implementation only —
 * this component calls the same three methods (getByCategory/getByHandle/
 * getById) either way, so no Admin screen needs to change. See
 * migration/docs/ADMIN_SHOPIFY_PICKER_READINESS.md.
 */
@Component({
  selector: 'app-shopify-product-picker',
  imports: [FormsModule],
  template: `
    <div class="picker">
      <input
        type="text"
        [placeholder]="placeholder()"
        [ngModel]="query()"
        (ngModelChange)="query.set($event)"
        class="picker-search"
        aria-label="Search products by name"
      />
      @if (loading()) {
        <p class="picker-hint">Loading products…</p>
      } @else if (visibleProducts().length === 0) {
        <p class="picker-hint">No matching products.</p>
      } @else {
        <ul class="picker-list">
          @for (product of visibleProducts(); track product.id) {
            <li>
              <button type="button" class="picker-item" (click)="pick(product)">
                <span class="picker-item-image" aria-hidden="true"></span>
                <span class="picker-item-text">
                  <span class="picker-item-title">{{ product.title }}</span>
                  <span class="picker-item-meta">{{ product.vendor }} · {{ categoryLabel(product.category) }}</span>
                </span>
              </button>
            </li>
          }
        </ul>
      }
    </div>
  `,
  styles: [
    `
      .picker {
        border: 1px solid var(--admin-border);
        border-radius: var(--admin-radius);
        background: var(--admin-surface);
        overflow: hidden;
      }
      .picker-search {
        width: 100%;
        border: none;
        border-bottom: 1px solid var(--admin-border);
        padding: var(--admin-space-3);
        font-size: 0.9375rem;
        background: var(--admin-surface);
        color: var(--admin-text);
      }
      .picker-search:focus-visible {
        outline: 2px solid var(--admin-accent);
        outline-offset: -2px;
      }
      .picker-hint {
        margin: 0;
        padding: var(--admin-space-4);
        color: var(--admin-text-muted);
        font-size: 0.875rem;
      }
      .picker-list {
        list-style: none;
        margin: 0;
        padding: 0;
        max-height: 260px;
        overflow-y: auto;
      }
      .picker-item {
        all: unset;
        display: flex;
        align-items: center;
        gap: var(--admin-space-3);
        width: 100%;
        box-sizing: border-box;
        padding: var(--admin-space-3);
        cursor: pointer;
        border-bottom: 1px solid var(--admin-border);
      }
      .picker-item:hover,
      .picker-item:focus-visible {
        background: var(--admin-surface-hover);
      }
      .picker-item-image {
        width: 36px;
        height: 36px;
        border-radius: var(--admin-radius-sm);
        background: var(--admin-surface-alt);
        flex-shrink: 0;
      }
      .picker-item-text {
        display: flex;
        flex-direction: column;
        min-width: 0;
      }
      .picker-item-title {
        font-weight: 500;
        font-size: 0.875rem;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }
      .picker-item-meta {
        font-size: 0.75rem;
        color: var(--admin-text-muted);
      }
    `,
  ],
})
export class ShopifyProductPicker implements OnInit {
  private readonly shopifyProducts = inject(ShopifyProductService);

  /** Restrict to one category, or omit to search all. */
  category = input<ShopifyProductSummary['category'] | undefined>(undefined);
  /** Product IDs to hide from results — e.g. products already added to a bundle. */
  excludeIds = input<string[]>([]);
  placeholder = input('Search products by name…');

  productPicked = output<ShopifyProductSummary>();

  protected query = signal('');
  protected loading = signal(true);
  private allProducts = signal<ShopifyProductSummary[]>([]);

  protected visibleProducts = computed(() => {
    const excluded = new Set(this.excludeIds());
    const q = this.query().trim().toLowerCase();
    return this.allProducts()
      .filter((p) => !excluded.has(p.id))
      .filter((p) => !q || p.title.toLowerCase().includes(q) || (p.vendor ?? '').toLowerCase().includes(q));
  });

  ngOnInit(): void {
    const cat = this.category();
    const categories: ShopifyProductSummary['category'][] = cat ? [cat] : ['airgun', 'pellet', 'accessory'];
    this.loading.set(true);
    let remaining = categories.length;
    const collected: ShopifyProductSummary[] = [];
    categories.forEach((c) => {
      this.shopifyProducts.getByCategory(c).subscribe((products) => {
        collected.push(...products);
        remaining -= 1;
        if (remaining === 0) {
          this.allProducts.set(collected);
          this.loading.set(false);
        }
      });
    });
  }

  protected pick(product: ShopifyProductSummary): void {
    this.productPicked.emit(product);
    this.query.set('');
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
