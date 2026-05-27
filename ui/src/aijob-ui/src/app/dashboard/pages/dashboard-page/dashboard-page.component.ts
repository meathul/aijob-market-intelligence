import { Component } from '@angular/core';

import {
  ApexAxisChartSeries,
  ApexChart,
  ApexDataLabels,
  ApexFill,
  ApexGrid,
  ApexStroke,
  ApexTooltip,
  ApexXAxis,
  ApexYAxis,
  ChartComponent,
  NgApexchartsModule
} from 'ng-apexcharts';

export type JobsTrendChartOptions = {
  series: ApexAxisChartSeries;
  chart: ApexChart;
  xaxis: ApexXAxis;
  yaxis: ApexYAxis;
  grid: ApexGrid;
  stroke: ApexStroke;
  dataLabels: ApexDataLabels;
  fill: ApexFill;
  tooltip: ApexTooltip;
  colors: string[];
};

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [NgApexchartsModule],
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

  // Mock series (daily counts)
  readonly chartOptions: JobsTrendChartOptions = {
    series: [
      {
        name: 'Jobs',
        data: [120, 132, 98, 154, 166, 142, 176, 190, 172, 210, 198, 222]
      }
    ],
    chart: {
      type: 'area',
      height: 260,
      toolbar: { show: false },
      zoom: { enabled: false },
      foreColor: '#cbd5e1',
      fontFamily: 'Roboto, "Helvetica Neue", sans-serif'
    },
    colors: ['#6366f1'],
    dataLabels: { enabled: false },
    stroke: { curve: 'smooth', width: 2 },
    fill: {
      type: 'gradient',
      gradient: {
        opacityFrom: 0.35,
        opacityTo: 0.05,
        stops: [0, 90, 100]
      }
    },
    grid: {
      borderColor: 'rgba(148, 163, 184, 0.15)',
      strokeDashArray: 3
    },
    xaxis: {
      categories: [
        'W1',
        'W2',
        'W3',
        'W4',
        'W5',
        'W6',
        'W7',
        'W8',
        'W9',
        'W10',
        'W11',
        'W12'
      ],
      axisBorder: { color: 'rgba(148, 163, 184, 0.2)' },
      axisTicks: { color: 'rgba(148, 163, 184, 0.2)' }
    },
    yaxis: {
      labels: {
        formatter: (v) => Math.round(v).toString()
      }
    },
    tooltip: { theme: 'dark' }
  };
}
