import { Component, input } from '@angular/core';

@Component({
  selector: 'app-loading-state',
  template: `
    <div class="loading-state" role="status" aria-live="polite">
      <span class="spinner" aria-hidden="true"></span>
      <span>{{ label() }}</span>
    </div>
  `,
  styles: [
    `
      .loading-state {
        display: flex;
        align-items: center;
        gap: var(--space-3);
        padding: var(--space-6) 0;
        color: var(--color-text-muted);
      }
      .spinner {
        width: 1.1rem;
        height: 1.1rem;
        border-radius: 50%;
        border: 2px solid var(--color-border);
        border-top-color: var(--color-accent);
        animation: spin 0.7s linear infinite;
      }
      @keyframes spin {
        to {
          transform: rotate(360deg);
        }
      }
    `,
  ],
})
export class LoadingState {
  label = input('Loading…');
}
