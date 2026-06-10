import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

export type JobsFilterState = {
  query: string;
  location: 'Any' | 'Remote' | 'US' | 'EU' | 'APAC';
  remoteOnly: boolean;
  minSalary: number;
  maxSalary: number;
};

@Component({
  selector: 'app-jobs-filters',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './jobs-filters.component.html',
  styleUrl: './jobs-filters.component.scss'
})
export class JobsFiltersComponent {
  @Input() mode: 'recommended' | 'all' = 'recommended';
  @Output() changed = new EventEmitter<JobsFilterState>();

  readonly state = signal<JobsFilterState>({
    query: '',
    location: 'Any',
    remoteOnly: false,
    minSalary: 0,
    maxSalary: 250000
  });

  readonly locations: JobsFilterState['location'][] = [
    'Any',
    'Remote',
    'US',
    'EU',
    'APAC'
  ];

  onQueryChange(query: string) {
    this.state.update((prev) => ({ ...prev, query }));
    this.changed.emit(this.state());
  }

  onLocationChange(location: JobsFilterState['location']) {
    this.state.update((prev) => ({ ...prev, location }));
    this.changed.emit(this.state());
  }

  onRemoteOnlyChange(remoteOnly: boolean) {
    this.state.update((prev) => ({ ...prev, remoteOnly }));
    this.changed.emit(this.state());
  }

  onMinSalaryChange(val: string | number) {
    let minSalary = Number(val);
    if (minSalary > this.state().maxSalary) {
      minSalary = this.state().maxSalary;
    }
    this.state.update((prev) => ({ ...prev, minSalary }));
    this.changed.emit(this.state());
  }

  onMaxSalaryChange(val: string | number) {
    let maxSalary = Number(val);
    if (maxSalary < this.state().minSalary) {
      maxSalary = this.state().minSalary;
    }
    this.state.update((prev) => ({ ...prev, maxSalary }));
    this.changed.emit(this.state());
  }
}
