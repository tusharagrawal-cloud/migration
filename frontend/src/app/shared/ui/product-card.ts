import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ShopifyProductSummary } from '../../core/models/shopify-product.model';
import { displayProductTitle } from '../../core/services/product-display';
import { ProductImageSlot } from './product-image-slot';

const CATEGORY_LABEL: Record<ShopifyProductSummary['category'], string> = {
  airgun: 'Airgun',
  pellet: 'Pellet',
  accessory: 'Accessory',
};

const CATEGORY_ROUTE: Record<ShopifyProductSummary['category'], string> = {
  airgun: '/airguns',
  pellet: '/pellets',
  accessory: '/accessories',
};

/**
 * The one product-card visual used across Home, category listings, and
 * anywhere else a Shopify-backed product needs a compact preview. No price
 * is shown here at all — commercial information belongs on the product
 * detail page once/where Shopify data exists, never fabricated on a card.
 * Real photography isn't available yet (see shopify-product.model.ts), so
 * the image slot carries an intentional brand-mark placeholder rather than
 * a blank rectangle; it becomes a real `<img>` once product.imageUrl is
 * populated by a live Shopify adapter, with no template restructuring.
 */
@Component({
  selector: 'app-product-card',
  imports: [RouterLink, ProductImageSlot],
  template: `
    <a [routerLink]="[linkPrefix(), product().handle]" class="product-card">
      <app-product-image-slot [imageUrl]="product().imageUrl" [alt]="displayTitle()" />
      <div class="body">
        <div class="eyebrow-row">
          @if (product().vendor) {
            <span class="vendor">{{ product().vendor }}</span>
          }
          <span class="category">{{ categoryLabel() }}</span>
        </div>
        <p class="title">{{ displayTitle() }}</p>
        <span class="view">View details →</span>
      </div>
    </a>
  `,
  styles: [
    `
      .product-card {
        display: block;
        text-decoration: none;
        color: inherit;
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-md);
        overflow: hidden;
        transition: border-color var(--transition-base), transform var(--transition-base);
      }
      .product-card:hover {
        border-color: var(--color-accent);
        transform: translateY(-2px);
      }
      app-product-image-slot {
        --slot-aspect: 4 / 3;
        display: block;
      }
      .body {
        padding: var(--space-4);
      }
      .eyebrow-row {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        gap: var(--space-2);
        font-size: 0.6875rem;
        letter-spacing: 0.14em;
        text-transform: uppercase;
      }
      .vendor {
        color: var(--color-accent);
        font-weight: var(--font-weight-bold);
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        min-width: 0;
      }
      .category {
        flex-shrink: 0;
        color: var(--color-text-faint);
      }
      .title {
        margin: var(--space-2) 0 0;
        font-family: var(--font-family-heading);
        font-size: var(--font-size-md);
        line-height: var(--line-height-snug);
      }
      .view {
        display: inline-block;
        margin-top: var(--space-3);
        font-size: 0.75rem;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: var(--color-text-faint);
      }
      .product-card:hover .view {
        color: var(--color-accent);
      }
    `,
  ],
})
export class ProductCard {
  product = input.required<ShopifyProductSummary>();

  protected displayTitle(): string {
    return displayProductTitle(this.product().title);
  }

  protected categoryLabel(): string {
    return CATEGORY_LABEL[this.product().category];
  }

  protected linkPrefix(): string {
    return CATEGORY_ROUTE[this.product().category];
  }
}
