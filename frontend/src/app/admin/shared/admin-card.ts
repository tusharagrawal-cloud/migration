import { Component } from '@angular/core';

@Component({
  selector: 'app-admin-card',
  template: `<div class="admin-card"><ng-content /></div>`,
  styles: [
    `
      .admin-card {
        background: var(--admin-surface);
        border: 1px solid var(--admin-border);
        border-radius: var(--admin-radius);
        box-shadow: var(--admin-shadow-sm);
        padding: var(--admin-space-5);
      }
    `,
  ],
})
export class AdminCard {}
