import { UserJobPreferencesDto } from './user-preferences.models';

/** Reads API fields regardless of camelCase / PascalCase JSON. */
function readBool(prefs: Record<string, unknown>, camel: string, pascal: string): boolean {
  const v = prefs[camel] ?? prefs[pascal];
  return v === true;
}

export function isOnboardingComplete(prefs?: UserJobPreferencesDto | null): boolean {
  if (!prefs) return false;
  const raw = prefs as UserJobPreferencesDto & Record<string, unknown>;
  return readBool(raw, 'onboardingCompleted', 'OnboardingCompleted');
}

/** Used for profile form validation before save. */
export function hasMeaningfulJobPreferences(prefs?: UserJobPreferencesDto | null): boolean {
  if (!prefs) return false;

  const hasSalary =
    (prefs.preferredSalaryMin != null && prefs.preferredSalaryMin > 0) ||
    (prefs.preferredSalaryMax != null && prefs.preferredSalaryMax > 0);

  return !!(
    prefs.location?.trim() ||
    prefs.preferredJobTitle?.trim() ||
    prefs.skillsText?.trim() ||
    hasSalary ||
    (prefs.workMode && prefs.workMode !== 'Any')
  );
}
