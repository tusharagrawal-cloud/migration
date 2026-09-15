import { Component } from '@angular/core';

/** Consistent max-width, centered content column used by every page. */
@Component({
  selector: 'app-page-container',
  template: `<div class="page-container"><ng-content /></div>`,
  styles: [
    `
      .page-container {
        max-width: var(--container-width);
        margin-inline: auto;
        padding-inline: var(--container-padding-inline);
        padding-block: var(--space-7);
      }
    `,
  ],
})
export class PageContainer {}
