import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { AdminMatchEditorPage } from './admin-match-editor-page';
import { SHOPIFY_DEV_FIXTURE_PRODUCTS } from '../../../core/services/shopify-dev-fixtures';
import { API_BASE_URL } from '../../../core/config/api-config';

describe('AdminMatchEditorPage', () => {
  let httpMock: HttpTestingController;
  const airgun = SHOPIFY_DEV_FIXTURE_PRODUCTS.find((p) => p.category === 'airgun')!;
  const pellet = SHOPIFY_DEV_FIXTURE_PRODUCTS.find((p) => p.category === 'pellet')!;

  async function setUp() {
    await TestBed.configureTestingModule({
      imports: [AdminMatchEditorPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: '' },
        // ActivatedRoute.paramMap always holds the already-decoded segment value in a real app.
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ shopifyProductId: airgun.id }) } } },
      ],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  }

  function flushInitialLoad(fixture: ReturnType<typeof TestBed.createComponent>, relationships: unknown[] = []) {
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/admin/enrichment/by-product').flush({
      id: 1,
      shopify_product_id: airgun.id,
      category: 'airgun',
      is_active: true,
      calibre: '4.5mm',
      powerplant_type: 'springer',
      weight_grains: null,
      recommended_pellet_weight_min: null,
      recommended_pellet_weight_max: null,
      compatible_powerplants: [],
      use_cases: [],
      specifications: [],
      created_at: '',
      updated_at: '',
    });
    httpMock.expectOne((r) => r.url === '/api/admin/match/relationships').flush({ items: relationships, count: relationships.length });
    httpMock.expectOne((r) => r.url === '/api/admin/match/candidates').flush({
      items: [{ product: { shopify_product_id: pellet.id, category: 'pellet', is_active: true, calibre: '4.5mm', powerplant_type: null, weight_grains: 8, recommended_pellet_weight_min: null, recommended_pellet_weight_max: null, use_cases: [], compatible_powerplants: [] }, current_relationship: null }],
      count: 1,
    });
    fixture.detectChanges();
  }

  afterEach(() => httpMock.verify());

  it('renders customer/business labels only — never internal status/reason_source/relationship-id vocabulary', async () => {
    await setUp();
    const fixture = TestBed.createComponent(AdminMatchEditorPage);
    flushInitialLoad(fixture, [
      {
        id: 42,
        source_shopify_product_id: airgun.id,
        target_shopify_product_id: pellet.id,
        target_category: 'pellet',
        status: 'recommended',
        priority: 1,
        priority_label: 'Best Match',
        reason: 'Great everyday pellet.',
        admin_notes: '',
        calibre_override: false,
        is_active: true,
        created_at: '',
        updated_at: '',
        use_cases: [],
        target: { shopify_product_id: pellet.id, category: 'pellet', is_active: true, calibre: '4.5mm', powerplant_type: null, weight_grains: 8, recommended_pellet_weight_min: null, recommended_pellet_weight_max: null, use_cases: [], compatible_powerplants: [] },
      },
    ]);

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain(pellet.title);
    expect(text).toContain('Best Match');
    expect(text).not.toContain('reason_source');
    expect(text).not.toContain('is_curated');
    expect(text).not.toContain(pellet.id); // never the raw GID
    expect(text).not.toContain('42'); // never the internal relationship id
  });

  it('quick "Recommend" upserts with the default Recommended priority (2)', async () => {
    await setUp();
    const fixture = TestBed.createComponent(AdminMatchEditorPage);
    flushInitialLoad(fixture);

    const recommendButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('.candidate-actions button')).find(
      (b: any) => b.textContent.trim() === 'Recommend',
    ) as HTMLButtonElement;
    recommendButton.click();

    const req = httpMock.expectOne((r) => r.url === '/api/admin/match/relationships' && r.method === 'POST');
    expect(req.request.body).toEqual({
      source_shopify_product_id: airgun.id,
      target_shopify_product_id: pellet.id,
      status: 'recommended',
      priority: 2,
      use_cases: [],
      reason: null,
      admin_notes: null,
      calibre_override: false,
    });

    req.flush({
      id: 1,
      source_shopify_product_id: airgun.id,
      target_shopify_product_id: pellet.id,
      target_category: 'pellet',
      status: 'recommended',
      priority: 2,
      priority_label: 'Recommended',
      reason: '',
      admin_notes: '',
      calibre_override: false,
      is_active: true,
      created_at: '',
      updated_at: '',
      use_cases: [],
      target: {},
    });

    // loadTabData() reloads after a successful quick-mark.
    httpMock.expectOne((r) => r.url === '/api/admin/match/relationships').flush({ items: [], count: 0 });
    httpMock.expectOne((r) => r.url === '/api/admin/match/candidates').flush({ items: [], count: 0 });
  });

  it('the edit panel presents priority as Best Match / Recommended / Alternative, never bare numbers', async () => {
    await setUp();
    const fixture = TestBed.createComponent(AdminMatchEditorPage);
    flushInitialLoad(fixture, [
      {
        id: 7,
        source_shopify_product_id: airgun.id,
        target_shopify_product_id: pellet.id,
        target_category: 'pellet',
        status: 'recommended',
        priority: 2,
        priority_label: 'Recommended',
        reason: '',
        admin_notes: '',
        calibre_override: false,
        is_active: true,
        created_at: '',
        updated_at: '',
        use_cases: [],
        target: { shopify_product_id: pellet.id, category: 'pellet', is_active: true, calibre: '4.5mm', powerplant_type: null, weight_grains: 8, recommended_pellet_weight_min: null, recommended_pellet_weight_max: null, use_cases: [], compatible_powerplants: [] },
      },
    ]);

    const editButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b: any) => b.textContent.trim() === 'Edit',
    ) as HTMLButtonElement;
    editButton.click();
    fixture.detectChanges();

    const modalText = (fixture.nativeElement.querySelector('.modal') as HTMLElement).textContent ?? '';
    expect(modalText).toContain('Best Match');
    expect(modalText).toContain('Recommended');
    expect(modalText).toContain('Alternative');
    expect(modalText).not.toMatch(/\bpriority\s*[:=]?\s*[123]\b/i);
  });
});
