import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layout/shell/shell.component').then((m) => m.ShellComponent),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./dashboard/pages/dashboard-page/dashboard-page.component').then(
            (m) => m.DashboardPageComponent
          )
      },
      {
        path: 'jobs',
        loadComponent: () =>
          import('./jobs/pages/jobs-page/jobs-page.component').then(
            (m) => m.JobsPageComponent
          )
      },
      {
        path: 'skills',
        loadComponent: () =>
          import('./skills/pages/skills-page/skills-page.component').then(
            (m) => m.SkillsPageComponent
          )
      },
      {
        path: 'salary',
        loadComponent: () =>
          import('./salary/pages/salary-page/salary-page.component').then(
            (m) => m.SalaryPageComponent
          )
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./reports/pages/reports-page/reports-page.component').then(
            (m) => m.ReportsPageComponent
          )
      },
      {
        path: 'insights',
        loadComponent: () =>
          import('./insights/pages/insights-page/insights-page.component').then(
            (m) => m.InsightsPageComponent
          )
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
