import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController
} from '@angular/common/http/testing';

import { AuthApiService } from './auth-api.service';
import { environment } from '../../environments/environment';

describe('AuthApiService', () => {
  let service: AuthApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });

    (environment as any).apiBaseUrl = 'http://localhost:5062';

    service = TestBed.inject(AuthApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should POST to /api/Auth/register', () => {
    service
      .register({ email: 'u@example.com', password: 'Test123!' })
      .subscribe();

    const req = httpMock.expectOne('http://localhost:5062/api/Auth/register');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      email: 'u@example.com',
      password: 'Test123!'
    });

    req.flush({ accessToken: 't', roles: ['User'] });
  });

  it('should POST to /api/Auth/login', () => {
    service.login({ email: 'u@example.com', password: 'Test123!' }).subscribe();

    const req = httpMock.expectOne('http://localhost:5062/api/Auth/login');
    expect(req.request.method).toBe('POST');
    req.flush({ accessToken: 't', roles: ['User'] });
  });
});
