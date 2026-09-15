import { Component, OnChanges, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { MatchService } from '../../core/services/match.service';
import { ShopifyProductService } from '../../core/services/shopify-product.service';
import { MatchCard, PublicMatchResult } from '../../core/models/match.models';
import { displayProductTitle } from '../../core/services/product-display';
import { LoadingState } from '../../shared/ui/loading-state';
import { EmptyState } from '../../shared/ui/empty-state';

type LoadState = 'idle' | 'loading' | 'loaded' | 'empty' | 'error';

interface DisplayCard {
  card: MatchCard;
  name: string;
  handle: string | null;
}

const CATEGORY_ROUTE: Record<string, string> = {
  pellet: '/pellets',
  accessory: '/accessories',
};

const BADGE_CLASS: Record<string, string> = {
  'Best Match': 'badge-best',
  Recommended: 'badge-recommended',
  Compatible: 'badge-compatible',
};

/**
 * Customer-facing compatibility section — ONE77's key differentiator, not
 * a technical appendix. Shows only priority_label ("Best Match" /
 * "Recommended" / "Compatible"), the resolved product name, and a plain
 * reason where the team gave one — never the internal status vocabulary,
 * priority number, reason_source, is_curated flag, relationship id, or a
 * raw Shopify GID from the Match API.
 */
@Component({
  selector: 'app-match-section',
  imports: [RouterLink, LoadingState, EmptyState],
  template: `
    <section class="match-section" aria-labelledby="match-heading">
      <p class="eyebrow">ONE77 Match</p>
      <h3 id="match-heading">What goes well with this airgun</h3>

      @switch (state()) {
        @case ('loading') {
          <app-loading-state label="Finding compatible products…" />
        }
        @case ('error') {
          <p class="quiet-note">We couldn't load compatibility suggestions right now.</p>
        }
        @case ('empty') {
          <app-empty-state
            heading="No suggestions yet"
            body="We haven't set up compatibility recommendations for this product yet."
          />
        }
        @case ('loaded') {
          @if (pellets().length > 0) {
            <div class="group">
              <h4>Pellets</h4>
              <div class="cards">
                @for (entry of pellets(); track entry.card.product.shopify_product_id) {
                  <a [routerLink]="[categoryRoute(entry.card.product.category), entry.handle]" class="match-card">
                    <span class="badge" [class]="badgeClass(displayLabel(entry.card))">{{ displayLabel(entry.card) }}</span>
                    <p class="name">{{ entry.name }}</p>
                    @if (entry.card.product.calibre) {
                      <p class="meta">{{ entry.card.product.calibre }}</p>
                    }
                    @if (entry.card.reason) {
                      <p class="reason">{{ entry.card.reason }}</p>
                    }
                  </a>
                }
              </div>
            </div>
          }
          @if (accessories().length > 0) {
            <div class="group">
              <h4>Accessories</h4>
              <div class="cards">
                @for (entry of accessories(); track entry.card.product.shopify_product_id) {
                  <a [routerLink]="[categoryRoute(entry.card.product.category), entry.handle]" class="match-card">
                    <span class="badge" [class]="badgeClass(displayLabel(entry.card))">{{ displayLabel(entry.card) }}</span>
                    <p class="name">{{ entry.name }}</p>
                    @if (entry.card.reason) {
                      <p class="reason">{{ entry.card.reason }}</p>
                    }
                  </a>
                }
              </div>
            </div>
          }
        }
      }
    </section>
  `,
  styles: [
    `
      .match-section {
        margin-top: var(--space-9);
        padding-top: var(--space-7);
        border-top: 1px solid var(--color-border-soft);
      }
      .eyebrow {
        font-size: 0.75rem;
        letter-spacing: 0.18em;
        text-transform: uppercase;
        font-weight: var(--font-weight-bold);
        color: var(--color-accent);
        margin: 0 0 var(--space-2);
      }
      h3 {
        font-family: var(--font-family-heading);
        font-size: var(--font-size-xl);
        margin: 0 0 var(--space-6);
      }
      .group {
        margin-bottom: var(--space-6);
      }
      .group h4 {
        font-family: var(--font-family-heading);
        font-size: var(--font-size-sm);
        text-transform: uppercase;
        letter-spacing: 0.06em;
        color: var(--color-text-faint);
        margin: 0 0 var(--space-3);
      }
      .cards {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
        gap: var(--space-4);
      }
      .match-card {
        display: block;
        text-decoration: none;
        color: inherit;
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-md);
        padding: var(--space-4);
        transition: border-color var(--transition-base);
      }
      .match-card:hover {
        border-color: var(--color-accent);
      }
      .badge {
        display: inline-block;
        font-size: 0.6875rem;
        font-weight: var(--font-weight-bold);
        letter-spacing: 0.06em;
        text-transform: uppercase;
        padding: 2px var(--space-2);
        border-radius: var(--radius-sm);
        margin-bottom: var(--space-2);
      }
      .badge-best {
        background: var(--color-accent-soft);
        color: var(--color-accent);
      }
      .badge-recommended {
        background: rgba(255, 255, 255, 0.08);
        color: var(--color-text);
      }
      .badge-compatible {
        background: transparent;
        color: var(--color-text-faint);
        border: 1px solid var(--color-border);
      }
      .name {
        margin: 0;
        font-family: var(--font-family-heading);
        font-weight: var(--font-weight-semibold);
      }
      .meta {
        margin: var(--space-1) 0 0;
        color: var(--color-text-faint);
        font-size: var(--font-size-sm);
      }
      .reason {
        margin: var(--space-2) 0 0;
        color: var(--color-text-muted);
        font-size: 0.8125rem;
        line-height: var(--line-height-snug);
      }
      .quiet-note {
        color: var(--color-text-muted);
      }
    `,
  ],
})
export class MatchSection implements OnChanges {
  private readonly matchService = inject(MatchService);
  private readonly shopifyProducts = inject(ShopifyProductService);

  shopifyProductId = input.required<string>();

  protected state = signal<LoadState>('idle');
  protected pellets = signal<DisplayCard[]>([]);
  protected accessories = signal<DisplayCard[]>([]);

  ngOnChanges(): void {
    this.load();
  }

  private load(): void {
    this.state.set('loading');
    this.matchService.getMatchesForProduct(this.shopifyProductId()).subscribe({
      next: (result) => this.resolveAndSet(result),
      error: () => this.state.set('error'),
    });
  }

  private resolveAndSet(result: PublicMatchResult): void {
    const pelletCards = [...result.best_match_pellets, ...result.recommended_pellets, ...result.compatible_pellets];
    const accessoryCards = [...result.best_match_accessories, ...result.recommended_accessories, ...result.compatible_accessories];
    const allCards = [...pelletCards, ...accessoryCards];

    if (allCards.length === 0) {
      this.pellets.set([]);
      this.accessories.set([]);
      this.state.set('empty');
      return;
    }

    forkJoin(allCards.map((card) => this.resolveCard(card))).subscribe((entries) => {
      this.pellets.set(entries.slice(0, pelletCards.length));
      this.accessories.set(entries.slice(pelletCards.length));
      this.state.set('loaded');
    });
  }

  private resolveCard(card: MatchCard) {
    return this.shopifyProducts.getById(card.product.shopify_product_id).pipe(
      map((product): DisplayCard => ({
        card,
        name: product ? displayProductTitle(product.title) : 'Unnamed product',
        handle: product?.handle ?? null,
      })),
      catchError(() => of<DisplayCard>({ card, name: 'Unnamed product', handle: null })),
    );
  }

  protected categoryRoute(category: string): string {
    return CATEGORY_ROUTE[category] ?? '/airguns';
  }

  protected badgeClass(label: string): string {
    return BADGE_CLASS[label] ?? 'badge-compatible';
  }

  /**
   * The backend only populates priority_label for "recommended" status
   * (Best Match / Recommended); "compatible" status carries an empty
   * label. Fall back to the plain "Compatible" customer-facing term
   * derived from the card's own status, rather than showing a blank badge.
   */
  protected displayLabel(card: MatchCard): string {
    if (card.priority_label) return card.priority_label;
    return card.status === 'compatible' ? 'Compatible' : '';
  }
}
