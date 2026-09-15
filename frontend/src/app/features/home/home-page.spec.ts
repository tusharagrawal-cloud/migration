import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { HomePage } from './home-page';
import { SHOPIFY_DEV_FIXTURE_PRODUCTS } from '../../core/services/shopify-dev-fixtures';
import { API_BASE_URL } from '../../core/config/api-config';

describe('HomePage', () => {
  let httpMock: HttpTestingController;
  const airgun = SHOPIFY_DEV_FIXTURE_PRODUCTS.find((p) => p.category === 'airgun')!;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HomePage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), { provide: API_BASE_URL, useValue: '' }],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function flushEmpty(fixture: ReturnType<typeof TestBed.createComponent>) {
    fixture.detectChanges();
    httpMock.expectOne('/api/homepage').flush({ hero_image_url: null });
    httpMock.expectOne('/api/bundles').flush([]);
    httpMock.expectOne('/api/learn/categories').flush([]);
    httpMock.expectOne('/api/webinar/events').flush([]);
    fixture.detectChanges();
  }

  it('renders the branded hero and the four shop-by-intent paths', () => {
    const fixture = TestBed.createComponent(HomePage);
    flushEmpty(fixture);

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Get the setup right.');
    expect(text).toContain('My First Airgun');
    expect(text).toContain('Target Practice');
    expect(text).toContain('Plinking');
    expect(text).toContain('Upgrade My Setup');
  });

  it('renders featured airguns with the dev-fixture label stripped and no fabricated price', () => {
    const fixture = TestBed.createComponent(HomePage);
    flushEmpty(fixture);

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain(airgun.title.replace('[DEV FIXTURE] ', ''));
    expect(text).not.toContain('[DEV FIXTURE]');
    expect(text).not.toMatch(/price via shopify/i);
  });

  it('hides the Bundles/Learn sections entirely when there is nothing published, and shows the Match explainer regardless', () => {
    const fixture = TestBed.createComponent(HomePage);
    flushEmpty(fixture);

    expect(fixture.nativeElement.querySelector('.bundle-grid')).toBeNull();
    expect(fixture.nativeElement.querySelector('.learn-grid')).toBeNull();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Best Match');
    expect(text).toContain('Recommended');
    expect(text).toContain('Compatible');
  });

  it('shows curated bundles (resolving item names) when the API returns published bundles', () => {
    const pellet = SHOPIFY_DEV_FIXTURE_PRODUCTS.find((p) => p.category === 'pellet')!;
    const fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();
    httpMock.expectOne('/api/homepage').flush({ hero_image_url: null });
    httpMock.expectOne('/api/bundles').flush([
      {
        id: 1,
        name: 'Beginner Setup',
        tagline: 'Everything to get started',
        sort_priority: 0,
        items: [
          { shopify_product_id: airgun.id, item_role: 'Primary', sort_order: 0 },
          { shopify_product_id: pellet.id, item_role: 'Pellet', sort_order: 1 },
        ],
      },
    ]);
    httpMock.expectOne('/api/learn/categories').flush([]);
    httpMock.expectOne('/api/webinar/events').flush([]);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Beginner Setup');
    expect(text).toContain(airgun.title.replace('[DEV FIXTURE] ', ''));
    expect(text).toContain(pellet.title.replace('[DEV FIXTURE] ', ''));
  });

  it('shows the next webinar session when one is upcoming', () => {
    const fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();
    httpMock.expectOne('/api/homepage').flush({ hero_image_url: null });
    httpMock.expectOne('/api/bundles').flush([]);
    httpMock.expectOne('/api/learn/categories').flush([]);
    httpMock.expectOne('/api/webinar/events').flush([
      { event_id: 1, title: 'Intro Session', event_date_time: '2027-01-15T18:00:00Z', join_link: null },
    ]);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Reserve My Seat');
    expect(text).toContain('Next session');
  });

  it('never leaks a raw Shopify GID anywhere on the page', () => {
    const fixture = TestBed.createComponent(HomePage);
    flushEmpty(fixture);

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).not.toContain('gid://shopify');
  });

  it('shows the decorative mark and no hero photo when no hero image is configured', () => {
    const fixture = TestBed.createComponent(HomePage);
    flushEmpty(fixture);

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.hero-mark')).toBeTruthy();
    expect(compiled.querySelector('.hero-photo')).toBeNull();
    expect(compiled.querySelector('.hero-scrim')).toBeNull();
  });

  it('shows a full-bleed hero photo (and hides the decorative mark) when a hero image is configured', () => {
    const fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();
    httpMock.expectOne('/api/homepage').flush({ hero_image_url: '/media/hero-abc123.jpg' });
    httpMock.expectOne('/api/bundles').flush([]);
    httpMock.expectOne('/api/learn/categories').flush([]);
    httpMock.expectOne('/api/webinar/events').flush([]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const photo: HTMLImageElement | null = compiled.querySelector('.hero-photo');
    expect(photo).toBeTruthy();
    expect(photo!.src).toContain('/media/hero-abc123.jpg');
    expect(compiled.querySelector('.hero-scrim')).toBeTruthy();
    expect(compiled.querySelector('.hero-mark')).toBeNull();

    // The existing headline/copy/CTAs still render, untouched by which hero variant is showing.
    const text = compiled.textContent ?? '';
    expect(text).toContain('Get the setup right.');
    expect(compiled.querySelector('.hero-actions .btn-primary')).toBeTruthy();
  });

  it('falls back to the standard dark hero if the homepage config request fails', () => {
    const fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();
    httpMock.expectOne('/api/homepage').flush('boom', { status: 500, statusText: 'Server Error' });
    httpMock.expectOne('/api/bundles').flush([]);
    httpMock.expectOne('/api/learn/categories').flush([]);
    httpMock.expectOne('/api/webinar/events').flush([]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.hero-mark')).toBeTruthy();
    expect(compiled.querySelector('.hero-photo')).toBeNull();
  });
});
