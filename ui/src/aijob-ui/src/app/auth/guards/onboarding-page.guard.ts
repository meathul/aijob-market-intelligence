import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import { UserPreferencesApiService } from '../../services/user-preferences-api.service';
import { isOnboardingComplete } from '../../models/user/user-preferences.utils';

/** Profile setup is only for users who have not finished onboarding yet. */
export const onboardingPageGuard: CanActivateFn = () => {
  const prefsApi = inject(UserPreferencesApiService);
  const router = inject(Router);

  return prefsApi.get().pipe(
    map((prefs) =>
      isOnboardingComplete(prefs) ? router.parseUrl('/dashboard') : true
    ),
    catchError(() => of(true))
  );
};
