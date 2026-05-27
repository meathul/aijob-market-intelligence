import { Component, computed, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter, map, startWith } from 'rxjs/operators';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-topbar',
  standalone: true,
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.scss'
})
export class TopbarComponent {
  private readonly router = inject(Router);

  private readonly url = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects),
      startWith(this.router.url)
    ),
    { initialValue: this.router.url }
  );

  readonly title = computed(() => {
    const u = this.url();
    if (u.startsWith('/jobs')) return 'Jobs';
    if (u.startsWith('/skills')) return 'Skills';
    if (u.startsWith('/salary')) return 'Salary';
    if (u.startsWith('/reports')) return 'Reports';
    if (u.startsWith('/insights')) return 'AI Insights';
    return 'Dashboard';
  });
}
