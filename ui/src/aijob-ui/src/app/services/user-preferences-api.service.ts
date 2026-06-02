import { Injectable } from '@angular/core';

import { ApiClient } from '../core/http/api-client';
import { UserJobPreferencesDto } from '../models/user/user-preferences.models';

@Injectable({ providedIn: 'root' })
export class UserPreferencesApiService {
  constructor(private readonly api: ApiClient) {}

  get() {
    return this.api.get<UserJobPreferencesDto>('/api/user/preferences');
  }

  upsert(body: UserJobPreferencesDto) {
    return this.api.put<UserJobPreferencesDto>('/api/user/preferences', body);
  }
}
