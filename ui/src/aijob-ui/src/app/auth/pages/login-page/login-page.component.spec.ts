import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { LoginPageComponent } from './login-page.component';
import { AuthApiService } from '../../../services/auth-api.service';
import { AuthService } from '../../../core/auth/auth.service';

describe('LoginPageComponent', () => {
  it('should show error when admin mode but roles do not include Admin', async () => {
    const authApi = {
      login: () => of({ accessToken: 't', roles: ['User'], email: 'u@x.com' })
    } as Partial<AuthApiService>;

    const auth = {
      setAuth: jasmine.createSpy('setAuth')
    } as unknown as AuthService;

    const router = {
      navigateByUrl: jasmine.createSpy('navigateByUrl').and.resolveTo(true)
    } as unknown as Router;

    await TestBed.configureTestingModule({
      imports: [LoginPageComponent],
      providers: [
        { provide: AuthApiService, useValue: authApi },
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(LoginPageComponent);
    fixture.componentInstance.mode.set('admin');
    fixture.componentInstance.email.set('u@x.com');
    fixture.componentInstance.password.set('p');

    await fixture.componentInstance.submit();

    expect(fixture.componentInstance.error()).toContain('not an admin');
    expect((auth as any).setAuth).not.toHaveBeenCalled();
    expect((router as any).navigateByUrl).not.toHaveBeenCalled();
  });

  it('should set auth and navigate on success', async () => {
    const authApi = {
      login: () => of({ accessToken: 't', roles: ['User'], email: 'u@x.com' })
    } as Partial<AuthApiService>;

    const auth = {
      setAuth: jasmine.createSpy('setAuth')
    } as unknown as AuthService;

    const router = {
      navigateByUrl: jasmine.createSpy('navigateByUrl').and.resolveTo(true)
    } as unknown as Router;

    await TestBed.configureTestingModule({
      imports: [LoginPageComponent],
      providers: [
        { provide: AuthApiService, useValue: authApi },
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(LoginPageComponent);
    fixture.componentInstance.mode.set('user');
    fixture.componentInstance.email.set('u@x.com');
    fixture.componentInstance.password.set('p');

    await fixture.componentInstance.submit();

    expect((auth as any).setAuth).toHaveBeenCalledWith({
      token: 't',
      email: 'u@x.com',
      roles: ['User']
    });
    expect((router as any).navigateByUrl).toHaveBeenCalledWith('/dashboard');
  });

  it('should surface api errors', async () => {
    const authApi = {
      login: () => throwError(() => new Error('bad credentials'))
    } as Partial<AuthApiService>;

    const auth = {
      setAuth: jasmine.createSpy('setAuth')
    } as unknown as AuthService;

    const router = {
      navigateByUrl: jasmine.createSpy('navigateByUrl').and.resolveTo(true)
    } as unknown as Router;

    await TestBed.configureTestingModule({
      imports: [LoginPageComponent],
      providers: [
        { provide: AuthApiService, useValue: authApi },
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(LoginPageComponent);
    fixture.componentInstance.email.set('u@x.com');
    fixture.componentInstance.password.set('p');

    await fixture.componentInstance.submit();

    expect(fixture.componentInstance.error()).toContain('bad credentials');
    expect((auth as any).setAuth).not.toHaveBeenCalled();
  });
});
