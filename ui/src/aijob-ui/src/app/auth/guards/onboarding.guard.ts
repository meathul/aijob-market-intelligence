import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import { UserPreferencesApiService } from '../../services/user-preferences-api.service';
import { isOnboardingComplete } from '../../models/user/user-preferences.utils';

/** Requires a completed job profile before accessing main app pages. */
export const onboardingGuard: CanActivateFn = () => {
  const prefsApi = inject(UserPreferencesApiService);
  const router = inject(Router);

  return prefsApi.get().pipe(
    map((prefs) =>
      isOnboardingComplete(prefs) ? true : router.parseUrl('/onboarding')
    ),
    catchError(() => of(router.parseUrl('/onboarding')))
  );
};
