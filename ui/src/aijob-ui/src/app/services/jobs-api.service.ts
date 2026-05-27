import { Injectable } from '@angular/core';

import { ApiClient } from '../core/http/api-client';
import { JobDto } from '../models/job.models';

@Injectable({ providedIn: 'root' })
export class JobsApiService {
  constructor(private readonly api: ApiClient) {}

  // NOTE: adjust params to match your backend contract.
  list(params?: { page?: number; pageSize?: number; query?: string }) {
    return this.api.get<JobDto[]>('/api/jobs', params);
  }
}
