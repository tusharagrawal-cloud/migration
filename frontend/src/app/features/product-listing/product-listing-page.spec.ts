import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { ProductListingPage } from './product-listing-page';

describe('ProductListingPage', () => {
  async function setUp(category: string, title: string) {
    await TestBed.configureTestingModule({
      imports: [ProductListingPage],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { data: { category, title } } } },
      ],
    }).compileComponents();
  }

  it('renders airgun dev fixtures under the Airguns heading, with no dev-fixture label or fabricated price', async () => {
    await setUp('airgun', 'Airguns');
    const fixture = TestBed.createComponent(ProductListingPage);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Airguns');
    expect(text).toContain('Sample Air Rifle One');
    expect(text).not.toContain('[DEV FIXTURE]');
    expect(text).not.toMatch(/₹|\$|price/i);
  });

  it('renders pellet dev fixtures under the Pellets heading', async () => {
    await setUp('pellet', 'Pellets');
    const fixture = TestBed.createComponent(ProductListingPage);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Pellets');
    expect(text).toContain('Sample Pellet Tin');
  });
});
