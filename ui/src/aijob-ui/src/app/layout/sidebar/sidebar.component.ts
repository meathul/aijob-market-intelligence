import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';

type NavItem = {
  label: string;
  path: string;
};

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly isAdmin = computed(() => (this.auth.state()?.roles ?? []).includes('Admin'));

  readonly nav: NavItem[] = [
    { label: 'Dashboard', path: '/dashboard' },
    { label: 'Jobs', path: '/jobs' },
    { label: 'Skills', path: '/skills' },
    { label: 'Salary', path: '/salary' }
  ];

  async logout() {
    this.auth.logout();
    await this.router.navigateByUrl('/auth/login');
  }
}
