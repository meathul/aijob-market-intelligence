import { Component, Input } from '@angular/core';

import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';

export type UiJob = {
  title: string;
  company: string;
  location: string;
  posted: string;
  salary?: string;
  skills?: string[];
};

@Component({
  selector: 'app-jobs-table',
  standalone: true,
  imports: [MatTableModule, MatChipsModule],
  templateUrl: './jobs-table.component.html',
  styleUrl: './jobs-table.component.scss'
})
export class JobsTableComponent {
  @Input({ required: true }) jobs: UiJob[] = [];

  readonly displayedColumns: Array<keyof UiJob | 'skills'> = [
    'title',
    'company',
    'location',
    'posted',
    'salary',
    'skills'
  ];
}
