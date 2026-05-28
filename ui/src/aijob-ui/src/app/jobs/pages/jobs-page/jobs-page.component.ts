import { Component, computed, signal } from '@angular/core';

import {
  JobsFiltersComponent,
  JobsFilterState
} from '../../components/jobs-filters/jobs-filters.component';
import { JobsTableComponent, UiJob } from '../../components/jobs-table/jobs-table.component';
import { JobsApiService } from '../../../services/jobs-api.service';
import { JobRawDto } from '../../../models/job.models';

@Component({
  selector: 'app-jobs-page',
  standalone: true,
  imports: [JobsFiltersComponent, JobsTableComponent],
  templateUrl: './jobs-page.component.html',
  styleUrl: './jobs-page.component.scss'
})
export class JobsPageComponent {
  readonly filters = signal<JobsFilterState>({
    query: '',
    location: 'Any',
    remoteOnly: true
  });

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  private readonly allJobs = signal<UiJob[]>([]);

  readonly jobs = computed(() => {
    const f = this.filters();
    const q = f.query.trim().toLowerCase();

    return this.allJobs().filter((j) => {
      if (f.remoteOnly && (j.location ?? '').toLowerCase() !== 'remote') return false;
      if (f.location !== 'Any' && (j.location ?? '') !== f.location) return false;

      if (!q) return true;

      const hay = [
        j.title,
        j.company ?? '',
        j.location ?? '',
        j.salary ?? '',
        ...(j.skills ?? [])
      ]
        .join(' ')
        .toLowerCase();

      return hay.includes(q);
    });
  });

  constructor(private readonly jobsApi: JobsApiService) {
    this.refresh();
  }

  onFiltersChanged(next: JobsFilterState) {
    this.filters.set(next);
  }

  refresh() {
    this.loading.set(true);
    this.error.set(null);

    this.jobsApi.list({ pageNumber: 1, pageSize: 50 }).subscribe({
      next: (res) => {
        const rows: JobRawDto[] = res.jobs ?? [];
        const mapped: UiJob[] = rows.map((r) => ({
          title: r.title ?? '—',
          company: r.company ?? r.source ?? '—',
          location: r.location ?? (r.url?.includes('remote') ? 'Remote' : '—'),
          posted: r.postedDate ? new Date(r.postedDate).toLocaleDateString() : '—',
          salary: r.salaryRaw ?? undefined,
          skills: (r.skills ?? [])
            .map((s) => s.skillName)
            .filter((x): x is string => !!x)
        }));

        this.allJobs.set(mapped);
        this.loading.set(false);
      },
      error: (e) => {
        this.loading.set(false);
        this.error.set('Failed to load jobs from API. Check API base URL / CORS / backend status.');
        this.allJobs.set([]);
        // eslint-disable-next-line no-console
        console.error(e);
      }
    });
  }
}
