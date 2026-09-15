import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { firstValueFrom, isObservable, of } from 'rxjs';
import { authGuard } from './auth.guard';
import { AdminAuthService } from './services/admin-auth.service';
import { API_BASE_URL } from '../../core/config/api-config';

describe('authGuard', () => {
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), { provide: API_BASE_URL, useValue: '' }],
    });
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  async function runGuard(): Promise<boolean> {
    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
    if (isObservable(result)) {
      return (await firstValueFrom(result)) as boolean;
    }
    return (await result) as boolean;
  }

  it('redirects to login and blocks access when there is no stored token', async () => {
    const navigateSpy = vi.spyOn(router, 'navigate');
    const allowed = await runGuard();

    expect(allowed).toBe(false);
    expect(navigateSpy).toHaveBeenCalledWith(['/admin/login']);
  });

  it('allows access without a network call when already authenticated', async () => {
    const authService = TestBed.inject(AdminAuthService);
    authService.login('a@b.com', 'x').subscribe();
    httpMock.expectOne('/api/admin/login').flush({ access_token: 't', token_type: 'Bearer', admin_user_id: 1, email: 'a@b.com', name: 'A' });

    const allowed = await runGuard();

    expect(allowed).toBe(true);
    httpMock.expectNone('/api/admin/me');
  });

  it('hydrates from a stored token and allows access when the token is valid', async () => {
    localStorage.setItem('one77_admin_token', 'valid-token');

    const pending = runGuard();
    httpMock.expectOne('/api/admin/me').flush({ admin_user_id: 1, email: 'a@b.com', name: 'A' });

    expect(await pending).toBe(true);
  });

  it('redirects to login when the stored token is rejected', async () => {
    localStorage.setItem('one77_admin_token', 'expired-token');
    const navigateSpy = vi.spyOn(router, 'navigate');

    const pending = runGuard();
    httpMock.expectOne('/api/admin/me').flush({ error: 'Authentication required.' }, { status: 401, statusText: 'Unauthorized' });

    expect(await pending).toBe(false);
    expect(navigateSpy).toHaveBeenCalledWith(['/admin/login']);
  });
});
