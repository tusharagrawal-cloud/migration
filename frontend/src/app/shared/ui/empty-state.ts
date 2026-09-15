import { Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  template: `
    <div class="empty-state">
      <p class="heading">{{ heading() }}</p>
      @if (body()) {
        <p class="body">{{ body() }}</p>
      }
    </div>
  `,
  styles: [
    `
      .empty-state {
        padding: var(--space-7) var(--space-5);
        text-align: center;
        border: 1px dashed var(--color-border);
        border-radius: var(--radius-md);
        color: var(--color-text-muted);
      }
      .heading {
        margin: 0;
        font-weight: var(--font-weight-medium);
        color: var(--color-text);
      }
      .body {
        margin: var(--space-2) 0 0;
      }
    `,
  ],
})
export class EmptyState {
  heading = input('Nothing here yet');
  body = input<string | null>(null);
}
