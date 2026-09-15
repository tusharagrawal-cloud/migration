import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { ShopifyProductService } from '../../core/services/shopify-product.service';
import { BundleService } from '../../core/services/bundle.service';
import { LearnService } from '../../core/services/learn.service';
import { WebinarService } from '../../core/services/webinar.service';
import { HomepageService } from '../../core/services/homepage.service';
import { API_BASE_URL } from '../../core/config/api-config';
import { ShopifyProductSummary } from '../../core/models/shopify-product.model';
import { PublicBundle } from '../../core/models/bundle.models';
import { LearnCategory } from '../../core/models/learn.models';
import { WebinarEvent } from '../../core/models/webinar.models';
import { displayProductTitle } from '../../core/services/product-display';
import { PageContainer } from '../../shared/ui/page-container';
import { ProductCard } from '../../shared/ui/product-card';
import { Logo } from '../../shared/ui/logo';

interface IntentPath {
  title: string;
  body: string;
  link: string;
}

const INTENTS: IntentPath[] = [
  { title: 'My First Airgun', body: 'New to the sport? Start with our beginner guides.', link: '/learn' },
  { title: 'Target Practice', body: 'Precision setups built for tight groups.', link: '/airguns' },
  { title: 'Plinking', body: 'Backyard fun — forgiving, easy-going gear.', link: '/airguns' },
  { title: 'Upgrade My Setup', body: 'Ready-to-shoot kits our team curated.', link: '/bundles' },
];

interface DisplayBundle {
  bundle: PublicBundle;
  itemNames: string[];
}

/**
 * The ONE77 storefront homepage — a branded entry point built around
 * customer discovery (what do I want to do / what should I buy / what
 * works together) rather than internal site structure. Design language
 * informed by the legacy storefront's more developed customer experience
 * (frontend/src/pages/Home.jsx) as a reference — reimplemented here against
 * this app's own architecture and real data sources, not copied.
 */
