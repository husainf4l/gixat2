import { inject } from '@angular/core';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from './services/auth.service';
import { map, catchError, of } from 'rxjs';

export const authGuard = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.me().pipe(
    map(data => {
      if (data && data.me) {
        const hasOrg = !!data.me.organizationId;
        const isSetupPage = state.url.includes('/organization-setup');

        if (!hasOrg && !isSetupPage) {
          router.navigate(['/organization-setup']);
          return false;
        }

        if (hasOrg && isSetupPage) {
          router.navigate(['/dashboard']);
          return false;
        }

        return true;
      }
      router.navigate(['/auth/login']);
      return false;
    }),
    catchError(() => {
      router.navigate(['/auth/login']);
      return of(false);
    })
  );
};
