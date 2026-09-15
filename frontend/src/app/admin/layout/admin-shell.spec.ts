import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { AdminShell } from './admin-shell';
import { AdminAuthService } from '../core/services/admin-auth.service';

describe('AdminShell', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AdminShell],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
  });

  it('renders shallow navigation to every top-level Admin area', () => {
    const fixture = TestBed.createComponent(AdminShell);
    fixture.detectChanges();

    const links = Array.from(fixture.nativeElement.querySelectorAll('.admin-nav a')).map((a: any) => a.textContent.trim());
    expect(links).toEqual(['Dashboard', 'Homepage', 'Products', 'Matches', 'Bundles', 'Learn', 'Webinars']);
  });

  it('logout clears the session and navigates to login', () => {
    const fixture = TestBed.createComponent(AdminShell);
    const authService = TestBed.inject(AdminAuthService);
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');
    const logoutSpy = vi.spyOn(authService, 'logout');
    fixture.detectChanges();

    const logoutButton: HTMLButtonElement = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b: any) => b.textContent.includes('Log out'),
    ) as HTMLButtonElement;
    logoutButton.click();

    expect(logoutSpy).toHaveBeenCalled();
    expect(navigateSpy).toHaveBeenCalledWith(['/admin/login']);
  });

  it('toggles the mobile menu open state', () => {
    const fixture = TestBed.createComponent(AdminShell);
    fixture.detectChanges();

    const toggle: HTMLButtonElement = fixture.nativeElement.querySelector('.admin-menu-toggle');
    const sidebar: HTMLElement = fixture.nativeElement.querySelector('.admin-sidebar');

    expect(sidebar.classList.contains('open')).toBe(false);
    toggle.click();
    fixture.detectChanges();
    expect(sidebar.classList.contains('open')).toBe(true);
  });
});
