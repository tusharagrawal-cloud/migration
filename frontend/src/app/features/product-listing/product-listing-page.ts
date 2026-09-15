import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ShopifyProductService } from '../../core/services/shopify-product.service';
import { ShopifyProductSummary } from '../../core/models/shopify-product.model';
import { PageContainer } from '../../shared/ui/page-container';
import { SectionHeading } from '../../shared/ui/section-heading';
import { LoadingState } from '../../shared/ui/loading-state';
import { EmptyState } from '../../shared/ui/empty-state';
import { ErrorState } from '../../shared/ui/error-state';
import { ProductCard } from '../../shared/ui/product-card';

type LoadState = 'loading' | 'loaded' | 'empty' | 'error';

/**
 * Reusable category listing shell — the same component renders Airguns,
 * Pellets, and Accessories, parametrized by route data. Backed today by
 * ShopifyProductService's dev fixtures; a real Shopify adapter swaps in
 * with no change to this component.
 */
@Component({
  selector: 'app-product-listing-page',
  imports: [PageContainer, SectionHeading, LoadingState, EmptyState, ErrorState, ProductCard],
  template: `
    <app-page-container>
      <app-section-heading [title]="title()" [subtitle]="subtitle()" />

      @switch (state()) {
        @case ('loading') {
          <app-loading-state label="Loading products…" />
        }
        @case ('error') {
          <app-error-state message="We couldn't load products right now." (retry)="load()" />
        }
        @case ('empty') {
          <app-empty-state heading="No products here yet" body="Check back soon." />
        }
        @case ('loaded') {
          <div class="grid">
            @for (product of products(); track product.id) {
              <app-product-card [product]="product" />
            }
          </div>
        }
      }
    </app-page-container>
  `,
  styles: [
    `
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
        gap: var(--space-5);
      }
    `,
  ],
})
export class ProductListingPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly shopifyProducts = inject(ShopifyProductService);

  protected state = signal<LoadState>('loading');
  protected products = signal<ShopifyProductSummary[]>([]);
  protected title = signal('Products');
  protected subtitle = signal<string | null>(null);

  private category: ShopifyProductSummary['category'] = 'airgun';

  ngOnInit(): void {
    const data = this.route.snapshot.data;
    this.category = data['category'];
    this.title.set(data['title']);
    this.subtitle.set(data['subtitle'] ?? null);
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    this.shopifyProducts.getByCategory(this.category).subscribe({
      next: (products) => {
        this.products.set(products);
        this.state.set(products.length ? 'loaded' : 'empty');
      },
      error: () => this.state.set('error'),
    });
  }
}
