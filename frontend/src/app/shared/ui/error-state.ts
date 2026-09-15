import { Component, input, output } from '@angular/core';

/**
 * Human-language error display. Never pass raw HTTP status text, SQL, or
 * exception details into `message` — components should translate failures
 * into plain sentences before reaching this component.
 */
@Component({
  selector: 'app-error-state',
  template: `
    <div class="error-state" role="alert">
      <p class="message">{{ message() }}</p>
      @if (retryable()) {
        <button type="button" (click)="retry.emit()">Try again</button>
      }
    </div>
  `,
  styles: [
    `
      .error-state {
        padding: var(--space-6) var(--space-5);
        border: 1px solid var(--color-danger);
        border-radius: var(--radius-md);
        background: color-mix(in srgb, var(--color-danger) 12%, var(--color-surface));
      }
      .message {
        margin: 0 0 var(--space-3);
        color: var(--color-text);
      }
      button {
        background: var(--color-accent);
        color: var(--color-accent-contrast);
        border: none;
        border-radius: var(--radius-sm);
        padding: var(--space-2) var(--space-4);
        cursor: pointer;
        font-size: var(--font-size-sm);
      }
      button:hover {
        background: var(--color-accent-strong);
      }
    `,
  ],
})
export class ErrorState {
  message = input('Something went wrong. Please try again.');
  retryable = input(true);
  retry = output<void>();
}
