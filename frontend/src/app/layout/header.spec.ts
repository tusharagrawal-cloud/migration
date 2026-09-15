import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Header } from './header';

describe('Header', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Header],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('renders the ONE77 logo and grouped navigation triggers', () => {
    const fixture = TestBed.createComponent(Header);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('app-logo').length).toBeGreaterThan(0);
    const triggers = Array.from(fixture.nativeElement.querySelectorAll('.nav-group-trigger')).map((b: any) =>
      b.textContent.trim(),
    );
    expect(triggers).toEqual(['Shop', 'Learn']);
  });

  it('opens a nav group to reveal its links, and closes it again', () => {
    const fixture = TestBed.createComponent(Header);
    fixture.detectChanges();

    const shopTrigger: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('.nav-group-trigger')).find(
      (b: any) => b.textContent.trim() === 'Shop',
    ) as HTMLButtonElement;

    expect(fixture.nativeElement.querySelector('.nav-dropdown')).toBeNull();

    shopTrigger.click();
    fixture.detectChanges();

    const shopLinks = Array.from(fixture.nativeElement.querySelectorAll('.nav-dropdown a')).map((a: any) =>
      a.textContent.trim(),
    );
    expect(shopLinks).toEqual(['Airguns', 'Pellets', 'Accessories', 'Bundles']);

    shopTrigger.click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.nav-dropdown')).toBeNull();
  });

  it('toggles the mobile navigation open state and lists every link', () => {
    const fixture = TestBed.createComponent(Header);
    fixture.detectChanges();
    const toggle: HTMLButtonElement = fixture.nativeElement.querySelector('.nav-toggle');

    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    expect(fixture.nativeElement.querySelector('.mobile-nav')).toBeNull();

    toggle.click();
    fixture.detectChanges();

    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    const mobileLinks = Array.from(fixture.nativeElement.querySelectorAll('.mobile-group a')).map((a: any) =>
      a.textContent.trim(),
    );
    expect(mobileLinks).toEqual(['Airguns', 'Pellets', 'Accessories', 'Bundles', 'All Guides', 'Live Webinar']);
  });
});
