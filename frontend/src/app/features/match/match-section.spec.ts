import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { MatchSection } from './match-section';
import { PublicMatchResult } from '../../core/models/match.models';
import { SHOPIFY_DEV_FIXTURE_PRODUCTS } from '../../core/services/shopify-dev-fixtures';
import { API_BASE_URL } from '../../core/config/api-config';

describe('MatchSection', () => {
  let httpMock: HttpTestingController;
  const airgun = SHOPIFY_DEV_FIXTURE_PRODUCTS.find((p) => p.category === 'airgun')!;
  const pellet = SHOPIFY_DEV_FIXTURE_PRODUCTS.find((p) => p.category === 'pellet')!;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MatchSection],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), { provide: API_BASE_URL, useValue: '' }],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('resolves the real product name and priority label, with a readable reason, and never leaks internal Match vocabulary', () => {
    const fixture = TestBed.createComponent(MatchSection);
    fixture.componentRef.setInput('shopifyProductId', airgun.id);
    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url === '/api/products/matches');
    const result: PublicMatchResult = {
      source: {
        shopify_product_id: airgun.id,
        category: 'airgun',
        is_active: true,
        calibre: '.177',
        powerplant_type: 'springer',
        weight_grains: null,
        recommended_pellet_weight_min: null,
        recommended_pellet_weight_max: null,
        use_cases: [],
        compatible_powerplants: [],
      },
      pellets: [],
      accessories: [],
      best_match_pellets: [
        {
          product: {
            shopify_product_id: pellet.id,
            category: 'pellet',
            is_active: true,
            calibre: '.177',
            powerplant_type: null,
            weight_grains: 8,
            recommended_pellet_weight_min: null,
            recommended_pellet_weight_max: null,
            use_cases: [],
            compatible_powerplants: [],
          },
          status: 'compatible',
          priority: 1,
          priority_label: 'Best Match',
          use_cases: [],
          reason: 'A proven combination for this airgun.',
          is_curated: true,
        },
      ],
      recommended_pellets: [],
      compatible_pellets: [],
      best_match_accessories: [],
      recommended_accessories: [],
      compatible_accessories: [],
      match_reason: 'curated',
      reason_source: 'curated',
    };
    req.flush(result);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Best Match');
    expect(text).toContain(pellet.title.replace('[DEV FIXTURE] ', ''));
    expect(text).toContain('A proven combination for this airgun.');
    expect(text).not.toContain('compatible');
    expect(text).not.toContain('is_curated');
    expect(text).not.toContain('reason_source');
    expect(text).not.toContain(pellet.id); // never a raw Shopify GID
  });

  it('shows a "Compatible" badge (never a blank badge) for compatible-status cards, which carry no priority_label from the API', () => {
    const accessory = SHOPIFY_DEV_FIXTURE_PRODUCTS.find((p) => p.category === 'accessory')!;
    const fixture = TestBed.createComponent(MatchSection);
    fixture.componentRef.setInput('shopifyProductId', airgun.id);
    fixture.detectChanges();

    httpMock.expectOne((r) => r.url === '/api/products/matches').flush({
      source: {
        shopify_product_id: airgun.id,
        category: 'airgun',
        is_active: true,
        calibre: '.177',
        powerplant_type: 'springer',
        weight_grains: null,
        recommended_pellet_weight_min: null,
        recommended_pellet_weight_max: null,
        use_cases: [],
        compatible_powerplants: [],
      },
      pellets: [],
      accessories: [],
      best_match_pellets: [],
      recommended_pellets: [],
      compatible_pellets: [],
      best_match_accessories: [],
      recommended_accessories: [],
      compatible_accessories: [
        {
          product: {
            shopify_product_id: accessory.id,
            category: 'accessory',
            is_active: true,
            calibre: null,
            powerplant_type: null,
            weight_grains: null,
            recommended_pellet_weight_min: null,
            recommended_pellet_weight_max: null,
            use_cases: [],
            compatible_powerplants: [],
          },
          status: 'compatible',
          priority: null,
          priority_label: '',
          use_cases: [],
          reason: 'Fits this setup.',
          is_curated: true,
        },
      ],
      match_reason: '',
      reason_source: 'curated',
    } satisfies PublicMatchResult);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const badge = compiled.querySelector('.badge');
    expect(badge?.textContent?.trim()).toBe('Compatible');
  });

  it('shows a plain empty state when there are no compatible products', () => {
    const fixture = TestBed.createComponent(MatchSection);
    fixture.componentRef.setInput('shopifyProductId', airgun.id);
    fixture.detectChanges();

    httpMock.expectOne((r) => r.url === '/api/products/matches').flush({
      source: {
        shopify_product_id: airgun.id,
        category: 'airgun',
        is_active: true,
        calibre: null,
        powerplant_type: null,
        weight_grains: null,
        recommended_pellet_weight_min: null,
        recommended_pellet_weight_max: null,
        use_cases: [],
        compatible_powerplants: [],
      },
      pellets: [],
      accessories: [],
      best_match_pellets: [],
      recommended_pellets: [],
      compatible_pellets: [],
      best_match_accessories: [],
      recommended_accessories: [],
      compatible_accessories: [],
      match_reason: '',
      reason_source: 'derived',
    } satisfies PublicMatchResult);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No suggestions yet');
  });
});
