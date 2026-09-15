import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminBundlesListPage } from './admin-bundles-list-page';
import { API_BASE_URL } from '../../../core/config/api-config';

describe('AdminBundlesListPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminBundlesListPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), { provide: API_BASE_URL, useValue: '' }],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists bundles with status and item count, and shows no price/savings figures', () => {
    const fixture = TestBed.createComponent(AdminBundlesListPage);
    fixture.detectChanges();

    httpMock.expectOne('/api/admin/bundles').flush({
      items: [
        {
          id: 1,
          name: 'Beginner Setup',
          tagline: 'Everything to get started',
          status: 'Draft',
          sort_priority: 0,
          created_at: '',
          updated_at: '',
          items: [{ id: 1, shopify_product_id: 'x', item_role: 'Primary', sort_order: 0 }],
        },
      ],
      count: 1,
    });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Beginner Setup');
    expect(text).toContain('Draft');
    expect(text).toContain('1 product(s)');
    expect(text).not.toMatch(/\$\d/);
    expect(text).not.toContain('savings');
  });

  it('shows an empty state with no bundles', () => {
    const fixture = TestBed.createComponent(AdminBundlesListPage);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/bundles').flush({ items: [], count: 0 });
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No bundles yet');
  });
});
