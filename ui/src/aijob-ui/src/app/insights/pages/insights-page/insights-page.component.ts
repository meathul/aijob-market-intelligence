import { Component, computed, signal, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';

import { AnalyticsApiService } from '../../../services/analytics-api.service';

@Component({
  selector: 'app-insights-page',
  standalone: true,
  imports: [],
  templateUrl: './insights-page.component.html',
  styleUrl: './insights-page.component.scss'
})
export class InsightsPageComponent {
  private readonly analyticsApi = inject(AnalyticsApiService);

  readonly days = signal(30);
  readonly points = toSignal(this.analyticsApi.ingestionDaily(this.days()), {
    initialValue: []
  });

  readonly total = computed(() =>
    this.points().reduce((sum, p) => sum + (p.count ?? 0), 0)
  );
}
