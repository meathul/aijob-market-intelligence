import { Injectable, computed, signal } from '@angular/core';

import { AuthState, AuthStorage } from './auth-storage';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _state = signal<AuthState | null>(null);

  readonly state = this._state.asReadonly();
  readonly token = computed(() => this._state()?.token ?? null);
  readonly isAuthenticated = computed(() => !!this.token());

  constructor(private readonly storage: AuthStorage) {
    this._state.set(this.storage.load());
  }

  setAuth(state: AuthState) {
    this.storage.save(state);
    this._state.set(state);
  }

  logout() {
    this.storage.clear();
    this._state.set(null);
  }
}
