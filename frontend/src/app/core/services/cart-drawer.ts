import { Component, inject } from '@angular/core';
import { CartService } from '../../core/services/cart.service';

/**
 * Global cart drawer, mounted once in app.html alongside Header/Footer (not
 * per-page) so its open/closed state and contents survive route changes.
 * Checkout is a redirect to Shopify's own hosted checkout (cart.checkoutUrl)
 * — no payment form is ever built here, per SHOPIFY_V1_CONTRACT.md's
 * ownership boundary ("Shopify owns... cart, checkout, orders").
 */
@Component({
  selector: 'app-cart-drawer',
  template: `
    @if (cart.isOpen()) {
      <div class="backdrop" (click)="cart.close()"></div>
      <aside class="drawer" role="dialog" aria-label="Cart">
        <header class="drawer-header">
          <h2>Your cart</h2>
          <button type="button" class="close-btn" (click)="cart.close()" aria-label="Close cart">✕</button>
        </header>

        @if (cart.error()) {
          <p class="error">{{ cart.error() }}</p>
        }

        @if (!cart.cart() || cart.cart()!.lines.length === 0) {
          <p class="empty">Your cart is empty.</p>
        } @else {
          <ul class="lines">
            @for (line of cart.cart()!.lines; track line.id) {
              <li class="line">
                <div class="line-image">
                  @if (line.imageUrl) {
                    <img [src]="line.imageUrl" [alt]="line.productTitle" />
                  }
                </div>
                <div class="line-body">
                  <p class="line-title">{{ line.productTitle }}</p>
                  @if (line.variantTitle && line.variantTitle !== 'Default Title') {
                    <p class="line-variant">{{ line.variantTitle }}</p>
                  }
                  <div class="line-controls">
                    <button
                      type="button"
                      class="qty-btn"
                      [disabled]="cart.isLoading()"
                      (click)="cart.updateQuantity(line.id, line.quantity - 1)"
                    >
                      −
                    </button>
                    <span class="qty">{{ line.quantity }}</span>
                    <button
                      type="button"
                      class="qty-btn"
                      [disabled]="cart.isLoading()"
                      (click)="cart.updateQuantity(line.id, line.quantity + 1)"
                    >
                      +
                    </button>
                    <button
                      type="button"
                      class="remove-btn"
                      [disabled]="cart.isLoading()"
                      (click)="cart.removeLine(line.id)"
                    >
                      Remove
                    </button>
                  </div>
                </div>
                @if (line.lineTotalLabel) {
                  <p class="line-total">{{ line.lineTotalLabel }}</p>
                }
              </li>
            }
          </ul>

          <footer class="drawer-footer">
            @if (cart.cart()!.subtotalLabel) {
              <div class="subtotal-row">
                <span>Subtotal</span>
                <span>{{ cart.cart()!.subtotalLabel }}</span>
              </div>
            }
            <button type="button" class="checkout-btn" [disabled]="cart.isLoading()" (click)="cart.goToCheckout()">
              Checkout
            </button>
            <p class="checkout-note">You'll complete payment securely on Shopify.</p>
          </footer>
        }
      </aside>
    }
  `,
  styles: [
    `
      .backdrop {
        position: fixed;
        inset: 0;
        background: rgba(0, 0, 0, 0.5);
        z-index: 200;
      }
      .drawer {
        position: fixed;
        top: 0;
        right: 0;
        bottom: 0;
        width: min(400px, 100vw);
        background: var(--color-bg);
        border-left: 1px solid var(--color-border);
        z-index: 201;
        display: flex;
        flex-direction: column;
        overflow-y: auto;
      }
      .drawer-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: var(--space-5);
        border-bottom: 1px solid var(--color-border-soft);
      }
      .drawer-header h2 {
        margin: 0;
        font-family: var(--font-family-heading);
        font-size: var(--font-size-lg);
      }
      .close-btn {
        background: none;
        border: none;
        color: var(--color-text-muted);
        font-size: 1.1rem;
        cursor: pointer;
        padding: var(--space-2);
      }
      .error {
        margin: var(--space-4) var(--space-5) 0;
        color: var(--color-danger, #e5484d);
        font-size: var(--font-size-sm);
      }
      .empty {
        padding: var(--space-8) var(--space-5);
        text-align: center;
        color: var(--color-text-muted);
      }
      .lines {
        list-style: none;
        margin: 0;
        padding: var(--space-4) var(--space-5);
        flex: 1;
      }
      .line {
        display: flex;
        gap: var(--space-3);
        padding-block: var(--space-4);
        border-bottom: 1px solid var(--color-border-soft);
      }
      .line-image {
        width: 64px;
        height: 64px;
        flex-shrink: 0;
        background: var(--color-surface);
        border-radius: var(--radius-sm);
        overflow: hidden;
      }
      .line-image img {
        width: 100%;
        height: 100%;
        object-fit: cover;
      }
      .line-body {
        flex: 1;
        min-width: 0;
      }
      .line-title {
        margin: 0;
        font-size: var(--font-size-sm);
        font-weight: var(--font-weight-medium);
      }
      .line-variant {
        margin: var(--space-1) 0 0;
        font-size: 0.8125rem;
        color: var(--color-text-faint);
      }
      .line-controls {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        margin-top: var(--space-3);
      }
      .qty-btn {
        width: 24px;
        height: 24px;
        border: 1px solid var(--color-border);
        background: none;
        color: var(--color-text);
        border-radius: var(--radius-sm);
        cursor: pointer;
      }
      .qty-btn:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
      .qty {
        min-width: 1.5rem;
        text-align: center;
        font-size: var(--font-size-sm);
      }
      .remove-btn {
        margin-left: var(--space-3);
        background: none;
        border: none;
        color: var(--color-text-faint);
        font-size: 0.8125rem;
        text-decoration: underline;
        cursor: pointer;
        padding: 0;
      }
      .remove-btn:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
      .line-total {
        margin: 0;
        font-size: var(--font-size-sm);
        white-space: nowrap;
      }
      .drawer-footer {
        padding: var(--space-5);
        border-top: 1px solid var(--color-border-soft);
      }
      .subtotal-row {
        display: flex;
        justify-content: space-between;
        font-weight: var(--font-weight-medium);
        margin-bottom: var(--space-4);
      }
      .checkout-btn {
        width: 100%;
        background: var(--color-accent);
        color: var(--color-accent-contrast);
        border: none;
        border-radius: var(--radius-sm);
        padding: var(--space-3);
        font-family: var(--font-family-heading);
        font-weight: var(--font-weight-semibold);
        text-transform: uppercase;
        letter-spacing: 0.06em;
        cursor: pointer;
      }
      .checkout-btn:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
      .checkout-btn:not(:disabled):hover {
        background: var(--color-accent-strong);
      }
      .checkout-note {
        margin: var(--space-3) 0 0;
        font-size: 0.75rem;
        color: var(--color-text-faint);
        text-align: center;
      }
    `,
  ],
})
export class CartDrawer {
  protected cart = inject(CartService);
}