import { Injectable } from '@angular/core';

import { ApiClient } from '../core/http/api-client';

export type JobRecommendationDto = {
  job: any; // reuse existing JobRawDto shape on the UI side
  score: number;
  reason?: string | null;
};

export type JobRecommendationsResultDto = {
  jobs: JobRecommendationDto[];
};

@Injectable({ providedIn: 'root' })
export class JobsRecommendationsApiService {
  constructor(private readonly api: ApiClient) {}

  list(params?: { take?: number }) {
    return this.api.get<JobRecommendationsResultDto>('/api/JobsRecommendations', params);
  }
}
