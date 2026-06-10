import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterModule } from '@angular/router';

import {
  JobsFiltersComponent,
  JobsFilterState
} from '../../components/jobs-filters/jobs-filters.component';
import { JobsTableComponent, UiJob } from '../../components/jobs-table/jobs-table.component';
import { JobsApiService } from '../../../services/jobs-api.service';
import { JobsRecommendationsApiService } from '../../../services/jobs-recommendations-api.service';
import { UserPreferencesApiService } from '../../../services/user-preferences-api.service';
import { ApplicationsService } from '../../../services/applications.service';
import { JobRawDto } from '../../../models/job.models';
import { isOnboardingComplete } from '../../../models/user/user-preferences.utils';
import { AuthService } from '../../../core/auth/auth.service';

type RecommendationRow = {
  job: JobRawDto;
  score?: number;
  reason?: string | null;
};

@Component({
  selector: 'app-jobs-page',
  standalone: true,
  imports: [CommonModule, RouterModule, JobsFiltersComponent, JobsTableComponent],
  templateUrl: './jobs-page.component.html',
  styleUrl: './jobs-page.component.scss'
})
export class JobsPageComponent implements OnInit {
  private readonly prefsApi = inject(UserPreferencesApiService);
  private readonly appService = inject(ApplicationsService);
  private readonly auth = inject(AuthService);
  
  readonly appliedJobs = this.appService.appliedJobs;
  readonly mode = signal<'recommended' | 'all'>('recommended');
  readonly isAdmin = computed(() => (this.auth.state()?.roles ?? []).includes('Admin'));

  readonly filters = signal<JobsFilterState>({
    query: '',
    location: 'Any',
    remoteOnly: false,
    minSalary: 0,
    maxSalary: 250000
  });

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly prefsWarning = signal<string | null>(null);

  private readonly allJobs = signal<UiJob[]>([]);

  readonly jobs = computed(() => {
    const f = this.filters();
    const q = f.query.trim().toLowerCase();

    return this.allJobs().filter((j) => {
      if (f.remoteOnly && !this.isRemoteLocation(j.location)) return false;
      if (f.location !== 'Any' && (j.location ?? '') !== f.location) return false;

      // Filter by minSalary and maxSalary if in 'all' mode
      if (this.mode() === 'all') {
        if (f.minSalary > 0 || f.maxSalary < 250000) {
          const salaryRange = this.parseSalary(j.salary);
          if (salaryRange.min !== undefined && salaryRange.min > f.maxSalary) {
            return false;
          }
          if (salaryRange.max !== undefined && salaryRange.max < f.minSalary) {
            return false;
          }
        }
      }

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

  private parseSalary(salaryStr?: string): { min?: number; max?: number } {
    if (!salaryStr) return {};

    const cleaned = salaryStr.toLowerCase()
      .replace(/,/g, '')
      .replace(/\$/g, '')
      .replace(/£/g, '')
      .replace(/€/g, '')
      .replace(/usd/g, '')
      .replace(/gbp/g, '')
      .replace(/eur/g, '');

    const matches = cleaned.match(/\b\d+(?:[.]\d+)?k?\b/g);
    if (!matches) return {};

    const values = matches.map(m => {
      let val = parseFloat(m);
      if (m.endsWith('k')) {
        val *= 1000;
      }
      return val;
    });

    const isHourly = salaryStr.toLowerCase().includes('hour') || salaryStr.toLowerCase().includes('hr') || salaryStr.toLowerCase().includes('/h');
    const isDaily = salaryStr.toLowerCase().includes('day') || salaryStr.toLowerCase().includes('/d');

    const multiplier = isHourly ? 2000 : (isDaily ? 260 : 1);

    if (values.length === 1) {
      let rate = values[0];
      if (multiplier === 1) {
        if (rate < 200) {
          rate *= 2000;
        } else if (rate < 2000) {
          rate *= 260;
        }
      } else {
        rate *= multiplier;
      }
      return { min: rate, max: rate };
    }

    if (values.length >= 2) {
      let min = values[0];
      let max = values[1];
      if (multiplier === 1) {
        if (min < 200) {
          min *= 2000;
          max *= 2000;
        } else if (min < 2000) {
          min *= 260;
          max *= 260;
        }
      } else {
        min *= multiplier;
        max *= multiplier;
      }
      return { min, max };
    }

    return {};
  }

  constructor(
    private readonly jobsApi: JobsApiService,
    private readonly recApi: JobsRecommendationsApiService
  ) {}

  ngOnInit() {
    if (this.isAdmin()) {
      this.mode.set('all');
      this.refresh();
      return;
    }

    this.prefsApi.get().subscribe({
      next: (prefs) => {
        if (!isOnboardingComplete(prefs)) {
          this.prefsWarning.set(
            'Add your job preferences to get personalized recommendations.'
          );
        }
        this.refresh();
      },
      error: () => this.refresh()
    });
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
    this.prefsWarning.set(null);

    if (this.mode() === 'recommended') {
      this.recApi.list({ take: 20 }).subscribe({
        next: (res) => {
          const rows: RecommendationRow[] = res.jobs ?? [];
          if (rows.length === 0) {
            this.prefsWarning.set(
              'No personalized matches yet. Showing all jobs instead.'
            );
            this.mode.set('all');
            this.loadAll();
            return;
          }

          this.allJobs.set(this.mapRecommendations(rows));
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

  private mapRecommendations(rows: RecommendationRow[]): UiJob[] {
    return rows.map((r) => {
      const base = this.map([r.job])[0];
      const score =
        typeof r.score === 'number' ? Math.round(r.score * 100) : undefined;
      const reason = r.reason?.trim();

      return {
        ...base,
        matchLabel:
          score !== undefined && reason
            ? `${score}% · ${reason}`
            : score !== undefined
              ? `${score}% match`
              : reason ?? undefined
      };
    });
  }

  private map(rows: JobRawDto[]): UiJob[] {
    return rows.map((r) => ({
      id: r.id,
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

  private isRemoteLocation(location?: string): boolean {
    const loc = (location ?? '').toLowerCase();
    return loc.includes('remote') || loc.includes('work from home') || loc === 'wfh';
  }
}
