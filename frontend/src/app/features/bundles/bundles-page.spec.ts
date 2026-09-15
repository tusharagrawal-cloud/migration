import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { BundlesPage } from './bundles-page';
import { SHOPIFY_DEV_FIXTURE_PRODUCTS } from '../../core/services/shopify-dev-fixtures';
import { PublicBundle } from '../../core/models/bundle.models';
import { API_BASE_URL } from '../../core/config/api-config';

describe('BundlesPage', () => {
  let httpMock: HttpTestingController;
  const fixtureProduct = SHOPIFY_DEV_FIXTURE_PRODUCTS[0];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BundlesPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: '' },
      ],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('renders bundle name, tagline, primary item, and no price/savings figures or dev-fixture label', () => {
    const fixture = TestBed.createComponent(BundlesPage);
    fixture.detectChanges();

    const bundles: PublicBundle[] = [
      {
        id: 1,
        name: 'Beginner Setup',
        tagline: 'Everything to get started',
        sort_priority: 1,
        items: [{ shopify_product_id: fixtureProduct.id, item_role: 'Primary', sort_order: 1 }],
      },
    ];
    httpMock.expectOne('/api/bundles').flush(bundles);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Beginner Setup');
    expect(text).toContain('Everything to get started');
    expect(text).toContain(fixtureProduct.title.replace('[DEV FIXTURE] ', ''));
    expect(text).not.toContain('[DEV FIXTURE]');
    expect(text).not.toMatch(/\$\d/);
    expect(text).not.toMatch(/savings|discount/i);
  });

  it('shows an empty state when there are no bundles', () => {
    const fixture = TestBed.createComponent(BundlesPage);
    fixture.detectChanges();

    httpMock.expectOne('/api/bundles').flush([]);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No bundles yet');
  });
});
