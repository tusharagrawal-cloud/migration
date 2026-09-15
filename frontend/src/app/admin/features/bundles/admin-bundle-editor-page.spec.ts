import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { AdminBundleEditorPage } from './admin-bundle-editor-page';
import { SHOPIFY_DEV_FIXTURE_PRODUCTS } from '../../../core/services/shopify-dev-fixtures';
import { API_BASE_URL } from '../../../core/config/api-config';

describe('AdminBundleEditorPage (new bundle)', () => {
  let httpMock: HttpTestingController;
  const airgun = SHOPIFY_DEV_FIXTURE_PRODUCTS.find((p) => p.category === 'airgun')!;
  const pellet = SHOPIFY_DEV_FIXTURE_PRODUCTS.find((p) => p.category === 'pellet')!;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminBundleEditorPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        // A real route for the post-create redirect target, so that navigation doesn't reject.
        provideRouter([{ path: 'admin/bundles/:id', component: AdminBundleEditorPage }]),
        { provide: API_BASE_URL, useValue: '' },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'new' }) } } },
      ],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('requires a name and a primary product before saving, in plain language', () => {
    const fixture = TestBed.createComponent(AdminBundleEditorPage);
    fixture.detectChanges();

    const saveButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b: any) => b.textContent.trim() === 'Save',
    ) as HTMLButtonElement;
    saveButton.click();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Please give this bundle a name.');
  });

  it('never shows a price, savings, or SortOrder field', () => {
    const fixture = TestBed.createComponent(AdminBundleEditorPage);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).not.toMatch(/price/i);
    expect(text).not.toMatch(/savings/i);
    expect(text).not.toContain('SortOrder');
    expect(text).not.toContain('sort_order');
  });

  it('saves the primary product and item order exactly as arranged', () => {
    const fixture = TestBed.createComponent(AdminBundleEditorPage);
    fixture.detectChanges();

    const nameInput: HTMLInputElement = fixture.nativeElement.querySelector('input[name="bundleName"]');
    nameInput.value = 'Beginner Setup';
    nameInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    // Pick the primary product via the real picker UI (fixtures resolve synchronously).
    const primaryPickButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('.picker-item')).find((b: any) =>
      b.textContent.includes(airgun.title),
    ) as HTMLButtonElement;
    primaryPickButton.click();
    fixture.detectChanges();

    // Pick a pellet via the pellets picker (now the only remaining app-shopify-product-picker).
    const pelletPickButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('.picker-item')).find((b: any) =>
      b.textContent.includes(pellet.title),
    ) as HTMLButtonElement;
    pelletPickButton.click();
    fixture.detectChanges();

    const saveButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b: any) => b.textContent.trim() === 'Save',
    ) as HTMLButtonElement;
    saveButton.click();

    const req = httpMock.expectOne((r) => r.url === '/api/admin/bundles' && r.method === 'POST');
    expect(req.request.body.items).toEqual([
      { shopify_product_id: airgun.id, item_role: 'Primary' },
      { shopify_product_id: pellet.id, item_role: 'Pellet' },
    ]);
    req.flush({ id: 5, name: 'Beginner Setup', tagline: null, status: 'Draft', sort_priority: 0, created_at: '', updated_at: '', items: [] });

    // Angular reuses this component instance across the post-create navigate (same route
    // config, only :id changes) — the lifecycle "Show on Website" control must appear without
    // a page reload, proving isNew/bundleId/status were updated locally, not left stale.
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Show on Website');
    expect(text).not.toContain('Create bundle');
  });
});
