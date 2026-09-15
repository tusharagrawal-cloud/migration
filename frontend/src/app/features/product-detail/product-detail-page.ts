import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ShopifyProductService } from '../../core/services/shopify-product.service';
import { EnrichmentService } from '../../core/services/enrichment.service';
import { CartService } from '../../core/services/cart.service';
import { ShopifyProductDetail, ShopifyProductVariant } from '../../core/models/shopify-product.model';
import { PublicEnrichment } from '../../core/models/enrichment.models';
import { displayProductTitle } from '../../core/services/product-display';
import { PageContainer } from '../../shared/ui/page-container';
import { LoadingState } from '../../shared/ui/loading-state';
import { ErrorState } from '../../shared/ui/error-state';
import { ProductImageSlot } from '../../shared/ui/product-image-slot';
import { MatchSection } from '../match/match-section';

type LoadState = 'loading' | 'loaded' | 'not-found' | 'error';

/**
 * Structured for the commerce data that arrives with the real Shopify
 * integration: a dedicated commercial-information area sits alongside —
 * not mixed into — ONE77's own technical specifications/enrichment and the
 * Match compatibility section, so the commercial area (now live: variant
 * selection + Add to Cart via CartService) stays isolated from that data.
 */
@Component({
  selector: 'app-product-detail-page',
  imports: [PageContainer, LoadingState, ErrorState, ProductImageSlot, MatchSection, FormsModule],
  template: `
    <app-page-container>
      @switch (state()) {
        @case ('loading') {
          <app-loading-state label="Loading product…" />
        }
        @case ('error') {
          <app-error-state message="We couldn't load this product right now." (retry)="load()" />
        }
        @case ('not-found') {
          <app-error-state message="We couldn't find that product." [retryable]="false" />
        }
        @case ('loaded') {
          <div class="layout">
            <app-product-image-slot [imageUrl]="product()!.imageUrl" [alt]="displayTitle()" class="hero-image" />

            <div class="details">
              @if (product()!.vendor) {
                <p class="eyebrow">{{ product()!.vendor }}</p>
              }
              <h1>{{ displayTitle() }}</h1>

              <div class="commerce-panel">
                @if (product()!.priceLabel) {
                  <div class="price-row">
                    <span class="price">{{ product()!.priceLabel }}</span>
                    @if (product()!.compareAtPriceLabel) {
                      <span class="compare-price">{{ product()!.compareAtPriceLabel }}</span>
                    }
                  </div>
                }

                @if (shortDescription()) {
                  <p class="short-description">{{ shortDescription() }}</p>
                }

                @if (hasVariantChoice()) {
                  <label class="variant-label" for="variant-select">
                    {{ product()!.variants[0].selectedOptions[0]?.name || 'Option' }}
                  </label>
                  <select id="variant-select" class="variant-select" [(ngModel)]="selectedVariantId">
                    @for (variant of product()!.variants; track variant.id) {
                      <option [value]="variant.id" [disabled]="!variant.availableForSale">
                        {{ variant.title }}{{ !variant.availableForSale ? ' — Sold out' : '' }}
                      </option>
                    }
                  </select>
                }

                <div class="action-row">
                  @if (canAddToCart()) {
                    <div class="qty-row">
                      <button type="button" class="qty-btn" (click)="decrementQty()" [disabled]="quantity() <= 1">−</button>
                      <span class="qty">{{ quantity() }}</span>
                      <button type="button" class="qty-btn" (click)="incrementQty()">+</button>
                    </div>
                    <button type="button" class="buy-button" [disabled]="addingToCart()" (click)="addToCart()">
                      {{ addingToCart() ? 'Adding…' : 'Add to Cart' }} <span class="arrow" aria-hidden="true">→</span>
                    </button>
                  } @else {
                    <button type="button" class="buy-button" disabled>
                      {{ product()!.variants.length === 0 ? 'Coming soon' : 'Sold out' }}
                    </button>
                  }

                  @if (isAirgun()) {
                    <a class="btn-outline" href="#match-section">
                      Find the best pellets &amp; gear <span class="arrow" aria-hidden="true">→</span>
                    </a>
                  }
                </div>

                @if (!canAddToCart() && product()!.variants.length === 0) {
                  <p class="commerce-note">Purchasing opens here once ONE77's Shopify store is connected.</p>
                }

                @if (cartError()) {
                  <p class="cart-error">{{ cartError() }}</p>
                }
              </div>

              @if (hasVisibleSpecs()) {
                @let spec = enrichment()!;
                <div class="spec-block">
                  <p class="spec-heading">Specifications</p>
                  <dl class="specs">
                    @if (spec.calibre) {
                      <div><dt>Calibre</dt><dd>{{ spec.calibre }}</dd></div>
                    }
                    @if (spec.powerplant_type) {
                      <div><dt>Powerplant</dt><dd>{{ spec.powerplant_type }}</dd></div>
                    }
                    @if (spec.weight_grains) {
                      <div><dt>Weight</dt><dd>{{ spec.weight_grains }} grains</dd></div>
                    }
                    @for (item of spec.specifications; track item.spec_key) {
                      <div><dt>{{ item.spec_key }}</dt><dd>{{ item.spec_value }}</dd></div>
                    }
                  </dl>
                </div>
              }
            </div>
          </div>

          @if (isAirgun()) {
            <div id="match-section">
              <app-match-section [shopifyProductId]="product()!.id" />
            </div>
          }
        }
      }
    </app-page-container>
  `,
  styles: [
    `
      .layout {
        display: grid;
        grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
        gap: var(--space-8);
        padding-block: var(--space-6);
      }
      @media (max-width: 720px) {
        .layout {
          grid-template-columns: 1fr;
        }
      }
      .hero-image {
        --slot-aspect: 1 / 1;
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
        line-height: var(--line-height-tight);
        margin-bottom: var(--space-5);
      }
      .commerce-panel {
        background: var(--color-surface);
        border: 1px dashed var(--color-border);
        border-radius: var(--radius-md);
        padding: var(--space-5);
        margin-bottom: var(--space-6);
      }
      .commerce-label {
        margin: 0;
        font-size: 0.75rem;
        letter-spacing: 0.1em;
        text-transform: uppercase;
        color: var(--color-text-faint);
      }
      .price {
        margin: 0;
        font-family: var(--font-family-heading);
        font-size: var(--font-size-xl);
        font-weight: var(--font-weight-black, 900);
        color: var(--color-text);
      }
      .price-row {
        display: flex;
        align-items: baseline;
        gap: var(--space-3);
        flex-wrap: wrap;
        margin-bottom: var(--space-4);
      }
      .compare-price {
        color: var(--color-text-faint);
        text-decoration: line-through;
        font-size: var(--font-size-md);
      }
      .short-description {
        margin: 0 0 var(--space-5);
        color: var(--color-text-muted);
        line-height: 1.6;
      }
      .variant-label {
        display: block;
        font-size: 0.75rem;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: var(--color-text-faint);
        margin-bottom: var(--space-2);
      }
      .variant-select {
        width: 100%;
        padding: var(--space-3);
        margin-bottom: var(--space-4);
        background: var(--color-bg);
        color: var(--color-text);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-sm);
        font-size: var(--font-size-sm);
      }
      .action-row {
        display: flex;
        flex-wrap: wrap;
        gap: var(--space-3);
        align-items: center;
      }
      .qty-row {
        display: inline-flex;
        align-items: center;
        gap: var(--space-3);
      }
      .qty-btn {
        width: 32px;
        height: 32px;
        border: 1px solid var(--color-border);
        background: none;
        color: var(--color-text);
        border-radius: var(--radius-sm);
        cursor: pointer;
        font-size: 1rem;
      }
      .qty-btn:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
      .qty {
        min-width: 1.5rem;
        text-align: center;
      }
      .arrow {
        display: inline-block;
        transition: transform var(--transition-base);
      }
      .btn-outline {
        display: inline-flex;
        align-items: center;
        gap: var(--space-2);
        background: transparent;
        color: var(--color-text);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-sm);
        padding: var(--space-3) var(--space-6);
        font-size: var(--font-size-sm);
        font-weight: var(--font-weight-medium);
        text-decoration: none;
        cursor: pointer;
      }
      .btn-outline:hover {
        border-color: var(--color-text-faint);
      }
      .cart-error {
        margin: var(--space-3) 0 0;
        color: var(--color-danger, #e5484d);
        font-size: var(--font-size-sm);
      }
      .commerce-note {
        margin: var(--space-2) 0 var(--space-4);
        color: var(--color-text-muted);
        font-size: var(--font-size-sm);
      }
      .buy-button {
        display: inline-flex;
        align-items: center;
        gap: var(--space-2);
        background: var(--color-accent);
        color: var(--color-accent-contrast);
        border: 1px solid transparent;
        border-radius: var(--radius-sm);
        padding: var(--space-3) var(--space-6);
        font-size: var(--font-size-sm);
        font-weight: var(--font-weight-medium);
        cursor: pointer;
      }
      .buy-button:hover:not(:disabled) .arrow {
        transform: translateX(2px);
      }
      .buy-button:hover:not(:disabled) {
        background: var(--color-accent-strong);
      }
      .buy-button:disabled {
        background: transparent;
        color: var(--color-text-faint);
        border-color: var(--color-border);
        cursor: not-allowed;
      }
      .spec-block {
        border-top: 1px solid var(--color-border-soft);
        padding-top: var(--space-5);
      }
      .spec-heading {
        margin: 0 0 var(--space-3);
        font-family: var(--font-family-heading);
        font-weight: var(--font-weight-semibold);
        font-size: var(--font-size-sm);
        text-transform: uppercase;
        letter-spacing: 0.06em;
        color: var(--color-text-faint);
      }
      .specs {
        display: grid;
        gap: var(--space-2);
        margin: 0;
      }
      .specs div {
        display: flex;
        justify-content: space-between;
        border-bottom: 1px solid var(--color-border-soft);
        padding-block: var(--space-2);
      }
      .specs dt {
        color: var(--color-text-muted);
      }
      .specs dd {
        margin: 0;
      }
    `,
  ],
})
export class ProductDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly shopifyProducts = inject(ShopifyProductService);
  private readonly enrichmentService = inject(EnrichmentService);
  private readonly cartService = inject(CartService);

  protected state = signal<LoadState>('loading');
  protected product = signal<ShopifyProductDetail | null>(null);
  protected enrichment = signal<PublicEnrichment | null>(null);
  protected isAirgun = signal(false);

  protected selectedVariantId = signal<string>('');
  protected quantity = signal(1);
  protected addingToCart = signal(false);
  protected cartError = signal<string | null>(null);

  protected hasVisibleSpecs = computed(() => {
    const spec = this.enrichment();
    if (!spec) return false;
    return !!(spec.calibre || spec.powerplant_type || spec.weight_grains || spec.specifications.length);
  });

  /** A variant picker is only worth showing when there's more than one real choice (e.g. calibre). */
  protected hasVariantChoice = computed(() => (this.product()?.variants.length ?? 0) > 1);

  protected selectedVariant = computed<ShopifyProductVariant | null>(() => {
    const variants = this.product()?.variants ?? [];
    return variants.find((v) => v.id === this.selectedVariantId()) ?? variants[0] ?? null;
  });

  protected canAddToCart = computed(() => !!this.selectedVariant()?.availableForSale);

  /** Plain-text preview of descriptionHtml (Shopify only gives HTML) — matches the old app's short_description role. */
  protected shortDescription = computed(() => {
    const html = this.product()?.descriptionHtml;
    if (!html) return '';
    const text = html
      .replace(/<[^>]*>/g, ' ')
      .replace(/\s+/g, ' ')
      .trim();
    return text.length > 180 ? text.slice(0, 177) + '…' : text;
  });

  private handle = '';

  ngOnInit(): void {
    this.handle = this.route.snapshot.paramMap.get('handle') ?? '';
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    this.shopifyProducts.getByHandle(this.handle).subscribe({
      next: (product) => {
        if (!product) {
          this.state.set('not-found');
          return;
        }
        this.product.set(product);
        this.isAirgun.set(product.category === 'airgun');
        this.selectedVariantId.set(product.variants[0]?.id ?? '');
        this.quantity.set(1);
        this.state.set('loaded');

        this.enrichmentService.getEnrichmentForProduct(product.id).subscribe({
          next: (enrichment) => this.enrichment.set(enrichment),
          error: () => this.enrichment.set(null),
        });
      },
      error: () => this.state.set('error'),
    });
  }

  protected displayTitle(): string {
    const product = this.product();
    return product ? displayProductTitle(product.title) : '';
  }

  protected incrementQty(): void {
    this.quantity.update((q) => q + 1);
  }

  protected decrementQty(): void {
    this.quantity.update((q) => Math.max(1, q - 1));
  }

  protected async addToCart(): Promise<void> {
    const variant = this.selectedVariant();
    if (!variant) return;
    this.addingToCart.set(true);
    this.cartError.set(null);
    await this.cartService.addToCart(variant.id, this.quantity());
    this.addingToCart.set(false);
    this.cartError.set(this.cartService.error());
  }
}