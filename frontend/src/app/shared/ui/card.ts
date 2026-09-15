import { Component } from '@angular/core';

@Component({
  selector: 'app-card',
  template: `<div class="card"><ng-content /></div>`,
  styles: [
    `
      .card {
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-lg);
        padding: var(--space-5);
      }
    `,
  ],
})
export class Card {}
