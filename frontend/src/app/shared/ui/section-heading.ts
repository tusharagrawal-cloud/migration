import { Component, input } from '@angular/core';

@Component({
  selector: 'app-section-heading',
  template: `
    <header class="section-heading">
      <h2>{{ title() }}</h2>
      @if (subtitle()) {
        <p class="subtitle">{{ subtitle() }}</p>
      }
    </header>
  `,
  styles: [
    `
      .section-heading {
        margin-bottom: var(--space-6);
      }
      h2 {
        font-size: var(--font-size-2xl);
      }
      .subtitle {
        margin: var(--space-2) 0 0;
        color: var(--color-text-muted);
        font-size: var(--font-size-md);
        max-width: 60ch;
      }
    `,
  ],
})
export class SectionHeading {
  title = input.required<string>();
  subtitle = input<string | null>(null);
}
