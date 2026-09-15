import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, of } from 'rxjs';
import { AdminAuthService } from './services/admin-auth.service';

/**
 * Protects every Admin route except /admin/login. Server-side enforcement
 * (JwtAuthorizeAttribute / the DevHost's equivalent middleware) is the real
 * security boundary — this guard exists purely so an operator without a
 * valid session lands on the login screen instead of a broken/empty page.
 */
export const authGuard: CanActivateFn = () => {
  const authService = inject(AdminAuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return of(true);
  }

  if (!authService.getToken()) {
    router.navigate(['/admin/login']);
    return of(false);
  }

  return authService.restoreSession().pipe(
    map((admin) => {
      if (admin) {
        return true;
      }
      router.navigate(['/admin/login']);
      return false;
    }),
  );
};
