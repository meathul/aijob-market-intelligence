import { Injectable, inject, signal, effect, computed } from '@angular/core';

import { AuthService } from '../core/auth/auth.service';
import { UiJob } from '../jobs/components/jobs-table/jobs-table.component';

@Injectable({ providedIn: 'root' })
export class ApplicationsService {
  private readonly auth = inject(AuthService);

  private readonly _appliedJobs = signal<UiJob[]>([]);
  readonly appliedJobs = this._appliedJobs.asReadonly();

  readonly appliedIds = computed(() => new Set(this._appliedJobs().map((j) => j.id)));

  constructor() {
    // Sync with localStorage when user identity changes
    effect(() => {
      const email = this.auth.state()?.email;
      if (email) {
        const key = `applied_jobs_${email}`;
        const stored = localStorage.getItem(key);
        if (stored) {
          try {
            this._appliedJobs.set(JSON.parse(stored));
          } catch {
            this._appliedJobs.set([]);
          }
        } else {
          this._appliedJobs.set([]);
        }
      } else {
        this._appliedJobs.set([]);
      }
    }, { allowSignalWrites: true });

    // Save changes to localStorage when appliedJobs changes
    effect(() => {
      const email = this.auth.state()?.email;
      if (email) {
        const key = `applied_jobs_${email}`;
        localStorage.setItem(key, JSON.stringify(this._appliedJobs()));
      }
    });
  }

  apply(job: UiJob) {
    if (this.appliedIds().has(job.id)) return;
    this._appliedJobs.update((jobs) => [...jobs, job]);
  }

  unapply(jobId: number) {
    this._appliedJobs.update((jobs) => jobs.filter((j) => j.id !== jobId));
  }

  isApplied(jobId: number): boolean {
    return this.appliedIds().has(jobId);
  }
}
