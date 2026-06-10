import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';

import { UserPreferencesApiService } from '../../../services/user-preferences-api.service';
import { hasMeaningfulJobPreferences } from '../../../models/user/user-preferences.utils';

@Component({
  selector: 'app-profile-setup-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './profile-setup-page.component.html'
})
export class ProfileSetupPageComponent implements OnInit {
  private readonly prefsApi = inject(UserPreferencesApiService);
  private readonly router = inject(Router);

  readonly location = signal('');
  readonly preferredJobTitle = signal('');
  readonly preferredSalaryMin = signal<number | null>(null);
  readonly preferredSalaryMax = signal<number | null>(null);
  readonly workMode = signal<'Any' | 'Remote' | 'Hybrid' | 'Onsite'>('Any');
  readonly skillsText = signal('');

  readonly error = signal<string | null>(null);
  readonly loading = signal(false);
  readonly isExistingUser = signal(false);

  ngOnInit() {
    this.loading.set(true);
    this.prefsApi.get().subscribe({
      next: (prefs) => {
        this.loading.set(false);
        if (prefs) {
          this.isExistingUser.set(prefs.onboardingCompleted ?? false);
          this.location.set(prefs.location || '');
          this.preferredJobTitle.set(prefs.preferredJobTitle || '');
          this.preferredSalaryMin.set(prefs.preferredSalaryMin ?? null);
          this.preferredSalaryMax.set(prefs.preferredSalaryMax ?? null);
          this.workMode.set(prefs.workMode as any ?? 'Any');
          this.skillsText.set(prefs.skillsText || '');
        }
      },
      error: (e) => {
        this.loading.set(false);
        // Clean error display but log to console
        console.error('Failed to load existing preferences:', e);
      }
    });
  }

  readonly canSubmit = computed(() => {
    const min = this.preferredSalaryMin();
    const max = this.preferredSalaryMax();
    if (min !== null && min < 0) return false;
    if (max !== null && max < 0) return false;
    if (min !== null && max !== null && min > max) return false;

    return hasMeaningfulJobPreferences({
      location: this.location(),
      preferredJobTitle: this.preferredJobTitle(),
      preferredSalaryMin: this.preferredSalaryMin(),
      preferredSalaryMax: this.preferredSalaryMax(),
      workMode: this.workMode(),
      skillsText: this.skillsText()
    });
  });

  async submit() {
    if (!this.canSubmit()) {
      this.error.set(
        'Add at least one preference (location, job title, skills, salary range, or work mode other than Any).'
      );
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.prefsApi
      .upsert({
        location: this.location().trim() || null,
        preferredJobTitle: this.preferredJobTitle().trim() || null,
        preferredSalaryMin: this.preferredSalaryMin(),
        preferredSalaryMax: this.preferredSalaryMax(),
        workMode: this.workMode(),
        skillsText: this.skillsText().trim() || null
      })
      .subscribe({
        next: async () => {
          this.loading.set(false);
          await this.router.navigateByUrl('/dashboard');
        },
        error: (e) => {
          this.loading.set(false);
          this.error.set('Failed to save preferences.');
          // eslint-disable-next-line no-console
          console.error(e);
        }
      });
  }
}
