import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminAuthService } from '../core/services/admin-auth.service';

@Component({
  selector: 'app-admin-login-page',
  imports: [ReactiveFormsModule],
  template: `
    <div class="admin-root login-screen">
      <div class="login-card">
        <h1>ONE77 Admin</h1>
        <p class="login-subtitle">Sign in to manage products, matches, bundles, and content.</p>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <label class="admin-field">
            Email
            <input type="email" formControlName="email" autocomplete="username" />
          </label>
          <label class="admin-field">
            Password
            <input type="password" formControlName="password" autocomplete="current-password" />
          </label>

          @if (errorMessage()) {
            <p class="login-error" role="alert">{{ errorMessage() }}</p>
          }

          <button type="submit" class="admin-btn admin-btn-primary" [disabled]="submitting()">
            {{ submitting() ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>
      </div>
    </div>
  `,
  styles: [
    `
      .login-screen {
        display: flex;
        align-items: center;
        justify-content: center;
        min-height: 100vh;
        padding: var(--admin-space-5);
      }
      .login-card {
        width: 100%;
        max-width: 380px;
        background: var(--admin-surface);
        border: 1px solid var(--admin-border);
        border-radius: var(--admin-radius-lg);
        box-shadow: var(--admin-shadow-sm);
        padding: var(--admin-space-6);
      }
      h1 {
        font-size: 1.375rem;
        margin-bottom: var(--admin-space-2);
      }
      .login-subtitle {
        color: var(--admin-text-muted);
        font-size: 0.875rem;
        margin-bottom: var(--admin-space-6);
      }
      form {
        display: grid;
        gap: var(--admin-space-4);
      }
      .login-error {
        margin: 0;
        color: var(--admin-danger);
        font-size: 0.875rem;
      }
      button[type='submit'] {
        justify-content: center;
      }
    `,
  ],
})
export class AdminLoginPage {
  private readonly authService = inject(AdminAuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected submitting = signal(false);
  protected errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    const { email, password } = this.form.getRawValue();

    this.authService.login(email, password).subscribe({
      next: () => {
        this.router.navigate(['/admin/dashboard']);
      },
      error: () => {
        this.submitting.set(false);
        this.errorMessage.set('Incorrect email or password.');
      },
    });
  }
}
