import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ProductCard } from './product-card';
import { ShopifyProductSummary } from '../../core/models/shopify-product.model';

describe('ProductCard', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductCard],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  function makeProduct(overrides: Partial<ShopifyProductSummary> = {}): ShopifyProductSummary {
    return {
      id: 'gid://shopify/Product/DEV-FIXTURE-1001',
      handle: 'sample-air-rifle',
      title: '[DEV FIXTURE] Sample Air Rifle One',
      vendor: 'Sample Vendor',
      imageUrl: null,
      priceLabel: null,
      availableForSale: true,
      category: 'airgun',
      ...overrides,
    };
  }

  it('strips the [DEV FIXTURE] label from the customer-facing title', () => {
    const fixture = TestBed.createComponent(ProductCard);
    fixture.componentRef.setInput('product', makeProduct());
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Sample Air Rifle One');
    expect(text).not.toContain('[DEV FIXTURE]');
  });

  it('never shows a price, and links to the correct category route', () => {
    const fixture = TestBed.createComponent(ProductCard);
    fixture.componentRef.setInput('product', makeProduct({ category: 'accessory', handle: 'sample-case' }));
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).not.toMatch(/₹|\$|price/i);
    const link: HTMLAnchorElement = fixture.nativeElement.querySelector('a');
    expect(link.getAttribute('href')).toBe('/accessories/sample-case');
  });
});
