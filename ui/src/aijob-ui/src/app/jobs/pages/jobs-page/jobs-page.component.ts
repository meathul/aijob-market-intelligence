import { Component, signal } from '@angular/core';

import { JobsFiltersComponent, JobsFilterState } from '../../components/jobs-filters/jobs-filters.component';
import { JobsTableComponent, UiJob } from '../../components/jobs-table/jobs-table.component';

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

  readonly jobs = signal<UiJob[]>([
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

  onFiltersChanged(next: JobsFilterState) {
    this.filters.set(next);
  }
}
