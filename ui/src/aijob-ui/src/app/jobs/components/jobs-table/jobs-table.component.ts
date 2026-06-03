import { CommonModule } from '@angular/common';
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
  url?: string;
  matchLabel?: string;
};

@Component({
  selector: 'app-jobs-table',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatChipsModule],
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

  openJob(row: UiJob) {
    const url = row.url?.trim();
    if (!url) return;

    // Use noopener for safety.
    window.open(url, '_blank', 'noopener,noreferrer');
  }
}
