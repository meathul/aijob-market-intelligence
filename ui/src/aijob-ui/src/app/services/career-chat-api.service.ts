import { Injectable } from '@angular/core';

import { ApiClient } from '../core/http/api-client';

export type CareerChatRequest = {
  message: string;
};

export type CareerChatResponse = {
  answer: string;
};

@Injectable({ providedIn: 'root' })
export class CareerChatApiService {
  constructor(private readonly api: ApiClient) {}

  ask(body: CareerChatRequest) {
    return this.api.post<CareerChatResponse>('/api/CareerChat', body);
  }
}
