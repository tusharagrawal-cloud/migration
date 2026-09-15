import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LearnService } from '../../core/services/learn.service';
import { LearnCategory } from '../../core/models/learn.models';
import { PageContainer } from '../../shared/ui/page-container';
import { LoadingState } from '../../shared/ui/loading-state';
import { EmptyState } from '../../shared/ui/empty-state';
import { ErrorState } from '../../shared/ui/error-state';

type LoadState = 'loading' | 'loaded' | 'empty' | 'error';

@Component({
  selector: 'app-learn-landing-page',
  imports: [RouterLink, PageContainer, LoadingState, EmptyState, ErrorState],
  template: `
    <app-page-container>
      <div class="intro">
        <p class="eyebrow">Know before you buy</p>
        <h1>Learn</h1>
        <p class="lede">Guides and articles from the ONE77 team — the background that makes Match make sense.</p>
      </div>

      @switch (state()) {
        @case ('loading') {
          <app-loading-state label="Loading categories…" />
        }
        @case ('error') {
          <app-error-state message="We couldn't load Learn categories right now." (retry)="load()" />
        }
        @case ('empty') {
          <app-empty-state heading="No categories yet" body="Check back soon." />
        }
        @case ('loaded') {
          <div class="grid">
            @for (category of categories(); track category.id) {
              <a [routerLink]="[category.slug]" class="tile">
                <p class="name">{{ category.name }}</p>
                <p class="description">{{ category.description }}</p>
                <span class="link">Read guides →</span>
              </a>
            }
          </div>
        }
      }
    </app-page-container>
  `,
  styles: [
    `
      .intro {
        max-width: 60ch;
        margin-bottom: var(--space-7);
      }
      .eyebrow {
        font-size: 0.75rem;
        letter-spacing: 0.18em;
        text-transform: uppercase;
        font-weight: var(--font-weight-bold);
        color: var(--color-accent);
        margin: 0 0 var(--space-2);
      }
      h1 {
        font-family: var(--font-family-heading);
        font-size: var(--font-size-2xl);
        margin: 0 0 var(--space-3);
      }
      .lede {
        color: var(--color-text-muted);
        margin: 0;
      }
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
        gap: var(--space-5);
      }
      .tile {
        display: block;
        text-decoration: none;
        color: inherit;
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-md);
        padding: var(--space-5);
        transition: border-color var(--transition-base);
      }
      .tile:hover {
        border-color: var(--color-accent);
      }
      .name {
        margin: 0;
        font-family: var(--font-family-heading);
        font-weight: var(--font-weight-semibold);
        font-size: var(--font-size-md);
      }
      .description {
        margin: var(--space-2) 0 0;
        color: var(--color-text-muted);
        font-size: var(--font-size-sm);
      }
      .link {
        display: inline-block;
        margin-top: var(--space-4);
        font-size: 0.75rem;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: var(--color-text-faint);
      }
      .tile:hover .link {
        color: var(--color-accent);
      }
    `,
  ],
})
export class LearnLandingPage implements OnInit {
  private readonly learnService = inject(LearnService);

  protected state = signal<LoadState>('loading');
  protected categories = signal<LearnCategory[]>([]);

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    this.learnService.getCategories().subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.state.set(categories.length ? 'loaded' : 'empty');
      },
      error: () => this.state.set('error'),
    });
  }
}
