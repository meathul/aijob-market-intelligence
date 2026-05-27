import { Injectable } from '@angular/core';

import { ApiClient } from '../core/http/api-client';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  constructor(private readonly api: ApiClient) {}

  register(req: RegisterRequest) {
    return this.api.post<AuthResponse>('/api/auth/register', req);
  }

  login(req: LoginRequest) {
    return this.api.post<AuthResponse>('/api/auth/login', req);
  }
}
