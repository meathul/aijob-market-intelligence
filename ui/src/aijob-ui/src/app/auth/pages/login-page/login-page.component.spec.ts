import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';

import { LoginPageComponent } from './login-page.component';
import { AuthApiService } from '../../../services/auth-api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { UserPreferencesApiService } from '../../../services/user-preferences-api.service';

describe('LoginPageComponent', () => {
  async function setup(
    authApi: Partial<AuthApiService>,
    prefs: Record<string, unknown> | null = null
  ) {
    const auth = {
      setAuth: jasmine.createSpy('setAuth')
    } as unknown as AuthService;

    const router = {
      navigateByUrl: jasmine.createSpy('navigateByUrl').and.resolveTo(true)
    } as unknown as Router;

    await TestBed.configureTestingModule({
      imports: [LoginPageComponent, RouterTestingModule],
      providers: [
        { provide: AuthApiService, useValue: authApi },
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
        {
          provide: UserPreferencesApiService,
          useValue: { get: () => of(prefs) }
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(LoginPageComponent);
    return { fixture, auth, router };
  }

  it('should show error when admin mode but roles do not include Admin', async () => {
    const { fixture, auth, router } = await setup({
      login: () => of({ accessToken: 't', roles: ['User'], email: 'u@x.com' })
    });

    fixture.componentInstance.mode.set('admin');
    fixture.componentInstance.email.set('u@x.com');
    fixture.componentInstance.password.set('p');

    await fixture.componentInstance.submit();

    expect(fixture.componentInstance.error()).toContain('not an admin');
    expect((auth as any).setAuth).not.toHaveBeenCalled();
    expect((router as any).navigateByUrl).not.toHaveBeenCalled();
  });

  it('should navigate to onboarding when onboarding is not complete', async () => {
    const { fixture, auth, router } = await setup(
      {
        login: () => of({ accessToken: 't', roles: ['User'], email: 'u@x.com' })
      },
      { onboardingCompleted: false, preferredJobTitle: 'Engineer' }
    );

    fixture.componentInstance.mode.set('user');
    fixture.componentInstance.email.set('u@x.com');
    fixture.componentInstance.password.set('p');

    await fixture.componentInstance.submit();

    expect((auth as any).setAuth).toHaveBeenCalled();
    expect((router as any).navigateByUrl).toHaveBeenCalledWith('/onboarding');
  });

  it('should navigate to dashboard when onboarding is complete', async () => {
    const { fixture, auth, router } = await setup(
      {
        login: () => of({ accessToken: 't', roles: ['User'], email: 'u@x.com' })
      },
      { onboardingCompleted: true }
    );

    fixture.componentInstance.mode.set('user');
    fixture.componentInstance.email.set('u@x.com');
    fixture.componentInstance.password.set('p');

    await fixture.componentInstance.submit();

    expect((auth as any).setAuth).toHaveBeenCalled();
    expect((router as any).navigateByUrl).toHaveBeenCalledWith('/dashboard');
  });

  it('should surface api errors', async () => {
    const { fixture, auth } = await setup({
      login: () => throwError(() => new Error('bad credentials'))
    });

    fixture.componentInstance.email.set('u@x.com');
    fixture.componentInstance.password.set('p');

    await fixture.componentInstance.submit();

    expect(fixture.componentInstance.error()).toContain('bad credentials');
    expect((auth as any).setAuth).not.toHaveBeenCalled();
  });
});
