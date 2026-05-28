import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';

import { AnalyticsApiService } from '../../../services/analytics-api.service';

type RangeOption = { label: string; days: number };

@Component({
  selector: 'app-salary-page',
  standalone: true,
  imports: [],
  templateUrl: './salary-page.component.html',
  styleUrl: './salary-page.component.scss'
})
export class SalaryPageComponent {
  private readonly analyticsApi = inject(AnalyticsApiService);

  // Filters
  readonly currency = signal<string>('USD');
  readonly location = signal<string>('');
  readonly experienceLevel = signal<string>('');
  readonly range = signal<RangeOption>({ label: 'Last 12 months', days: 365 });

  readonly ranges: RangeOption[] = [
    { label: 'Last 30 days', days: 30 },
    { label: 'Last 90 days', days: 90 },
    { label: 'Last 12 months', days: 365 },
    { label: 'All time', days: 3650 }
  ];

  // Main stats for current filters
  readonly stats = toSignal(
    this.analyticsApi.salary({
      currency: this.currency(),
      location: this.location() || undefined,
      experienceLevel: this.experienceLevel() || undefined,
      postedWithinDays: this.range().days
    }),
    { initialValue: null }
  );

  // Comparison: Remote vs non-remote (best-effort; depends on your location naming)
  readonly remoteStats = toSignal(
    this.analyticsApi.salary({
      currency: this.currency(),
      location: 'Remote',
      postedWithinDays: this.range().days
    }),
    { initialValue: null }
  );

  readonly onSiteStats = toSignal(
    this.analyticsApi.salary({
      currency: this.currency(),
      location: 'Onsite',
      postedWithinDays: this.range().days
    }),
    { initialValue: null }
  );

  readonly hasData = computed(() => !!this.stats());

  setRange(days: number) {
    const found = this.ranges.find((r) => r.days === days);
    if (found) this.range.set(found);
  }

  clearFilters() {
    this.location.set('');
    this.experienceLevel.set('');
  }

  formatMoney(v?: number | null) {
    if (v === null || v === undefined) return '—';
    return Math.round(v).toLocaleString();
  }
}
