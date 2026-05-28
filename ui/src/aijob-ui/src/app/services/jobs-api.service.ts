import { Injectable } from '@angular/core';

import { ApiClient } from '../core/http/api-client';
import { JobSearchResultDto } from '../models/job.models';

@Injectable({ providedIn: 'root' })
export class JobsApiService {
  constructor(private readonly api: ApiClient) {}

  list(params?: { pageNumber?: number; pageSize?: number }) {
    return this.api.get<JobSearchResultDto>('/api/Jobs', params);
  }

  processed(params?: { pageNumber?: number; pageSize?: number }) {
    return this.api.get<JobSearchResultDto>('/api/Jobs/processed', params);
  }

  search(params: {
    keyword?: string;
    location?: string;
    pageNumber?: number;
    pageSize?: number;
  }) {
    return this.api.get<JobSearchResultDto>('/api/Jobs/search', params);
  }

  getById(id: number) {
    return this.api.get(`/api/Jobs/${id}`);
  }
}
