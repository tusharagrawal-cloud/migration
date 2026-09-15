import { Component, input } from '@angular/core';
import { Logo } from './logo';

/**
 * The one product-image treatment reused by ProductCard and the product
 * detail page: a real photo once product.imageUrl is populated by a live
 * Shopify adapter, or an intentional brand-mark placeholder in the
 * meantime — never a bare empty rectangle.
 */
@Component({
  selector: 'app-product-image-slot',
  imports: [Logo],
  template: `
    <div class="image-slot">
      @if (imageUrl()) {
        <img [src]="imageUrl()" [alt]="alt()" />
      } @else {
        <app-logo variant="symbol" tone="white" class="placeholder-mark" />
      }
    </div>
  `,
  styles: [
    `
      .image-slot {
        aspect-ratio: var(--slot-aspect, 4 / 3);
        background: radial-gradient(circle at 30% 20%, var(--color-surface-raised) 0%, var(--color-bg-alt) 100%);
        border: 1px solid var(--color-border-soft);
        border-radius: var(--radius-md);
        display: flex;
        align-items: center;
        justify-content: center;
        overflow: hidden;
      }
      .image-slot img {
        width: 100%;
        height: 100%;
        object-fit: cover;
      }
      .placeholder-mark {
        height: 30%;
        opacity: 0.14;
      }
    `,
  ],
})
export class ProductImageSlot {
  imageUrl = input<string | null>(null);
  alt = input('');
}
