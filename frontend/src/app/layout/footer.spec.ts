import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Footer } from './footer';

describe('Footer', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Footer],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('renders the ONE77 logo and shop/guidance links', () => {
    const fixture = TestBed.createComponent(Footer);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-logo')).toBeTruthy();
    const links = Array.from(fixture.nativeElement.querySelectorAll('a')).map((a: any) => a.textContent.trim());
    expect(links).toEqual(['Airguns', 'Pellets', 'Accessories', 'Bundles', 'Learning Centre', 'Live Webinar']);
  });

  it('shows the current year in the legal line', () => {
    const fixture = TestBed.createComponent(Footer);
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain(String(new Date().getFullYear()));
  });
});
