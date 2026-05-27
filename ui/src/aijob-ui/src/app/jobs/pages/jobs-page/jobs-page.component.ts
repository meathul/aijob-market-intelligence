import { Component, computed, signal } from '@angular/core';

import {
  JobsFiltersComponent,
  JobsFilterState
} from '../../components/jobs-filters/jobs-filters.component';
import { JobsTableComponent, UiJob } from '../../components/jobs-table/jobs-table.component';
import { JobsApiService } from '../../../services/jobs-api.service';

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

  // Backend-loaded jobs (mapped to UiJob). Falls back to mock data if API is unavailable.
  private readonly allJobs = signal<UiJob[]>([
    {
      title: 'Senior Fullstack Engineer',
      company: 'Acme',
      location: 'Remote',
      posted: '1d',
      salary: '$140k–$180k',
      skills: ['TypeScript', 'Angular', 'Node.js']
    },
    {
      title: 'Data Engineer',
      company: 'Northwind',
      location: 'US',
      posted: '2d',
      salary: '$120k–$160k',
      skills: ['Python', 'Spark', 'AWS']
    },
    {
      title: 'ML Engineer',
      company: 'Contoso',
      location: 'EU',
      posted: '3d',
      salary: '$150k–$210k',
      skills: ['PyTorch', 'LLMs', 'Kubernetes']
    }
  ]);

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
    this.jobsApi.list().subscribe({
      next: (rows) => {
        const mapped: UiJob[] = (rows ?? []).map((r) => ({
          title: r.title,
          company: r.company ?? r.source ?? '—',
          location: r.location ?? '—',
          posted: r.publishedAt ? new Date(r.publishedAt).toLocaleDateString() : '—',
          salary:
            r.minSalary || r.maxSalary
              ? `${r.salaryCurrency ?? ''} ${r.minSalary ?? ''}–${r.maxSalary ?? ''} ${r.salaryPeriod ?? ''}`.trim()
              : undefined,
          skills: r.skills ?? []
        }));

        if (mapped.length) this.allJobs.set(mapped);
      },
      error: () => {
        // Keep mock data.
      }
    });
  }
}
