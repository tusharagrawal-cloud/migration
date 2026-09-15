import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api-config';
import { AdminMe, LoginResponse } from '../models/admin-auth.models';

const TOKEN_STORAGE_KEY = 'one77_admin_token';

/**
 * V1 admin auth: a stateless JWT stored in localStorage, attached by
 * AuthInterceptor, checked by AuthGuard. No refresh tokens, no session
 * management UI — logout is simply discarding the token (Milestone 10's
 * approved V1 scope).
 */
@Injectable({ providedIn: 'root' })
export class AdminAuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  private readonly currentAdminSignal = signal<AdminMe | null>(null);
  readonly currentAdmin = this.currentAdminSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.currentAdminSignal() !== null);

  getToken(): string | null {
    return localStorage.getItem(TOKEN_STORAGE_KEY);
  }

  login(email: string, password: string): Observable<AdminMe> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/api/admin/login`, { email, password }).pipe(
      tap((res) => localStorage.setItem(TOKEN_STORAGE_KEY, res.access_token)),
      map((res): AdminMe => ({ admin_user_id: res.admin_user_id, email: res.email, name: res.name })),
      tap((admin) => this.currentAdminSignal.set(admin)),
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    this.currentAdminSignal.set(null);
  }

  /** Called once at app/guard startup to hydrate the session from a stored token, if any. */
  restoreSession(): Observable<AdminMe | null> {
    const token = this.getToken();
    if (!token) {
      return of(null);
    }
    return this.http.get<AdminMe>(`${this.baseUrl}/api/admin/me`).pipe(
      tap((admin) => this.currentAdminSignal.set(admin)),
      catchError(() => {
        this.logout();
        return of(null);
      }),
    );
  }
}
