import { TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { of, signal } from 'rxjs';
import { JobsPageComponent } from './jobs-page.component';
import { JobsApiService } from '../../../services/jobs-api.service';
import { JobsRecommendationsApiService } from '../../../services/jobs-recommendations-api.service';
import { UserPreferencesApiService } from '../../../services/user-preferences-api.service';
import { ApplicationsService } from '../../../services/applications.service';
import { AuthService } from '../../../core/auth/auth.service';

describe('JobsPageComponent', () => {
  async function setup(
    roles: string[],
    jobsVal: any[] = [],
    recVal: any[] = [],
    prefsVal: any = null
  ) {
    const jobsApiMock = {
      list: jasmine.createSpy('list').and.returnValue(of({ jobs: jobsVal }))
    };

    const recApiMock = {
      list: jasmine.createSpy('list').and.returnValue(of({ jobs: recVal }))
    };

    const prefsApiMock = {
      get: jasmine.createSpy('get').and.returnValue(of(prefsVal))
    };

    const appServiceMock = {
      appliedJobs: signal([])
    };

    const authMock = {
      state: signal({ roles, email: 'user@test.com' })
    };

    await TestBed.configureTestingModule({
      imports: [JobsPageComponent, RouterTestingModule],
      providers: [
        { provide: JobsApiService, useValue: jobsApiMock },
        { provide: JobsRecommendationsApiService, useValue: recApiMock },
        { provide: UserPreferencesApiService, useValue: prefsApiMock },
        { provide: ApplicationsService, useValue: appServiceMock },
        { provide: AuthService, useValue: authMock }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(JobsPageComponent);
    return { fixture, jobsApiMock, recApiMock, prefsApiMock };
  }

  it('should initialize in all-jobs mode for Admin role and skip onboarding check', async () => {
    const { fixture, prefsApiMock, jobsApiMock } = await setup(
      ['Admin'],
      [{ id: 1, title: 'Admin Job', company: 'Company' }]
    );

    fixture.detectChanges(); // ngOnInit

    expect(fixture.componentInstance.isAdmin()).toBeTrue();
    expect(fixture.componentInstance.mode()).toBe('all');
    expect(prefsApiMock.get).not.toHaveBeenCalled();
    expect(jobsApiMock.list).toHaveBeenCalled();
    expect(fixture.componentInstance.jobs().length).toBe(1);
  });

  it('should check onboarding status and start in recommended mode for normal Job Seekers', async () => {
    const { fixture, prefsApiMock, recApiMock } = await setup(
      ['User'],
      [],
      [{ job: { id: 1, title: 'Recommended Job' }, score: 0.95, reason: 'Skills match' }],
      { onboardingCompleted: true }
    );

    fixture.detectChanges();

    expect(fixture.componentInstance.isAdmin()).toBeFalse();
    expect(fixture.componentInstance.mode()).toBe('recommended');
    expect(prefsApiMock.get).toHaveBeenCalled();
    expect(recApiMock.list).toHaveBeenCalled();
    expect(fixture.componentInstance.prefsWarning()).toBeNull();
  });

  it('should show onboarding warning alert if job seeker has not onboarded', async () => {
    const { fixture, prefsApiMock } = await setup(
      ['User'],
      [],
      [],
      { onboardingCompleted: false }
    );

    fixture.detectChanges();

    expect(fixture.componentInstance.prefsWarning()).toContain('Add your job preferences');
  });

  describe('Salary Parsing & Filtering', () => {
    it('should parse and annualize yearly, daily, and hourly rates', async () => {
      const mockJobs = [
        { id: 1, title: 'Yearly Job', salaryRaw: '$120k per year' },
        { id: 2, title: 'Hourly Job', salaryRaw: '$50/hour' },
        { id: 3, title: 'Daily Job', salaryRaw: '$400 a day' },
        { id: 4, title: 'No Salary Job', salaryRaw: null }
      ];

      const { fixture } = await setup(['Admin'], mockJobs);
      fixture.detectChanges();

      // Access private helper parseSalary using bracket notation
      const parser = (fixture.componentInstance as any).parseSalary.bind(fixture.componentInstance);

      expect(parser('$120k per year')).toEqual({ min: 120000, max: 120000 });
      expect(parser('$50/hour')).toEqual({ min: 100000, max: 100000 }); // 50 * 2000
      expect(parser('$400 a day')).toEqual({ min: 104000, max: 104000 }); // 400 * 260
      expect(parser(null)).toEqual({});
    });

    it('should filter jobs correctly based on Min-Max salary range values in All Jobs mode', async () => {
      const mockJobs = [
        { id: 1, title: 'Underpaid Job', salaryRaw: '$40k - $60k' },
        { id: 2, title: 'Target Job', salaryRaw: '$90k - $120k' },
        { id: 3, title: 'Overpaid Job', salaryRaw: '$200k - $250k' },
        { id: 4, title: 'Undisclosed Salary Job', salaryRaw: null }
      ];

      const { fixture } = await setup(['Admin'], mockJobs);
      fixture.detectChanges();

      expect(fixture.componentInstance.jobs().length).toBe(4);

      // Set filter bounds: min = 80k, max = 150k
      fixture.componentInstance.onFiltersChanged({
        query: '',
        location: 'Any',
        remoteOnly: false,
        minSalary: 80000,
        maxSalary: 150000
      });

      const filtered = fixture.componentInstance.jobs();
      // Should retain: "Target Job" and "Undisclosed Salary Job"
      expect(filtered.length).toBe(2);
      expect(filtered.map(j => j.title)).toContain('Target Job');
      expect(filtered.map(j => j.title)).toContain('Undisclosed Salary Job');
    });
  });
});
