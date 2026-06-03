import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { UserPreferencesApiService } from '../../services/user-preferences-api.service';
import { isOnboardingComplete } from '../../models/user/user-preferences.utils';

/** Login/register only when signed out; signed-in users go to profile or dashboard. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const prefsApi = inject(UserPreferencesApiService);

  if (!auth.isAuthenticated()) return true;

  return prefsApi.get().pipe(
    map((prefs) =>
      router.parseUrl(
        isOnboardingComplete(prefs) ? '/dashboard' : '/onboarding'
      )
    ),
    catchError(() => of(router.parseUrl('/onboarding')))
  );
};
