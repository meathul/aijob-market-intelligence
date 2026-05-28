import { Injectable } from '@angular/core';

import { ApiClient } from '../core/http/api-client';

export type SkillCountDto = {
  skill?: string | null;
  count: number;
};

@Injectable({ providedIn: 'root' })
export class SkillsApiService {
  constructor(private readonly api: ApiClient) {}

  list() {
    return this.api.get<string[]>('/api/Skills');
  }

  top(take = 50) {
    return this.api.get<SkillCountDto[]>('/api/Skills/top', { take });
  }
}
