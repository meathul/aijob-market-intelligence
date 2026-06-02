import { CommonModule } from '@angular/common';
import { Component, computed, signal } from '@angular/core';

import {
  JobsFiltersComponent,
  JobsFilterState
} from '../../components/jobs-filters/jobs-filters.component';
import { JobsTableComponent, UiJob } from '../../components/jobs-table/jobs-table.component';
import { JobsApiService } from '../../../services/jobs-api.service';
import { JobsRecommendationsApiService } from '../../../services/jobs-recommendations-api.service';
import { JobRawDto } from '../../../models/job.models';

@Component({
  selector: 'app-jobs-page',
  standalone: true,
  imports: [CommonModule, JobsFiltersComponent, JobsTableComponent],
  templateUrl: './jobs-page.component.html',
  styleUrl: './jobs-page.component.scss'
})
export class JobsPageComponent {
  readonly mode = signal<'recommended' | 'all'>('recommended');

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

  constructor(
    private readonly jobsApi: JobsApiService,
    private readonly recApi: JobsRecommendationsApiService
  ) {
    this.refresh();
  }

  onFiltersChanged(next: JobsFilterState) {
    this.filters.set(next);
  }

  setMode(next: 'recommended' | 'all') {
    this.mode.set(next);
    this.refresh();
  }

  refresh() {
    this.loading.set(true);
    this.error.set(null);

    if (this.mode() === 'recommended') {
      this.recApi.list({ take: 20 }).subscribe({
        next: (res) => {
          const rows: JobRawDto[] = (res.jobs ?? []).map((x: any) => x.job) ?? [];
          this.allJobs.set(this.map(rows));
          this.loading.set(false);
        },
        error: (e) => {
          // eslint-disable-next-line no-console
          console.error(e);
          this.mode.set('all');
          this.error.set('Could not load AI recommendations. Showing all jobs instead.');
          this.loadAll();
        }
      });

      return;
    }

    this.loadAll();
  }

  private loadAll() {
    this.jobsApi.list({ pageNumber: 1, pageSize: 50 }).subscribe({
      next: (res) => {
        const rows: JobRawDto[] = res.jobs ?? [];
        this.allJobs.set(this.map(rows));
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

  private map(rows: JobRawDto[]): UiJob[] {
    return rows.map((r) => ({
      title: r.title ?? '—',
      company: r.company ?? r.source ?? '—',
      location: r.location ?? (r.url?.includes('remote') ? 'Remote' : '—'),
      posted: r.postedDate ? new Date(r.postedDate).toLocaleDateString() : '—',
      salary: r.salaryRaw ?? undefined,
      skills: (r.skills ?? [])
        .map((s) => s.skillName)
        .filter((x): x is string => !!x),
      url: r.url ?? undefined
    }));
  }
}
