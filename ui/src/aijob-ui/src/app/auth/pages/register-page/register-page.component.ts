import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthApiService } from '../../../services/auth-api.service';

type RegisterMode = 'user' | 'admin';

@Component({
  selector: 'app-register-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register-page.component.html'
})
export class RegisterPageComponent {
  private readonly authApi = inject(AuthApiService);
  private readonly router = inject(Router);

  readonly mode = signal<RegisterMode>('user');

  readonly email = signal('');
  readonly password = signal('');
  readonly confirmPassword = signal('');

  readonly error = signal<string | null>(null);
  readonly loading = signal(false);

  readonly title = computed(() =>
    this.mode() === 'admin' ? 'Admin Registration' : 'User Registration'
  );

  async submit() {
    this.error.set(null);

    if (!this.email().trim()) {
      this.error.set('Email is required');
      return;
    }

    if (this.password().length < 6) {
      this.error.set('Password must be at least 6 characters');
      return;
    }

    if (this.password() !== this.confirmPassword()) {
      this.error.set('Passwords do not match');
      return;
    }

    // NOTE: Backend currently exposes /api/Auth/register only.
    // This creates a normal user. Admin creation usually should be server-controlled.
    if (this.mode() === 'admin') {
      this.error.set('Admin accounts must be created by an existing admin.');
      return;
    }

    this.loading.set(true);
    try {
      await this.authApi
        .register({ email: this.email(), password: this.password() })
        .toPromise();

      await this.router.navigateByUrl('/auth/login');
    } catch (e: any) {
      this.error.set(e?.message ?? 'Registration failed');
    } finally {
      this.loading.set(false);
    }
  }
}
