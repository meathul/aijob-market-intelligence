import { Component, EventEmitter, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';

export type JobsFilterState = {
  query: string;
  location: 'Any' | 'Remote' | 'US' | 'EU' | 'APAC';
  remoteOnly: boolean;
};

@Component({
  selector: 'app-jobs-filters',
  standalone: true,
  imports: [
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule
  ],
  templateUrl: './jobs-filters.component.html',
  styleUrl: './jobs-filters.component.scss'
})
export class JobsFiltersComponent {
  @Output() changed = new EventEmitter<JobsFilterState>();

  readonly state = signal<JobsFilterState>({
    query: '',
    location: 'Any',
    remoteOnly: true
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
}
