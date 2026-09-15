import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AdminAuthService } from './services/admin-auth.service';
import { API_BASE_URL } from '../../core/config/api-config';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: AdminAuthService;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: '' },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AdminAuthService);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('attaches the bearer token to outgoing requests when one is stored', () => {
    localStorage.setItem('one77_admin_token', 'stored-jwt');
    http.get('/api/admin/learn/categories').subscribe();

    const req = httpMock.expectOne('/api/admin/learn/categories');
    expect(req.request.headers.get('Authorization')).toBe('Bearer stored-jwt');
    req.flush({});
  });

  it('sends no Authorization header when no token is stored', () => {
    http.get('/api/learn/categories').subscribe();
    const req = httpMock.expectOne('/api/learn/categories');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush([]);
  });

  it('logs out and redirects to login on a 401 while authenticated', () => {
    authService.login('a@b.com', 'x').subscribe();
    httpMock.expectOne('/api/admin/login').flush({ access_token: 'stored-jwt', token_type: 'Bearer', admin_user_id: 1, email: 'a@b.com', name: 'A' });
    expect(authService.isAuthenticated()).toBe(true);

    const navigateSpy = vi.spyOn(router, 'navigate');

    http.get('/api/admin/learn/categories').subscribe({ error: () => {} });
    httpMock.expectOne('/api/admin/learn/categories').flush({ error: 'Authentication required.' }, { status: 401, statusText: 'Unauthorized' });

    expect(authService.getToken()).toBeNull();
    expect(navigateSpy).toHaveBeenCalledWith(['/admin/login']);
  });
});
