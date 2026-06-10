import { Injectable } from '@angular/core';
import { ApiClient } from '../core/http/api-client';

export interface TriggerFetchResponse {
  success: boolean;
  message: string;
  jobsAdded: number;
  timestamp: string;
}

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  constructor(private readonly api: ApiClient) {}

  triggerFetch() {
    return this.api.post<TriggerFetchResponse>('/api/admin/trigger-fetch', {});
  }
}
