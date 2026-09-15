import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { ProductDetailPage } from './product-detail-page';
import { SHOPIFY_DEV_FIXTURE_PRODUCTS } from '../../core/services/shopify-dev-fixtures';
import { API_BASE_URL } from '../../core/config/api-config';

describe('ProductDetailPage', () => {
  let httpMock: HttpTestingController;
  const airgunFixture = SHOPIFY_DEV_FIXTURE_PRODUCTS.find((p) => p.category === 'airgun')!;

  function setUp(handle: string) {
    return TestBed.configureTestingModule({
      imports: [ProductDetailPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: '' },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ handle }) } } },
      ],
    }).compileComponents();
  }

  afterEach(() => httpMock?.verify());

  it('renders the product shell with a disabled purchase placeholder and no dev-fixture/technical leakage', () => {
    return setUp(airgunFixture.handle).then(() => {
      httpMock = TestBed.inject(HttpTestingController);
      const fixture = TestBed.createComponent(ProductDetailPage);
      fixture.detectChanges();

      httpMock.expectOne((r) => r.url === '/api/products/enrichment').flush({
        shopify_product_id: airgunFixture.id,
        category: 'airgun',
        calibre: '.177',
        powerplant_type: 'springer',
        weight_grains: null,
        recommended_pellet_weight_min: null,
        recommended_pellet_weight_max: null,
        compatible_powerplants: [],
        use_cases: [],
        specifications: [],
      });
      httpMock.expectOne((r) => r.url === '/api/products/matches').flush({
        source: { shopify_product_id: airgunFixture.id, category: 'airgun', is_active: true, calibre: '.177', powerplant_type: 'springer', weight_grains: null, recommended_pellet_weight_min: null, recommended_pellet_weight_max: null, use_cases: [], compatible_powerplants: [] },
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
      });
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain(airgunFixture.title.replace('[DEV FIXTURE] ', ''));
      expect(compiled.textContent).not.toContain('[DEV FIXTURE]');
      expect(compiled.textContent).not.toMatch(/price via shopify/i);
      const buyButton: HTMLButtonElement = compiled.querySelector('.buy-button')!;
      expect(buyButton.disabled).toBe(true);
      expect(compiled.querySelector('app-match-section')).toBeTruthy();
    });
  });

  it('hides the Specifications heading entirely when enrichment has no populated fields', () => {
    const accessoryFixture = SHOPIFY_DEV_FIXTURE_PRODUCTS.find((p) => p.category === 'accessory')!;
    return setUp(accessoryFixture.handle).then(() => {
      httpMock = TestBed.inject(HttpTestingController);
      const fixture = TestBed.createComponent(ProductDetailPage);
      fixture.detectChanges();

      httpMock.expectOne((r) => r.url === '/api/products/enrichment').flush({
        shopify_product_id: accessoryFixture.id,
        category: 'accessory',
        calibre: null,
        powerplant_type: null,
        weight_grains: null,
        recommended_pellet_weight_min: null,
        recommended_pellet_weight_max: null,
        compatible_powerplants: [],
        use_cases: [],
        specifications: [],
      });
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).not.toContain('Specifications');
      expect(compiled.querySelector('.spec-block')).toBeFalsy();
    });
  });

  it('shows a not-found state for an unknown handle', () => {
    return setUp('does-not-exist').then(() => {
      httpMock = TestBed.inject(HttpTestingController);
      const fixture = TestBed.createComponent(ProductDetailPage);
      fixture.detectChanges();

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain("couldn't find that product");
    });
  });
});
