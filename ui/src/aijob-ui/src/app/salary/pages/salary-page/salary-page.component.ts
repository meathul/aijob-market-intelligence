import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { toSignal, toObservable } from '@angular/core/rxjs-interop';
import { forkJoin, switchMap, tap } from 'rxjs';
import { NgApexchartsModule } from 'ng-apexcharts';

import { AnalyticsApiService } from '../../../services/analytics-api.service';
import { UserPreferencesApiService } from '../../../services/user-preferences-api.service';
import { UserJobPreferencesDto } from '../../../models/user/user-preferences.models';

type RangeOption = { label: string; days: number };

@Component({
  selector: 'app-salary-page',
  standalone: true,
  imports: [CommonModule, FormsModule, NgApexchartsModule],
  templateUrl: './salary-page.component.html',
  styleUrl: './salary-page.component.scss'
})
export class SalaryPageComponent implements OnInit {
  private readonly analyticsApi = inject(AnalyticsApiService);
  private readonly prefsApi = inject(UserPreferencesApiService);

  readonly currencies = ['USD', 'EUR', 'GBP', 'CAD', 'AUD', 'INR'];
  readonly experienceLevels = ['Junior', 'Mid', 'Senior', 'Lead'];

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

  readonly loading = signal(false);
  readonly userPrefs = signal<UserJobPreferencesDto | null>(null);

  // Combine filters into a reactive stream
  private readonly filterParams$ = toObservable(
    computed(() => ({
      currency: this.currency(),
      location: this.location(),
      experienceLevel: this.experienceLevel(),
      days: this.range().days
    }))
  );

  // Single reactive data signal that fetches everything reactively in parallel
  readonly salaryData = toSignal(
    this.filterParams$.pipe(
      switchMap((p) => {
        this.loading.set(true);
        return forkJoin({
          stats: this.analyticsApi.salary({
            currency: p.currency,
            location: p.location || undefined,
            experienceLevel: p.experienceLevel || undefined,
            postedWithinDays: p.days
          }),
          remoteStats: this.analyticsApi.salary({
            currency: p.currency,
            location: 'Remote',
            postedWithinDays: p.days
          }),
          onSiteStats: this.analyticsApi.salary({
            currency: p.currency,
            location: 'Onsite',
            postedWithinDays: p.days
          }),
          benchmarks: forkJoin({
            junior: this.analyticsApi.salary({
              currency: p.currency,
              location: p.location || undefined,
              experienceLevel: 'Junior',
              postedWithinDays: p.days
            }),
            mid: this.analyticsApi.salary({
              currency: p.currency,
              location: p.location || undefined,
              experienceLevel: 'Mid',
              postedWithinDays: p.days
            }),
            senior: this.analyticsApi.salary({
              currency: p.currency,
              location: p.location || undefined,
              experienceLevel: 'Senior',
              postedWithinDays: p.days
            }),
            lead: this.analyticsApi.salary({
              currency: p.currency,
              location: p.location || undefined,
              experienceLevel: 'Lead',
              postedWithinDays: p.days
            })
          })
        }).pipe(
          tap({
            next: () => this.loading.set(false),
            error: () => this.loading.set(false)
          })
        );
      })
    ),
    { initialValue: null }
  );

  // Helpers to get direct stats from the combined data
  readonly stats = computed(() => this.salaryData()?.stats ?? null);
  readonly remoteStats = computed(() => this.salaryData()?.remoteStats ?? null);
  readonly onSiteStats = computed(() => this.salaryData()?.onSiteStats ?? null);

  readonly hasData = computed(() => !!this.stats()?.avg);

  // Visual slider position (clamped 0 to 100)
  readonly rangePct = computed(() => {
    const s = this.stats();
    if (!s || !s.min || !s.max || !s.avg) return 50;
    const range = s.max - s.min;
    if (range <= 0) return 50;
    const pct = ((s.avg - s.min) / range) * 100;
    return Math.min(Math.max(pct, 0), 100);
  });

  // Dynamically compute chart configuration based on loaded experience levels
  readonly chartOptions = computed(() => {
    const data = this.salaryData()?.benchmarks;
    const juniorVal = data?.junior?.avg ?? 0;
    const midVal = data?.mid?.avg ?? 0;
    const seniorVal = data?.senior?.avg ?? 0;
    const leadVal = data?.lead?.avg ?? 0;

    return {
      series: [
        {
          name: 'Average Salary',
          data: [juniorVal, midVal, seniorVal, leadVal]
        }
      ],
      chart: {
        type: 'bar' as const,
        height: 240,
        toolbar: { show: false },
        zoom: { enabled: false },
        foreColor: '#cbd5e1',
        fontFamily: 'Roboto, "Helvetica Neue", sans-serif'
      },
      colors: ['#6366f1'],
      plotOptions: {
        bar: {
          borderRadius: 4,
          horizontal: false,
          columnWidth: '50%'
        }
      },
      dataLabels: { enabled: false },
      xaxis: {
        categories: ['Junior', 'Mid', 'Senior', 'Lead'],
        axisBorder: { color: 'rgba(148, 163, 184, 0.2)' },
        axisTicks: { color: 'rgba(148, 163, 184, 0.2)' }
      },
      yaxis: {
        labels: {
          formatter: (v: number) => this.formatMoney(v)
        }
      },
      grid: {
        borderColor: 'rgba(148, 163, 184, 0.15)',
        strokeDashArray: 3
      },
      tooltip: { theme: 'dark' }
    };
  });

  ngOnInit() {
    this.prefsApi.get().subscribe({
      next: (prefs) => this.userPrefs.set(prefs),
      error: () => {}
    });
  }

  setRange(days: number) {
    const found = this.ranges.find((r) => r.days === days);
    if (found) this.range.set(found);
  }

  clearFilters() {
    this.location.set('');
    this.experienceLevel.set('');
  }

  formatMoney(v?: number | null) {
    if (v === null || v === undefined || isNaN(v)) return '—';
    return Math.round(v).toLocaleString();
  }

  getBenchmark(level: string) {
    const key = level.toLowerCase() as 'junior' | 'mid' | 'senior' | 'lead';
    return this.salaryData()?.benchmarks?.[key];
  }

  getAlignmentStatus() {
    const avg = this.stats()?.avg;
    const minPref = this.userPrefs()?.preferredSalaryMin;
    const maxPref = this.userPrefs()?.preferredSalaryMax;

    if (!avg) {
      return { status: 'unknown', label: 'No data to compare' };
    }

    if (minPref && avg < minPref) {
      return { status: 'below', label: 'Market Average is Below Target' };
    }

    if (maxPref && avg > maxPref) {
      return { status: 'above', label: 'Market Average exceeds Target' };
    }

    return { status: 'aligned', label: 'Aligned with your Target' };
  }
}
