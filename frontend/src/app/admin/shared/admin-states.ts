import { Component, input, output } from '@angular/core';

/**
 * Loading/empty/error states for Admin screens, in the same spirit as the
 * public site's shared/ui equivalents, but toned to the Admin palette.
 * Kept in one file since each is a couple of lines — not worth three files.
 */
@Component({
  selector: 'app-admin-loading',
  template: `
    <div class="admin-loading" role="status" aria-live="polite">
      <span class="spinner" aria-hidden="true"></span>
      <span>{{ label() }}</span>
    </div>
  `,
  styles: [
    `
      .admin-loading {
        display: flex;
        align-items: center;
        gap: var(--admin-space-3);
        padding: var(--admin-space-5) 0;
        color: var(--admin-text-muted);
      }
      .spinner {
        width: 1rem;
        height: 1rem;
        border-radius: 50%;
        border: 2px solid var(--admin-border);
        border-top-color: var(--admin-accent);
        animation: admin-spin 0.7s linear infinite;
      }
      @keyframes admin-spin {
        to {
          transform: rotate(360deg);
        }
      }
    `,
  ],
})
export class AdminLoading {
  label = input('Loading…');
}

@Component({
  selector: 'app-admin-empty',
  template: `
    <div class="admin-empty">
      <p class="heading">{{ heading() }}</p>
      @if (body()) {
        <p class="body">{{ body() }}</p>
      }
    </div>
  `,
  styles: [
    `
      .admin-empty {
        padding: var(--admin-space-6) var(--admin-space-5);
        text-align: center;
        border: 1px dashed var(--admin-border);
        border-radius: var(--admin-radius);
        color: var(--admin-text-muted);
      }
      .heading {
        margin: 0;
        font-weight: 500;
        color: var(--admin-text);
      }
      .body {
        margin: var(--admin-space-2) 0 0;
        font-size: 0.875rem;
      }
    `,
  ],
})
export class AdminEmpty {
  heading = input('Nothing here yet');
  body = input<string | null>(null);
}

@Component({
  selector: 'app-admin-error',
  template: `
    <div class="admin-error" role="alert">
      <p class="message">{{ message() }}</p>
      @if (retryable()) {
        <button type="button" class="admin-btn admin-btn-secondary" (click)="retry.emit()">Try again</button>
      }
    </div>
  `,
  styles: [
    `
      .admin-error {
        padding: var(--admin-space-5);
        border: 1px solid var(--admin-danger);
        background: var(--admin-danger-bg);
        border-radius: var(--admin-radius);
      }
      .message {
        margin: 0 0 var(--admin-space-3);
        color: var(--admin-text);
      }
    `,
  ],
})
export class AdminError {
  message = input('Something went wrong. Please try again.');
  retryable = input(true);
  retry = output<void>();
}
