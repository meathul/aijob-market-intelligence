import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';

import { AnalyticsApiService } from '../../../services/analytics-api.service';

@Component({
  selector: 'app-reports-page',
  standalone: true,
  imports: [],
  templateUrl: './reports-page.component.html',
  styleUrl: './reports-page.component.scss'
})
export class ReportsPageComponent {
  private readonly analyticsApi = inject(AnalyticsApiService);

  readonly sources = toSignal(this.analyticsApi.breakdownSource(10), {
    initialValue: []
  });

  readonly locations = toSignal(this.analyticsApi.breakdownLocation(10), {
    initialValue: []
  });

  readonly experience = toSignal(this.analyticsApi.breakdownExperience(10), {
    initialValue: []
  });
}