@Component({
  selector: 'app-home-page',
  imports: [RouterLink, PageContainer, ProductCard, Logo],
  template: `
    <!-- A. Hero -->
    <section class="hero">
      @if (heroImageUrl(); as photo) {
        <img [src]="photo" alt="" class="hero-photo" />
        <div class="hero-scrim" aria-hidden="true"></div>
      } @else {
        <div class="hero-mark" aria-hidden="true">
          <app-logo variant="numeric" tone="white" />
        </div>
      }
      <app-page-container>
        <div class="hero-content">
          <p class="eyebrow">ONE77 Sports</p>
          <h1>Get the setup right.</h1>
          <p class="lede">
            Airguns, pellets and accessories — matched by our team so you know it'll work together before you buy.
          </p>
          <div class="hero-actions">
            <a routerLink="/airguns" class="btn-primary">Shop Airguns</a>
            <a routerLink="/learn" class="btn-outline">Learn the Basics</a>
          </div>
        </div>
      </app-page-container>
    </section>

    <!-- H. Trust strip -->
    <section class="trust-strip">
      <app-page-container>
        <div class="trust-grid">
          <div class="trust-item">
            <span class="dot" aria-hidden="true"></span>
            <div>
              <p class="trust-title">Expert-led selection</p>
              <p class="trust-body">Every listing reviewed by our team.</p>
            </div>
          </div>
          <div class="trust-item">
            <span class="dot" aria-hidden="true"></span>
            <div>
              <p class="trust-title">Matched recommendations</p>
              <p class="trust-body">Right calibre, weight and use case.</p>
            </div>
          </div>
          <div class="trust-item">
            <span class="dot" aria-hidden="true"></span>
            <div>
              <p class="trust-title">Curated bundles</p>
              <p class="trust-body">No noise — only kit worth owning.</p>
            </div>
          </div>
          <div class="trust-item">
            <span class="dot" aria-hidden="true"></span>
            <div>
              <p class="trust-title">Live human help</p>
              <p class="trust-body">Ask us on the free Q&amp;A.</p>
            </div>
          </div>
        </div>
      </app-page-container>
    </section>

    <!-- B. Shop by intent -->
    <section class="section">
      <app-page-container>
        <div class="section-head">
          <div>
            <p class="eyebrow">Start with your goal</p>
            <h2>What are you shooting for?</h2>
          </div>
          <p class="section-note">Not sure where to begin? Pick what you're after — we'll point you the right way.</p>
        </div>
        <div class="intent-grid">
          @for (intent of intents; track intent.title) {
            <a [routerLink]="intent.link" class="intent-tile">
              <p class="intent-title">{{ intent.title }}</p>
              <p class="intent-body">{{ intent.body }}</p>
            </a>
          }
        </div>
      </app-page-container>
    </section>

    <!-- C. Featured airguns -->
    @if (featuredAirguns().length > 0) {
      <section class="section alt">
        <app-page-container>
          <div class="section-head">
            <div>
              <p class="eyebrow">Handpicked</p>
              <h2>Featured airguns</h2>
            </div>
            <a routerLink="/airguns" class="see-all">View all airguns →</a>
          </div>
          <div class="product-grid">
            @for (product of featuredAirguns(); track product.id) {
              <app-product-card [product]="product" />
            }
          </div>
        </app-page-container>
      </section>
    }

<section class="field-band">
      <app-page-container>
        <div class="field-band-content">
          <app-logo variant="symbol" tone="white" class="field-icon" />
          <div>
<p class="eyebrow field-eyebrow"><span class="eyebrow-line" aria-hidden="true"></span>One77 in the field</p>            <p class="field-headline">Built for the range, tuned for the field.</p>
          </div>
        </div>
      </app-page-container>
    </section>



    <!-- D. Match / compatibility -->
    <section class="match-strip">
      <app-page-container>
        <div class="match-layout">
          <div class="match-copy">
            <p class="eyebrow">ONE77 Match</p>
            <h2>Your airgun.<br /><span class="muted">The pellets and gear that actually fit.</span></h2>
            <p class="lede">
              Every airgun on ONE77 is matched to compatible pellets and accessories by calibre, use case and
              powerplant — so you're never guessing what works together.
            </p>
            <a routerLink="/airguns" class="btn-primary">Browse Matched Airguns</a>
          </div>
          <div class="match-badges" aria-hidden="true">
            <div class="match-badge best">
              <span class="badge-label">Best Match</span>
              <span class="badge-body">Our top pick for this airgun</span>
            </div>
            <div class="match-badge recommended">
              <span class="badge-label">Recommended</span>
              <span class="badge-body">A strong, proven combination</span>
            </div>
            <div class="match-badge compatible">
              <span class="badge-label">Compatible</span>
              <span class="badge-body">Confirmed to work together</span>
            </div>
          </div>
        </div>
      </app-page-container>
    </section>

    <!-- E. Curated bundles -->
    @if (bundles().length > 0) {
      <section class="section">
        <app-page-container>
          <div class="section-head">
            <div>
              <p class="eyebrow">Ready-to-shoot kits</p>
              <h2>Curated bundles</h2>
            </div>
            <a routerLink="/bundles" class="see-all">See all bundles →</a>
          </div>
          <div class="bundle-grid">
            @for (entry of bundles(); track entry.bundle.id) {
              <div class="bundle-card">
                <p class="bundle-name">{{ entry.bundle.name }}</p>
                @if (entry.bundle.tagline) {
                  <p class="bundle-tagline">{{ entry.bundle.tagline }}</p>
                }
                <ul class="bundle-items">
                  @for (name of entry.itemNames; track name) {
                    <li>{{ name }}</li>
                  }
                </ul>
              </div>
            }
          </div>
        </app-page-container>
      </section>
    }

    <!-- F. Learn -->
    @if (learnCategories().length > 0) {
      <section class="section alt">
        <app-page-container>
          <div class="section-head">
            <div>
              <p class="eyebrow">Know before you buy</p>
              <h2>Learn</h2>
            </div>
            <a routerLink="/learn" class="see-all">Browse all guides →</a>
          </div>
          <div class="learn-grid">
            @for (category of learnCategories(); track category.id) {
              <a [routerLink]="['/learn', category.slug]" class="learn-tile">
                <p class="learn-title">{{ category.name }}</p>
                @if (category.description) {
                  <p class="learn-body">{{ category.description }}</p>
                }
              </a>
            }
          </div>
        </app-page-container>
      </section>
    }

    <!-- G. Webinar (lower prominence than Airguns) -->
    @if (nextWebinar(); as event) {
      <section class="webinar-strip">
        <app-page-container>
          <div class="webinar-layout">
            <div>
              <p class="eyebrow">Human support</p>
              <p class="webinar-headline">Still unsure? Ask us live — free 30-minute Q&amp;A.</p>
              <p class="webinar-next">Next session: {{ formatDate(event.event_date_time) }}</p>
            </div>
            <a routerLink="/webinar" class="btn-outline">Reserve My Seat</a>
          </div>
        </app-page-container>
      </section>
}


     <section class="field-band">
  <app-page-container>
    <div class="field-band-content">
      <app-logo variant="symbol" tone="white" class="field-icon" />
      <div>
<p class="eyebrow field-eyebrow"><span class="eyebrow-line" aria-hidden="true"></span>One77 in the field</p>        <p class="field-headline">Built for the range, tuned for the field.</p>
      </div>
    </div>
  </app-page-container>
</section>
  `,
  styles: [
    `

    .eyebrow-line {
  display: inline-block;
  width: 20px;
  height: 1px;
  background: var(--color-accent);
  margin-right: var(--space-2);
  vertical-align: middle;
}
      .eyebrow {
        font-size: 0.75rem;
        letter-spacing: 0.22em;
        text-transform: uppercase;
        font-weight: var(--font-weight-bold);
        color: var(--color-accent);
        margin: 0 0 var(--space-3);
      }
      h1,
      h2 {
        font-family: var(--font-family-heading);
      }
      .lede {
        color: var(--color-text-muted);
        font-size: var(--font-size-md);
        max-width: 52ch;
      }
      .btn-primary,
      .btn-outline {
        display: inline-block;
        text-decoration: none;
        padding: var(--space-3) var(--space-6);
        border-radius: var(--radius-sm);
        font-family: var(--font-family-heading);
        font-weight: var(--font-weight-semibold);
        font-size: var(--font-size-sm);
      }
      .btn-primary {
        background: var(--color-accent);
        color: var(--color-accent-contrast);
      }
      .btn-primary:hover {
        background: var(--color-accent-strong);
      }
      .btn-outline {
        color: var(--color-text);
        border: 1px solid var(--color-border);
      }
      .btn-outline:hover {
        border-color: var(--color-text);
      }

      /* Hero */
      .hero {
  position: relative;
  overflow: hidden;
  background: #000000;
  border-bottom: 1px solid var(--color-border-soft);
  padding-block: var(--space-9);
  color: #ffffff;
}
.hero .lede {
  color: #cccccc;
}
      .hero-mark {
        position: absolute;
        top: 50%;
        right: -6%;
        width: 46%;
        max-width: 560px;
        opacity: 0.07;
        transform: translateY(-50%) rotate(-6deg);
        pointer-events: none;
      }
      .hero-mark app-logo {
        height: auto;
        width: 100%;
      }
      /*
       * Locked layout: the photograph fills the hero edge-to-edge and the
       * scrim sits above it, both absolutely positioned and placed before
       * .hero-content in the template — the existing headline/copy/CTAs
       * need no z-index or restructuring to stay on top. Swapping which
       * photograph is supplied never changes hero height, text position,
       * or CTA position; the image adapts to the layout via object-fit,
       * never the other way around.
       */
      .hero-photo {
        position: absolute;
        inset: 0;
        width: 100%;
        height: 100%;
        object-fit: cover;
        object-position: center;
      }
      .hero-scrim {
        position: absolute;
        inset: 0;
        background: linear-gradient(115deg, var(--color-bg) 15%, rgba(10, 10, 10, 0.75) 45%, rgba(10, 10, 10, 0.35) 100%);
      }
      .hero-content {
        position: relative;
        max-width: 40rem;
      }
      .hero h1 {
        font-size: var(--font-size-4xl);
        line-height: var(--line-height-tight);
        margin: 0 0 var(--space-5);
      }
      .hero-actions {
  display: flex;
  gap: var(--space-4);
  flex-wrap: wrap;
  margin-top: var(--space-6);
}
.hero .btn-outline {
  color: #ffffff;
  border: 1px solid #ffffff;
}
.hero .btn-outline:hover {
  border-color: var(--color-accent);
  color: var(--color-accent);
}
      @media (max-width: 640px) {
        .hero {
          padding-block: var(--space-7);
        }
        .hero h1 {
          font-size: var(--font-size-3xl);
        }
        .hero-mark {
          width: 70%;
          opacity: 0.05;
        }
        /* A narrower/taller crop on mobile — bias slightly toward the top third so a landscape photo's main subject is less likely to be cropped out. */
        .hero-photo {
          object-position: center 30%;
        }
        .hero-scrim {
          background: linear-gradient(180deg, rgba(10, 10, 10, 0.3) 0%, var(--color-bg) 85%);
        }
      }

      /* Trust strip */
      .trust-strip {
        background: var(--color-bg-alt);
        border-bottom: 1px solid var(--color-border-soft);
        padding-block: var(--space-6);
      }
      .trust-grid {
        display: grid;
        grid-template-columns: repeat(2, 1fr);
        gap: var(--space-5);
      }
      @media (min-width: 900px) {
        .trust-grid {
          grid-template-columns: repeat(4, 1fr);
        }
      }
      .trust-item {
        display: flex;
        align-items: flex-start;
        gap: var(--space-3);
      }
      .dot {
        margin-top: 6px;
        width: 6px;
        height: 6px;
        background: var(--color-accent);
        flex-shrink: 0;
      }
      .trust-title {
        margin: 0;
        font-weight: var(--font-weight-semibold);
        font-size: var(--font-size-sm);
      }
      .trust-body {
        margin: var(--space-1) 0 0;
        color: var(--color-text-faint);
        font-size: 0.8125rem;
      }

      /* Section rhythm */
      .section {
        padding-block: var(--space-8);
      }
      .section.alt {
        background: var(--color-bg-alt);
        border-block: 1px solid var(--color-border-soft);
      }
      .section-head {
        display: flex;
        align-items: flex-end;
        justify-content: space-between;
        flex-wrap: wrap;
        gap: var(--space-3);
        margin-bottom: var(--space-6);
      }
      .section-head h2 {
        font-size: var(--font-size-2xl);
        margin: 0;
      }
      .section-note {
        color: var(--color-text-faint);
        font-size: var(--font-size-sm);
        max-width: 30ch;
        margin: 0;
      }
      .see-all {
        text-decoration: none;
        color: var(--color-text);
        font-size: 0.8125rem;
        font-weight: var(--font-weight-semibold);
        letter-spacing: 0.06em;
        text-transform: uppercase;
        white-space: nowrap;
      }
      .see-all:hover {
        color: var(--color-accent);
      }

      /* Intent grid */
      .intent-grid {
        display: grid;
        grid-template-columns: repeat(2, 1fr);
        gap: var(--space-4);
      }
      @media (min-width: 900px) {
        .intent-grid {
          grid-template-columns: repeat(4, 1fr);
        }
      }
      .intent-tile {
        display: block;
        text-decoration: none;
        color: inherit;
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        border-left: 3px solid var(--color-accent);
        border-radius: var(--radius-sm);
        padding: var(--space-5);
        min-height: 130px;
        transition: border-color var(--transition-base), background var(--transition-base);
      }
      .intent-tile:hover {
        background: var(--color-surface-raised);
      }
      .intent-title {
        margin: 0;
        font-family: var(--font-family-heading);
        font-weight: var(--font-weight-semibold);
        font-size: var(--font-size-md);
      }
      .intent-body {
        margin: var(--space-2) 0 0;
        color: var(--color-text-faint);
        font-size: 0.8125rem;
        line-height: var(--line-height-snug);
      }

      /* Product grid */
      .product-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
        gap: var(--space-5);
      }

      /* Match strip */
     .match-strip {
  background: #000000;
  border-block: 1px solid var(--color-border-soft);
  padding-block: var(--space-8);
  color: #ffffff;
}
.match-strip .lede {
  color: #cccccc;
}
      .match-layout {
        display: grid;
        gap: var(--space-7);
        align-items: center;
      }
      @media (min-width: 900px) {
        .match-layout {
          grid-template-columns: 1.2fr 1fr;
        }
      }
      .match-copy h2 {
        font-size: var(--font-size-2xl);
        line-height: var(--line-height-tight);
        margin: 0 0 var(--space-4);
      }
      .muted {
        color: var(--color-text-faint);
      }
      .match-copy .lede {
        margin-bottom: var(--space-5);
      }
      .match-badges {
        display: flex;
        flex-direction: column;
        gap: var(--space-3);
      }
      .match-badge {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
  padding: var(--space-4);
  border: 1px solid #2a2a2a;
  background: #141414;
  border-left: 3px solid #2a2a2a;
}
      .match-badge.best {
        border-left-color: var(--color-accent);
      }
      .match-badge.recommended {
        border-left-color: var(--color-text-muted);
      }
     .badge-label {
  font-family: var(--font-family-heading);
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-sm);
  color: #ffffff;
}
.badge-body {
  color: #999999;
  font-size: 0.8125rem;
}

      /* Bundles */
      .bundle-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
        gap: var(--space-5);
      }
      .bundle-card {
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-md);
        padding: var(--space-5);
      }
      .bundle-name {
        margin: 0;
        font-family: var(--font-family-heading);
        font-size: var(--font-size-md);
        font-weight: var(--font-weight-semibold);
      }
      .bundle-tagline {
        margin: var(--space-2) 0 0;
        color: var(--color-text-muted);
        font-size: var(--font-size-sm);
      }
      .bundle-items {
        margin: var(--space-4) 0 0;
        padding-left: var(--space-4);
        color: var(--color-text-faint);
        font-size: 0.8125rem;
      }

      /* Learn */
      .learn-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
        gap: var(--space-5);
      }
      .learn-tile {
        display: block;
        text-decoration: none;
        color: inherit;
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-md);
        padding: var(--space-5);
      }
      .learn-tile:hover {
        border-color: var(--color-accent);
      }
      .learn-title {
        margin: 0;
        font-family: var(--font-family-heading);
        font-weight: var(--font-weight-semibold);
      }
      .learn-body {
        margin: var(--space-2) 0 0;
        color: var(--color-text-faint);
        font-size: 0.8125rem;
      }

      /* Webinar strip — deliberately lighter-weight than the sections above */
      .webinar-strip {
        padding-block: var(--space-6);
        border-bottom: 1px solid var(--color-border-soft);
      }
      .webinar-layout {
        display: flex;
        align-items: center;
        justify-content: space-between;
        flex-wrap: wrap;
        gap: var(--space-4);
      }
      .webinar-headline {
        margin: 0;
        font-family: var(--font-family-heading);
        font-weight: var(--font-weight-semibold);
        font-size: var(--font-size-md);
      }
      .webinar-next {
        margin: var(--space-1) 0 0;
        color: var(--color-text-faint);
        font-size: 0.8125rem;
      }


      /* Field band */
      .field-band {
        background: #0a0a0a;
        border-bottom: none;
        border-top: none;
  padding-block: 0rem;
      }
      .field-band-content {
        display: flex;
        align-items: center;
        gap: 0.6rem;
      }
      .field-icon {
        height: 26px;
        width: auto;
        flex-shrink: 0;
        opacity: 0.8;
      }
      .field-band .eyebrow {
        margin: 0 0 var(--space-2);
      }
      .field-headline {
        margin: 0;
        color: #ffffff;
        font-family: var(--font-family-heading);
        font-weight: var(--font-weight-semibold);
        font-size: var(--font-size-md);
      }



    `,
  ],
})
export class HomePage implements OnInit {
  private readonly shopifyProducts = inject(ShopifyProductService);
  private readonly bundleService = inject(BundleService);
  private readonly learnService = inject(LearnService);
  private readonly webinarService = inject(WebinarService);
  private readonly homepageService = inject(HomepageService);
  private readonly apiBaseUrl = inject(API_BASE_URL);

