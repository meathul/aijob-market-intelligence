import { Injectable, inject, signal, effect, computed } from '@angular/core';

import { AuthService } from '../core/auth/auth.service';
import { ApiClient } from '../core/http/api-client';
import { UiJob } from '../jobs/components/jobs-table/jobs-table.component';

@Injectable({ providedIn: 'root' })
export class ApplicationsService {
  private readonly auth = inject(AuthService);
  private readonly http = inject(ApiClient);

  private readonly _appliedJobs = signal<UiJob[]>([]);
  readonly appliedJobs = this._appliedJobs.asReadonly();

  readonly appliedIds = computed(() => new Set(this._appliedJobs().map((j) => j.id)));

  constructor() {
    // Sync with database when user identity changes
    effect(() => {
      const email = this.auth.state()?.email;
      if (email) {
        this.http.get<any[]>('api/applications').subscribe({
          next: (jobs) => {
            this._appliedJobs.set(this.map(jobs));
          },
          error: (err) => {
            console.error('Failed to load database applied jobs:', err);
            this._appliedJobs.set([]);
          }
        });
      } else {
        this._appliedJobs.set([]);
      }
    }, { allowSignalWrites: true });
  }

  apply(job: UiJob) {
    if (this.appliedIds().has(job.id)) return;
    
    // Optimistic UI update
    this._appliedJobs.update((jobs) => [...jobs, job]);

    // Persist to database
    this.http.post(`api/applications/${job.id}`, {}).subscribe({
      error: (err) => {
        console.error('Failed to save job application to database:', err);
        // Rollback on error
        this._appliedJobs.update((jobs) => jobs.filter((j) => j.id !== job.id));
      }
    });
  }

  unapply(jobId: number) {
    const jobToRollback = this._appliedJobs().find((j) => j.id === jobId);
    if (!jobToRollback) return;

    // Optimistic UI update
    this._appliedJobs.update((jobs) => jobs.filter((j) => j.id !== jobId));

    // Remove from database
    this.http.delete(`api/applications/${jobId}`).subscribe({
      error: (err) => {
        console.error('Failed to delete job application from database:', err);
        // Rollback on error
        this._appliedJobs.update((jobs) => [...jobs, jobToRollback]);
      }
    });
  }

  isApplied(jobId: number): boolean {
    return this.appliedIds().has(jobId);
  }

  private map(rows: any[]): UiJob[] {
    return rows.map((r) => ({
      id: r.id,
      title: r.title ?? '—',
      company: r.company ?? r.source ?? '—',
      location: r.location ?? (r.url?.includes('remote') ? 'Remote' : '—'),
      posted: r.postedDate ? new Date(r.postedDate).toLocaleDateString() : '—',
      salary: r.salaryRaw ?? undefined,
      skills: (r.skills ?? [])
        .map((s: any) => s.skillName)
        .filter((x: any): x is string => !!x),
      url: r.url ?? undefined
    }));
  }
}
