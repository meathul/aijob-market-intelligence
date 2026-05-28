import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';

export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const roles = auth.state()?.roles ?? [];
  if (auth.isAuthenticated() && roles.includes('Admin')) return true;

  // If logged in but not admin, send to dashboard.
  if (auth.isAuthenticated()) return router.parseUrl('/dashboard');

  return router.parseUrl('/auth/login?mode=admin');
};
