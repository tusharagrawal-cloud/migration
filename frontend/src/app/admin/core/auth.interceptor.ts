import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AdminAuthService } from './services/admin-auth.service';

/**
 * Attaches the admin bearer token to every request, and on a 401 from any
 * request (expired/invalid token, or an admin disabled server-side), signs
 * the operator out and returns them to the login screen — no silent retry,
 * no refresh-token dance, matching the approved V1 auth scope.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AdminAuthService);
  const router = inject(Router);
  const token = authService.getToken();

  const authorizedReq = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authorizedReq).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && authService.isAuthenticated()) {
        authService.logout();
        router.navigate(['/admin/login']);
      }
      return throwError(() => error);
    }),
  );
};
