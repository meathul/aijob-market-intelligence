import { Component } from '@angular/core';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss'
})
export class DashboardPageComponent {
  readonly kpis = [
    { label: 'New jobs (7d)', value: '1,284', delta: '+12%' },
    { label: 'Active sources', value: '3', delta: 'Stable' },
    { label: 'Top skill', value: 'TypeScript', delta: '+5%' },
    { label: 'Median salary', value: '$118k', delta: '+2%' }
  ];
}
