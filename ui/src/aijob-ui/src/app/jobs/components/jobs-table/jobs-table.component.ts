import { CommonModule } from '@angular/common';
import { Component, Input, inject, computed } from '@angular/core';

import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { ApplicationsService } from '../../../services/applications.service';
import { AuthService } from '../../../core/auth/auth.service';

export type UiJob = {
  id: number;
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
  private readonly appService = inject(ApplicationsService);
  private readonly auth = inject(AuthService);

  @Input({ required: true }) jobs: UiJob[] = [];

  readonly isAdmin = computed(() => (this.auth.state()?.roles ?? []).includes('Admin'));

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

  apply(job: UiJob) {
    this.appService.apply(job);
  }

  unapply(jobId: number) {
    this.appService.unapply(jobId);
  }

  isApplied(jobId: number): boolean {
    return this.appService.isApplied(jobId);
  }
}
