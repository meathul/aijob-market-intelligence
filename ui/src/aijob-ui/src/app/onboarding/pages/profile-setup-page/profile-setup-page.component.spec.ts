import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { ProfileSetupPageComponent } from './profile-setup-page.component';
import { UserPreferencesApiService } from '../../../services/user-preferences-api.service';

describe('ProfileSetupPageComponent', () => {
  async function setup(
    prefsApiVal: Partial<UserPreferencesApiService>
  ) {
    const router = {
      navigateByUrl: jasmine.createSpy('navigateByUrl').and.resolveTo(true)
    } as unknown as Router;

    await TestBed.configureTestingModule({
      imports: [ProfileSetupPageComponent, RouterTestingModule],
      providers: [
        { provide: UserPreferencesApiService, useValue: prefsApiVal },
        { provide: Router, useValue: router }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(ProfileSetupPageComponent);
    return { fixture, router };
  }

  it('should initialize and load existing user preferences', async () => {
    const mockPrefs = {
      location: 'Denver',
      preferredJobTitle: 'Developer',
      preferredSalaryMin: 90000,
      preferredSalaryMax: 150000,
      workMode: 'Hybrid',
      skillsText: 'TypeScript, Angular',
      onboardingCompleted: true
    };

    const { fixture } = await setup({
      get: () => of(mockPrefs)
    });

    fixture.detectChanges(); // triggers ngOnInit

    expect(fixture.componentInstance.loading()).toBeFalse();
    expect(fixture.componentInstance.isExistingUser()).toBeTrue();
    expect(fixture.componentInstance.location()).toBe('Denver');
    expect(fixture.componentInstance.preferredJobTitle()).toBe('Developer');
    expect(fixture.componentInstance.preferredSalaryMin()).toBe(90000);
    expect(fixture.componentInstance.preferredSalaryMax()).toBe(150000);
    expect(fixture.componentInstance.workMode()).toBe('Hybrid');
    expect(fixture.componentInstance.skillsText()).toBe('TypeScript, Angular');
  });

  it('should invalidate form if no meaningful preferences are set', async () => {
    const { fixture } = await setup({
      get: () => of(null)
    });

    fixture.detectChanges();

    // Default is empty location, title, etc. workMode is 'Any'
    expect(fixture.componentInstance.canSubmit()).toBeFalse();

    // Provide a salary min only
    fixture.componentInstance.preferredSalaryMin.set(60000);
    expect(fixture.componentInstance.canSubmit()).toBeTrue();
  });

  it('should submit preferences and navigate to dashboard on success', async () => {
    const upsertSpy = jasmine.createSpy('upsert').and.returnValue(of({ onboardingCompleted: true }));
    const { fixture, router } = await setup({
      get: () => of(null),
      upsert: upsertSpy
    });

    fixture.detectChanges();

    // Set valid preferences
    fixture.componentInstance.location.set('Dallas');
    fixture.componentInstance.preferredJobTitle.set('Architect');
    fixture.componentInstance.preferredSalaryMin.set(120000);
    fixture.componentInstance.preferredSalaryMax.set(160000);
    fixture.componentInstance.workMode.set('Remote');

    expect(fixture.componentInstance.canSubmit()).toBeTrue();

    await fixture.componentInstance.submit();

    expect(upsertSpy).toHaveBeenCalledWith({
      location: 'Dallas',
      preferredJobTitle: 'Architect',
      preferredSalaryMin: 120000,
      preferredSalaryMax: 160000,
      workMode: 'Remote',
      skillsText: ''
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/dashboard');
  });

  it('should set error message if upsert fails', async () => {
    const { fixture } = await setup({
      get: () => of(null),
      upsert: () => throwError(() => new Error('Save failed'))
    });

    fixture.detectChanges();
    fixture.componentInstance.location.set('Austin'); // make form valid

    await fixture.componentInstance.submit();

    expect(fixture.componentInstance.error()).toBe('Failed to save preferences.');
  });
});
