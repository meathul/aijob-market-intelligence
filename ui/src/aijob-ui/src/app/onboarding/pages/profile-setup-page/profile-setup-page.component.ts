import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';

import { UserPreferencesApiService } from '../../../services/user-preferences-api.service';

@Component({
  selector: 'app-profile-setup-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './profile-setup-page.component.html'
})
export class ProfileSetupPageComponent {
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

  readonly canSubmit = computed(() => {
    const min = this.preferredSalaryMin();
    const max = this.preferredSalaryMax();
    if (min !== null && min < 0) return false;
    if (max !== null && max < 0) return false;
    if (min !== null && max !== null && min > max) return false;
    return true;
  });

  async submit() {
    if (!this.canSubmit()) {
      this.error.set('Please fix salary range.');
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
          await this.router.navigateByUrl('/jobs');
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
