import { Injectable } from '@angular/core';

import { ApiClient } from '../core/http/api-client';
import {
  KeyValueCountDto,
  SalaryStatsDto,
  TimeSeriesPointDto
} from '../models/analytics.models';

@Injectable({ providedIn: 'root' })
export class AnalyticsApiService {
  constructor(private readonly api: ApiClient) {}

  ingestionDaily(days = 30) {
    return this.api.get<TimeSeriesPointDto[]>('/api/Analytics/ingestion/daily', {
      days
    });
  }

  breakdownSource(take = 10) {
    return this.api.get<KeyValueCountDto[]>('/api/Analytics/breakdown/source', {
      take
    });
  }

  breakdownLocation(take = 10) {
    return this.api.get<KeyValueCountDto[]>('/api/Analytics/breakdown/location', {
      take
    });
  }

  breakdownExperience(take = 10) {
    return this.api.get<KeyValueCountDto[]>('/api/Analytics/breakdown/experience', {
      take
    });
  }

  salary(params?: {
    currency?: string;
    location?: string;
    experienceLevel?: string;
    postedWithinDays?: number;
  }) {
    return this.api.get<SalaryStatsDto>('/api/Analytics/salary', params);
  }
}
