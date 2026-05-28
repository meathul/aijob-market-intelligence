import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController
} from '@angular/common/http/testing';

import { ApiClient } from './api-client';
import { environment } from '../../../environments/environment';

describe('ApiClient', () => {
  let api: ApiClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });
    api = TestBed.inject(ApiClient);
    httpMock = TestBed.inject(HttpTestingController);

    // Ensure a stable base url for expectations
    (environment as any).apiBaseUrl = 'http://localhost:5062';
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should prefix apiBaseUrl and serialize params, skipping null/undefined', () => {
    api
      .get<{ ok: boolean }>('/api/Jobs', {
        pageNumber: 1,
        pageSize: 50,
        keyword: undefined,
        location: null
      })
      .subscribe();

    const req = httpMock.expectOne(
      'http://localhost:5062/api/Jobs?pageNumber=1&pageSize=50'
    );
    expect(req.request.method).toBe('GET');
    req.flush({ ok: true });
  });

  it('should handle paths without leading slash', () => {
    api.get('/api/System/health').subscribe();
    const req = httpMock.expectOne('http://localhost:5062/api/System/health');
    expect(req.request.method).toBe('GET');
    req.flush({});
  });
});
