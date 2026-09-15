import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { AdminLoginPage } from './admin-login-page';
import { API_BASE_URL } from '../../core/config/api-config';

describe('AdminLoginPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [AdminLoginPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), { provide: API_BASE_URL, useValue: '' }],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('shows a useful message and does not navigate on invalid login', () => {
    const fixture = TestBed.createComponent(AdminLoginPage);
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');
    fixture.detectChanges();

    const email: HTMLInputElement = fixture.nativeElement.querySelector('input[type="email"]');
    const password: HTMLInputElement = fixture.nativeElement.querySelector('input[type="password"]');
    email.value = 'admin@one77.example';
    email.dispatchEvent(new Event('input'));
    password.value = 'wrong-password';
    password.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    httpMock.expectOne('/api/admin/login').flush({ error: 'Incorrect email or password.' }, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Incorrect email or password.');
    expect(text).not.toContain('401');
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('navigates to the dashboard on successful login', () => {
    const fixture = TestBed.createComponent(AdminLoginPage);
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');
    fixture.detectChanges();

    const email: HTMLInputElement = fixture.nativeElement.querySelector('input[type="email"]');
    const password: HTMLInputElement = fixture.nativeElement.querySelector('input[type="password"]');
    email.value = 'admin@one77.example';
    email.dispatchEvent(new Event('input'));
    password.value = 'correct-password';
    password.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    httpMock
      .expectOne('/api/admin/login')
      .flush({ access_token: 'jwt', token_type: 'Bearer', admin_user_id: 1, email: 'admin@one77.example', name: 'Admin' });

    expect(navigateSpy).toHaveBeenCalledWith(['/admin/dashboard']);
  });
});
