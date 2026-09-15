import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminMatchListPage } from './admin-match-list-page';
import { SHOPIFY_DEV_FIXTURE_PRODUCTS } from '../../../core/services/shopify-dev-fixtures';
import { API_BASE_URL } from '../../../core/config/api-config';

describe('AdminMatchListPage', () => {
  let httpMock: HttpTestingController;
  const airgun = SHOPIFY_DEV_FIXTURE_PRODUCTS.find((p) => p.category === 'airgun')!;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminMatchListPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), { provide: API_BASE_URL, useValue: '' }],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('shows completeness state in plain business language, resolves a name (never a raw GID), and links to the editor', () => {
    const fixture = TestBed.createComponent(AdminMatchListPage);
    fixture.detectChanges();

    httpMock.expectOne('/api/admin/match/airguns').flush({
      items: [
        {
          shopify_product_id: airgun.id,
          calibre: '4.5mm',
          powerplant_type: 'springer',
          counts: { compat_pellets: 1, rec_pellets: 1, compat_acc: 1, rec_acc: 0, state: 'complete' },
        },
      ],
      count: 1,
    });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain(airgun.title);
    expect(text).toContain('Complete');
    expect(text).not.toContain(airgun.id);
    expect(text).not.toContain('no_matches');
    expect(text).not.toContain('needs_review');

    const link: HTMLAnchorElement = fixture.nativeElement.querySelector('a.tile');
    // routerLink encodes the GID's "/" characters itself (a single array element is one path
    // segment) — asserting the decoded href avoids coupling this test to Angular's exact escaping.
    expect(decodeURIComponent(link.getAttribute('href') ?? '')).toContain(airgun.id);
  });

  it('shows an empty state when there are no airguns', () => {
    const fixture = TestBed.createComponent(AdminMatchListPage);
    fixture.detectChanges();

    httpMock.expectOne('/api/admin/match/airguns').flush({ items: [], count: 0 });
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No airguns match');
  });
});
