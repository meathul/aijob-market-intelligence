import { TestBed } from '@angular/core/testing';

import { AuthService } from './auth.service';
import { AuthStorage } from './auth-storage';

describe('AuthService', () => {
  let service: AuthService;
  let storage: jasmine.SpyObj<AuthStorage>;

  beforeEach(() => {
    storage = jasmine.createSpyObj<AuthStorage>('AuthStorage', [
      'load',
      'save',
      'clear'
    ]);
    storage.load.and.returnValue(null);

    TestBed.configureTestingModule({
      providers: [AuthService, { provide: AuthStorage, useValue: storage }]
    });

    service = TestBed.inject(AuthService);
  });

  it('should start unauthenticated', () => {
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('should persist and expose token/roles', () => {
    service.setAuth({ token: 'abc', email: 'u@x.com', roles: ['User'] });

    expect(storage.save).toHaveBeenCalled();
    expect(service.token()).toBe('abc');
    expect(service.isAuthenticated()).toBeTrue();
    expect(service.state()?.roles).toEqual(['User']);
  });

  it('logout should clear storage and state', () => {
    service.setAuth({ token: 'abc' });
    service.logout();

    expect(storage.clear).toHaveBeenCalled();
    expect(service.isAuthenticated()).toBeFalse();
  });
});
