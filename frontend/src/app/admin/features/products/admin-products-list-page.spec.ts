import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminProductsListPage } from './admin-products-list-page';
import { SHOPIFY_DEV_FIXTURE_PRODUCTS } from '../../../core/services/shopify-dev-fixtures';
import { API_BASE_URL } from '../../../core/config/api-config';

describe('AdminProductsListPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminProductsListPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), { provide: API_BASE_URL, useValue: '' }],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists every dev-fixture product by name, and marks which have ONE77 details, without leaking Shopify IDs', () => {
    const fixture = TestBed.createComponent(AdminProductsListPage);
    fixture.detectChanges();

    const withDetails = SHOPIFY_DEV_FIXTURE_PRODUCTS[0];
    httpMock.expectOne('/api/admin/enrichment').flush({
      items: [{ id: 1, shopify_product_id: withDetails.id, category: withDetails.category, is_active: true, calibre: null, powerplant_type: null, weight_grains: null, recommended_pellet_weight_min: null, recommended_pellet_weight_max: null, compatible_powerplants: [], use_cases: [], specifications: [], created_at: '', updated_at: '' }],
      count: 1,
    });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    for (const product of SHOPIFY_DEV_FIXTURE_PRODUCTS) {
      expect(text).toContain(product.title);
      expect(text).not.toContain(product.id); // never render the raw Shopify GID
    }
    expect(text).toContain('ONE77 Details set');
    expect(text).toContain('Needs ONE77 Details');
  });
});
