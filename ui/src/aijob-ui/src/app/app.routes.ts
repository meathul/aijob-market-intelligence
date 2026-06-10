import { Routes } from '@angular/router';
import { authGuard } from './auth/guards/auth.guard';
import { adminGuard } from './auth/guards/admin.guard';
import { guestGuard } from './auth/guards/guest.guard';
import { onboardingGuard } from './auth/guards/onboarding.guard';
import { onboardingPageGuard } from './auth/guards/onboarding-page.guard';

export const routes: Routes = [
  {
    path: 'auth',
    canActivate: [guestGuard],
    children: [
      {
        path: 'login',
        loadComponent: () =>
          import('./auth/pages/login-page/login-page.component').then(
            (m) => m.LoginPageComponent
          )
      },
      {
        path: 'register',
        loadComponent: () =>
          import('./auth/pages/register-page/register-page.component').then(
            (m) => m.RegisterPageComponent
          )
      },
      {
        path: 'logged-out',
        loadComponent: () =>
          import('./auth/pages/logged-out-page/logged-out-page.component').then(
            (m) => m.LoggedOutPageComponent
          )
      },
      { path: '', pathMatch: 'full', redirectTo: 'login' }
    ]
  },
  {
    path: 'onboarding',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./onboarding/pages/profile-setup-page/profile-setup-page.component').then(
        (m) => m.ProfileSetupPageComponent
      )
  },
  {
    path: '',
    canActivate: [authGuard, onboardingGuard],
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
        canActivate: [adminGuard],
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
    redirectTo: 'auth/login'
  }
];
