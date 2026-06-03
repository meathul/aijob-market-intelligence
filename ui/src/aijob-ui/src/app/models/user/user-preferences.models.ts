export type UserJobPreferencesDto = {
  location?: string | null;
  preferredSalaryMin?: number | null;
  preferredSalaryMax?: number | null;
  preferredJobTitle?: string | null;
  workMode?: string | null; // Remote | Hybrid | Onsite | Any
  skillsText?: string | null;
  onboardingCompleted?: boolean;
};