  protected readonly intents = INTENTS;

  protected featuredAirguns = signal<ShopifyProductSummary[]>([]);
  protected bundles = signal<DisplayBundle[]>([]);
  protected learnCategories = signal<LearnCategory[]>([]);
  protected nextWebinar = signal<WebinarEvent | null>(null);
  protected heroImageUrl = signal<string | null>(null);

  ngOnInit(): void {
    this.homepageService
      .getConfig()
      .pipe(catchError(() => of(null)))
      .subscribe((config) => this.heroImageUrl.set(config?.hero_image_url ? `${this.apiBaseUrl}${config.hero_image_url}` : null));

    this.shopifyProducts
      .getByCategory('airgun')
      .pipe(
        map((products) => products.slice(0, 4)),
        catchError(() => of([] as ShopifyProductSummary[])),
      )
      .subscribe((products) => this.featuredAirguns.set(products));

    this.bundleService
      .getBundles()
      .pipe(
        map((all) => all.slice(0, 3)),
        switchMap((bundles) => {
          if (bundles.length === 0) {
            return of([] as DisplayBundle[]);
          }
          return forkJoin(
            bundles.map((bundle) =>
              forkJoin(
                bundle.items
                  .slice()
                  .sort((a, b) => a.sort_order - b.sort_order)
                  .map((item) =>
                    this.shopifyProducts.getById(item.shopify_product_id).pipe(
                      map((product) => (product ? displayProductTitle(product.title) : 'Unavailable item')),
                      catchError(() => of('Unavailable item')),
                    ),
                  ),
              ).pipe(map((itemNames): DisplayBundle => ({ bundle, itemNames }))),
            ),
          );
        }),
        catchError(() => of([] as DisplayBundle[])),
      )
      .subscribe((entries) => this.bundles.set(entries));

    this.learnService
      .getCategories()
      .pipe(
        map((categories) => categories.slice(0, 3)),
        catchError(() => of([] as LearnCategory[])),
      )
      .subscribe((categories) => this.learnCategories.set(categories));

    this.webinarService
      .getUpcomingEvents()
      .pipe(
        map((events) => events[0] ?? null),
        catchError(() => of(null)),
      )
      .subscribe((event) => this.nextWebinar.set(event));
  }

  protected formatDate(isoDate: string): string {
    return new Date(isoDate).toLocaleString(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short',
    });
  }
}
