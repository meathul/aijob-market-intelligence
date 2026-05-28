import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthApiService } from '../../../services/auth-api.service';
import { AuthService } from '../../../core/auth/auth.service';

type LoginMode = 'user' | 'admin';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login-page.component.html'
})
export class LoginPageComponent {
  private readonly authApi = inject(AuthApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly mode = signal<LoginMode>('user');

  readonly email = signal('');
  readonly password = signal('');
  readonly error = signal<string | null>(null);
  readonly loading = signal(false);

  readonly title = computed(() =>
    this.mode() === 'admin' ? 'Admin Login' : 'User Login'
  );

  async submit() {
    this.error.set(null);
    this.loading.set(true);

    try {
      const res = await this.authApi
        .login({ email: this.email(), password: this.password() })
        .toPromise();

      const token = res?.accessToken ?? res?.token;
      if (!token) throw new Error('Login response missing accessToken');

      const roles = res?.roles ?? [];
      const isAdmin = roles.includes('Admin');
      if (this.mode() === 'admin' && !isAdmin) {
        throw new Error('This account is not an admin.');
      }

      this.auth.setAuth({ token, email: res?.email, roles });
      await this.router.navigateByUrl('/dashboard');
    } catch (e: any) {
      this.error.set(e?.message ?? 'Login failed');
    } finally {
      this.loading.set(false);
    }
  }
}
