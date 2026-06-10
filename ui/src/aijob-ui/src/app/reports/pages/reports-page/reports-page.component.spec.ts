import { TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of, throwError } from 'rxjs';
import { ReportsPageComponent } from './reports-page.component';
import { AnalyticsApiService } from '../../../services/analytics-api.service';
import { AdminApiService } from '../../../services/admin-api.service';

describe('ReportsPageComponent', () => {
  async function setup(
    analyticsApiVal: Partial<AnalyticsApiService>,
    adminApiVal: Partial<AdminApiService>
  ) {
    // Clear localStorage before each run to isolate sync history tests
    localStorage.removeItem('admin_sync_history');

    await TestBed.configureTestingModule({
      imports: [ReportsPageComponent],
      providers: [
        { provide: AnalyticsApiService, useValue: analyticsApiVal },
        { provide: AdminApiService, useValue: adminApiVal }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();

    const fixture = TestBed.createComponent(ReportsPageComponent);
    return { fixture };
  }

  const defaultAnalyticsMock: Partial<AnalyticsApiService> = {
    breakdownSource: () => of([{ source: 'LinkedIn', count: 35 }]),
    breakdownLocation: () => of([{ location: 'New York', count: 20 }]),
    breakdownExperience: () => of([{ experienceLevel: 'Mid', count: 15 } as any]),
    ingestionDaily: () => of([{ day: '2026-06-09T00:00:00Z', count: 10 }]),
    salary: () => of({ averageSalary: 125000, minSalary: 70000, maxSalary: 210000 })
  };

  it('should initialize and load daily ingestion trends, salary stats, and sync logs', async () => {
    const { fixture } = await setup(defaultAnalyticsMock, {});
    fixture.detectChanges(); // ngOnInit

    expect(fixture.componentInstance.dailyIngestionData().length).toBe(1);
    expect(fixture.componentInstance.salaryStats()).toEqual({
      averageSalary: 125000,
      minSalary: 70000,
      maxSalary: 210000
    });
    // Check seed sync history is populated if localStorage was empty
    expect(fixture.componentInstance.syncHistory().length).toBe(2);
    expect(fixture.componentInstance.syncHistory()[0].jobsAdded).toBe(24);
  });

  it('should calculate proper computed stats (source totals, trends totals)', async () => {
    const { fixture } = await setup(defaultAnalyticsMock, {});
    fixture.detectChanges();

    expect(fixture.componentInstance.totalIngestionLast30Days()).toBe(10);
    expect(fixture.componentInstance.totalSources()).toBe(35);
  });

  it('should trigger sync successfully, append to history, and store in localStorage', async () => {
    const triggerFetchSpy = jasmine.createSpy('triggerFetch').and.returnValue(
      of({ success: true, message: 'Sync done', jobsAdded: 7, timestamp: new Date().toISOString() })
    );

    const { fixture } = await setup(defaultAnalyticsMock, {
      triggerFetch: triggerFetchSpy
    });

    fixture.detectChanges();

    // Trigger manual sync
    fixture.componentInstance.triggerSync();

    expect(fixture.componentInstance.syncing()).toBeFalse();
    expect(fixture.componentInstance.syncResult()?.jobsAdded).toBe(7);
    expect(fixture.componentInstance.syncError()).toBeNull();

    // In addition to initial 2 seed history items, we added a new successful sync item
    const history = fixture.componentInstance.syncHistory();
    expect(history.length).toBe(3);
    expect(history[0].status).toBe('success');
    expect(history[0].jobsAdded).toBe(7);

    // Verify localStorage persistence
    const saved = JSON.parse(localStorage.getItem('admin_sync_history') || '[]');
    expect(saved.length).toBe(3);
    expect(saved[0].jobsAdded).toBe(7);
  });

  it('should handle sync errors gracefully, update UI, and log to sync history', async () => {
    const triggerFetchSpy = jasmine.createSpy('triggerFetch').and.returnValue(
      throwError(() => ({ error: { message: 'Timeout contacting source provider' } }))
    );

    const { fixture } = await setup(defaultAnalyticsMock, {
      triggerFetch: triggerFetchSpy
    });

    fixture.detectChanges();

    fixture.componentInstance.triggerSync();

    expect(fixture.componentInstance.syncing()).toBeFalse();
    expect(fixture.componentInstance.syncError()).toBe('Timeout contacting source provider');
    expect(fixture.componentInstance.syncResult()).toBeNull();

    // A failure item is recorded in history
    const history = fixture.componentInstance.syncHistory();
    expect(history.length).toBe(3);
    expect(history[0].status).toBe('error');
    expect(history[0].message).toBe('Timeout contacting source provider');
  });
});
