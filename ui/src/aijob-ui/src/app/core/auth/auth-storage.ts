import { Injectable } from '@angular/core';

const KEY = 'aijob.auth';

export type AuthState = {
  token: string;
  email?: string;
  roles?: string[];
};

@Injectable({ providedIn: 'root' })
export class AuthStorage {
  load(): AuthState | null {
    try {
      const raw = localStorage.getItem(KEY);
      return raw ? (JSON.parse(raw) as AuthState) : null;
    } catch {
      return null;
    }
  }

  save(state: AuthState) {
    localStorage.setItem(KEY, JSON.stringify(state));
  }

  clear() {
    localStorage.removeItem(KEY);
  }
}
