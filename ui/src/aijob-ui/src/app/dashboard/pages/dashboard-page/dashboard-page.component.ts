import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import {
  ApexAxisChartSeries,
  ApexChart,
  ApexDataLabels,
  ApexFill,
  ApexGrid,
  ApexStroke,
  ApexTooltip,
  ApexXAxis,
  ApexYAxis,
  NgApexchartsModule
} from 'ng-apexcharts';

import { AuthService } from '../../../core/auth/auth.service';
import { JobRawDto } from '../../../models/job.models';
import { UserJobPreferencesDto } from '../../../models/user/user-preferences.models';
import { isOnboardingComplete } from '../../../models/user/user-preferences.utils';
import { JobsRecommendationsApiService } from '../../../services/jobs-recommendations-api.service';
import { UserPreferencesApiService } from '../../../services/user-preferences-api.service';

export type JobsTrendChartOptions = {
  series: ApexAxisChartSeries;
  chart: ApexChart;
  xaxis: ApexXAxis;
  yaxis: ApexYAxis;
  grid: ApexGrid;
  stroke: ApexStroke;
  dataLabels: ApexDataLabels;
  fill: ApexFill;
  tooltip: ApexTooltip;
  colors: string[];
};

type RecommendedJob = {
  title: string;
  company: string;
  location: string;
  posted: string;
  score?: number;
  reason?: string | null;
};

type RecommendationRow = {
  job: JobRawDto;
  score?: number;
  reason?: string | null;
};

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, RouterLink, NgApexchartsModule],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss'
})
export class DashboardPageComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly prefsApi = inject(UserPreferencesApiService);
  private readonly recApi = inject(JobsRecommendationsApiService);

  readonly isAdmin = computed(() => (this.auth.state()?.roles ?? []).includes('Admin'));
  readonly userEmail = computed(() => this.auth.state()?.email ?? 'Your profile');

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly prefs = signal<UserJobPreferencesDto | null>(null);
  readonly recommendedJobs = signal<RecommendedJob[]>([]);

  readonly profileComplete = computed(() => isOnboardingComplete(this.prefs()));
  readonly skills = computed(() =>
    (this.prefs()?.skillsText ?? '')
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean)
      .slice(0, 6)
  );

  readonly userKpis = computed(() => {
    const prefs = this.prefs();
    const jobs = this.recommendedJobs();

    return [
      {
        label: 'Recommended matches',
        value: jobs.length.toString(),
        detail: jobs.length ? 'Based on your profile' : 'No matches loaded yet'
      },
      {
        label: 'Profile status',
        value: this.profileComplete() ? 'Complete' : 'Needs setup',
        detail: this.profileComplete() ? 'Preferences are saved' : 'Add preferences for better matches'
      },
      {
        label: 'Preferred role',
        value: prefs?.preferredJobTitle?.trim() || 'Any role',
        detail: 'Used for matching'
      },
      {
        label: 'Work mode',
        value: prefs?.workMode || 'Any',
        detail: prefs?.location?.trim() || 'No location preference'
      }
    ];
  });

  readonly kpis = [
    { label: 'New jobs (7d)', value: '1,284', delta: '+12%' },
    { label: 'Active sources', value: '3', delta: 'Stable' },
    { label: 'Top skill', value: 'TypeScript', delta: '+5%' },
    { label: 'Median salary', value: '$118k', delta: '+2%' }
  ];

  // Mock series (daily counts)
  readonly chartOptions: JobsTrendChartOptions = {
    series: [
      {
        name: 'Jobs',
        data: [120, 132, 98, 154, 166, 142, 176, 190, 172, 210, 198, 222]
      }
    ],
    chart: {
      type: 'area',
      height: 260,
      toolbar: { show: false },
      zoom: { enabled: false },
      foreColor: '#cbd5e1',
      fontFamily: 'Roboto, "Helvetica Neue", sans-serif'
    },
    colors: ['#6366f1'],
    dataLabels: { enabled: false },
    stroke: { curve: 'smooth', width: 2 },
    fill: {
      type: 'gradient',
      gradient: {
        opacityFrom: 0.35,
        opacityTo: 0.05,
        stops: [0, 90, 100]
      }
    },
    grid: {
      borderColor: 'rgba(148, 163, 184, 0.15)',
      strokeDashArray: 3
    },
    xaxis: {
      categories: [
        'W1',
        'W2',
        'W3',
        'W4',
        'W5',
        'W6',
        'W7',
        'W8',
        'W9',
        'W10',
        'W11',
        'W12'
      ],
      axisBorder: { color: 'rgba(148, 163, 184, 0.2)' },
      axisTicks: { color: 'rgba(148, 163, 184, 0.2)' }
    },
    yaxis: {
      labels: {
        formatter: (v) => Math.round(v).toString()
      }
    },
    tooltip: { theme: 'dark' }
  };

  ngOnInit() {
    if (this.isAdmin()) return;

    this.loading.set(true);
    this.error.set(null);

    this.prefsApi.get().subscribe({
      next: (prefs) => this.prefs.set(prefs),
      error: (e) => {
        this.error.set('Could not load your saved preferences.');
        // eslint-disable-next-line no-console
        console.error(e);
      }
    });

    this.recApi.list({ take: 5 }).subscribe({
      next: (res) => {
        const rows: RecommendationRow[] = res.jobs ?? [];
        this.recommendedJobs.set(rows.map((r) => this.mapRecommendation(r)));
        this.loading.set(false);
      },
      error: (e) => {
        this.loading.set(false);
        this.error.set('Could not load your recommended jobs.');
        // eslint-disable-next-line no-console
        console.error(e);
      }
    });
  }

  private mapRecommendation(row: RecommendationRow): RecommendedJob {
    const job = row.job;

    return {
      title: job.title ?? '—',
      company: job.company ?? job.source ?? '—',
      location: job.location ?? '—',
      posted: job.postedDate ? new Date(job.postedDate).toLocaleDateString() : '—',
      score: typeof row.score === 'number' ? Math.round(row.score * 100) : undefined,
      reason: row.reason ?? null
    };
  }
}
