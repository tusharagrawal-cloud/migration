import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { AdminProductDetailPage } from './admin-product-detail-page';
import { SHOPIFY_DEV_FIXTURE_PRODUCTS } from '../../../core/services/shopify-dev-fixtures';
import { API_BASE_URL } from '../../../core/config/api-config';

describe('AdminProductDetailPage', () => {
  let httpMock: HttpTestingController;
  const airgunFixture = SHOPIFY_DEV_FIXTURE_PRODUCTS.find((p) => p.category === 'airgun')!;

  async function setUp() {
    await TestBed.configureTestingModule({
      imports: [AdminProductDetailPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: '' },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ handle: airgunFixture.handle }) } } },
      ],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  }

  afterEach(() => httpMock.verify());

  it('shows a ready-to-fill form (not an error) when no ONE77 details exist yet, and never shows JSON/technical vocabulary', async () => {
    await setUp();
    const fixture = TestBed.createComponent(AdminProductDetailPage);
    fixture.detectChanges();

    httpMock.expectOne((r) => r.url === '/api/admin/enrichment/by-product').flush(
      { error: 'Not found.' },
      { status: 404, statusText: 'Not Found' },
    );
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain(airgunFixture.title);
    expect(text).toContain('Category');
    expect(text).toContain('Specifications');
    expect(text).not.toContain('ShopifyProductId');
    expect(text).not.toContain('gid://');
    expect(text).not.toContain('SortOrder');
    expect(fixture.nativeElement.querySelector('form')).toBeTruthy();
  });

  it('adding and removing a specification row updates the list', async () => {
    await setUp();
    const fixture = TestBed.createComponent(AdminProductDetailPage);
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/admin/enrichment/by-product').flush({ error: 'Not found.' }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    const addButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find((b: any) =>
      b.textContent.includes('Add specification'),
    ) as HTMLButtonElement;
    addButton.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.spec-row').length).toBe(1);

    const removeButton: HTMLButtonElement = fixture.nativeElement.querySelector('.spec-row button.admin-btn-danger');
    removeButton.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.spec-row').length).toBe(0);
  });

  it('saves a new enrichment record via POST when none existed', async () => {
    await setUp();
    const fixture = TestBed.createComponent(AdminProductDetailPage);
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/admin/enrichment/by-product').flush({ error: 'Not found.' }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    const saveButton: HTMLButtonElement = fixture.nativeElement.querySelector('button[type="submit"]');
    saveButton.click();

    // save() re-checks whether a record now exists before deciding create vs update.
    httpMock.expectOne((r) => r.url === '/api/admin/enrichment/by-product').flush({ error: 'Not found.' }, { status: 404, statusText: 'Not Found' });

    const createReq = httpMock.expectOne((r) => r.url === '/api/admin/enrichment' && r.method === 'POST');
    expect(createReq.request.body.shopify_product_id).toBe(airgunFixture.id);
    createReq.flush({
      id: 1,
      shopify_product_id: airgunFixture.id,
      category: 'airgun',
      is_active: true,
      calibre: null,
      powerplant_type: null,
      weight_grains: null,
      recommended_pellet_weight_min: null,
      recommended_pellet_weight_max: null,
      compatible_powerplants: [],
      use_cases: [],
      specifications: [],
      created_at: '',
      updated_at: '',
    });
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Saved.');
  });
});
