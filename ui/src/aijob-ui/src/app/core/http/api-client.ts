import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);

  get<T>(path: string, params?: Record<string, string | number | boolean | undefined | null>) {
    return this.http.get<T>(this.url(path), {
      params: this.toParams(params)
    });
  }

  post<T>(path: string, body: unknown) {
    return this.http.post<T>(this.url(path), body);
  }

  private url(path: string) {
    const base = environment.apiBaseUrl?.replace(/\/$/, '') ?? '';
    const p = path.startsWith('/') ? path : `/${path}`;
    return `${base}${p}`;
  }

  private toParams(params?: Record<string, string | number | boolean | undefined | null>) {
    if (!params) return undefined;
    let p = new HttpParams();
    for (const [k, v] of Object.entries(params)) {
      if (v === undefined || v === null) continue;
      p = p.set(k, String(v));
    }
    return p;
  }
}
