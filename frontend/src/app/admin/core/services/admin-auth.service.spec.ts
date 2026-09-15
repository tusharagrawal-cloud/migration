import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AdminAuthService } from './admin-auth.service';
import { API_BASE_URL } from '../../../core/config/api-config';

describe('AdminAuthService', () => {
  let service: AdminAuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: API_BASE_URL, useValue: '' }],
    });
    service = TestBed.inject(AdminAuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('login success stores the token and sets the current admin', () => {
    let result: unknown;
    service.login('admin@one77.example', 'correct-password').subscribe((admin) => (result = admin));

    const req = httpMock.expectOne('/api/admin/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'admin@one77.example', password: 'correct-password' });
    req.flush({ access_token: 'jwt-token-value', token_type: 'Bearer', admin_user_id: 1, email: 'admin@one77.example', name: 'Admin' });

    expect(result).toEqual({ admin_user_id: 1, email: 'admin@one77.example', name: 'Admin' });
    expect(service.getToken()).toBe('jwt-token-value');
    expect(service.isAuthenticated()).toBe(true);
  });

  it('login failure does not set a token or current admin', () => {
    let errored = false;
    service.login('admin@one77.example', 'wrong-password').subscribe({ error: () => (errored = true) });

    httpMock.expectOne('/api/admin/login').flush({ error: 'Incorrect email or password.' }, { status: 401, statusText: 'Unauthorized' });

    expect(errored).toBe(true);
    expect(service.getToken()).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
  });

  it('logout discards the token and current admin', () => {
    service.login('admin@one77.example', 'x').subscribe();
    httpMock.expectOne('/api/admin/login').flush({ access_token: 't', token_type: 'Bearer', admin_user_id: 1, email: 'a', name: 'A' });
    expect(service.isAuthenticated()).toBe(true);

    service.logout();

    expect(service.getToken()).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
  });

  it('restoreSession resolves null immediately when no token is stored', () => {
    let result: unknown = 'not-set';
    service.restoreSession().subscribe((admin) => (result = admin));
    httpMock.expectNone('/api/admin/me');
    expect(result).toBeNull();
  });

  it('restoreSession hydrates the current admin from a valid stored token', () => {
    localStorage.setItem('one77_admin_token', 'stored-token');
    let result: unknown;
    service.restoreSession().subscribe((admin) => (result = admin));

    httpMock.expectOne('/api/admin/me').flush({ admin_user_id: 5, email: 'a@b.com', name: 'A' });

    expect(result).toEqual({ admin_user_id: 5, email: 'a@b.com', name: 'A' });
    expect(service.isAuthenticated()).toBe(true);
  });

  it('restoreSession logs out and resolves null when the stored token is rejected', () => {
    localStorage.setItem('one77_admin_token', 'expired-token');
    let result: unknown = 'not-set';
    service.restoreSession().subscribe((admin) => (result = admin));

    httpMock.expectOne('/api/admin/me').flush({ error: 'Authentication required.' }, { status: 401, statusText: 'Unauthorized' });

    expect(result).toBeNull();
    expect(service.getToken()).toBeNull();
  });
});
