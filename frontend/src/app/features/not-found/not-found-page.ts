import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageContainer } from '../../shared/ui/page-container';

@Component({
  selector: 'app-not-found-page',
  imports: [RouterLink, PageContainer],
  template: `
    <app-page-container>
      <div class="not-found">
        <p class="code">404</p>
        <h1>We couldn't find that page</h1>
        <p class="body">The page you're looking for doesn't exist or may have moved.</p>
        <a routerLink="/">Back to home</a>
      </div>
    </app-page-container>
  `,
  styles: [
    `
      .not-found {
        text-align: center;
        padding-block: var(--space-9);
      }
      .code {
        font-size: var(--font-size-3xl);
        color: var(--color-text-faint);
        font-weight: var(--font-weight-bold);
        margin-bottom: var(--space-3);
      }
      h1 {
        font-size: var(--font-size-xl);
        margin-bottom: var(--space-3);
      }
      .body {
        color: var(--color-text-muted);
        margin-bottom: var(--space-5);
      }
      a {
        color: var(--color-accent-strong);
        text-decoration: none;
        font-weight: var(--font-weight-medium);
      }
    `,
  ],
})
export class NotFoundPage {}
