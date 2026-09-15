import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LearnService } from '../../core/services/learn.service';
import { LearnCategoryStore } from '../../core/services/learn-category-store';
import { LearnCategoryDetail } from '../../core/models/learn.models';
import { PageContainer } from '../../shared/ui/page-container';
import { LoadingState } from '../../shared/ui/loading-state';
import { EmptyState } from '../../shared/ui/empty-state';
import { ErrorState } from '../../shared/ui/error-state';

type LoadState = 'loading' | 'loaded' | 'error';

@Component({
  selector: 'app-learn-category-page',
  imports: [RouterLink, PageContainer, LoadingState, EmptyState, ErrorState],
  template: `
    <app-page-container>
      @switch (state()) {
        @case ('loading') {
          <app-loading-state label="Loading articles…" />
        }
        @case ('error') {
          <app-error-state message="We couldn't load this category right now." (retry)="load()" />
        }
        @case ('loaded') {
          <a routerLink="/learn" class="back-link">&larr; All guides</a>
          <div class="intro">
            <p class="eyebrow">Learn</p>
            <h1>{{ detail()!.category.name }}</h1>
            <p class="description">{{ detail()!.category.description }}</p>
          </div>

          @if (!detail()!.entries.length) {
            <app-empty-state heading="No articles yet" body="Check back soon." />
          } @else {
            <div class="entries">
              @for (entry of detail()!.entries; track entry.id) {
                <a [routerLink]="[entry.id]" class="tile">
                  <p class="title">{{ entry.title }}</p>
                  <span class="link">Read →</span>
                </a>
              }
            </div>
          }
        }
      }
    </app-page-container>
  `,
  styles: [
    `
      .back-link {
        display: inline-block;
        margin-bottom: var(--space-5);
        color: var(--color-text-muted);
        text-decoration: none;
        font-size: var(--font-size-sm);
      }
      .back-link:hover {
        color: var(--color-text);
      }
      .intro {
        max-width: 60ch;
        margin-bottom: var(--space-6);
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
      .description {
        color: var(--color-text-muted);
        margin: 0;
      }
      .entries {
        display: grid;
        gap: var(--space-3);
      }
      .tile {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--space-4);
        text-decoration: none;
        color: inherit;
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-md);
        padding: var(--space-4) var(--space-5);
        transition: border-color var(--transition-base);
      }
      .tile:hover {
        border-color: var(--color-accent);
      }
      .title {
        margin: 0;
        font-family: var(--font-family-heading);
        font-weight: var(--font-weight-medium);
      }
      .link {
        flex-shrink: 0;
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
export class LearnCategoryPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly learnService = inject(LearnService);
  private readonly store = inject(LearnCategoryStore);

  protected state = signal<LoadState>('loading');
  protected detail = signal<LearnCategoryDetail | null>(null);

  private slug = '';

  ngOnInit(): void {
    this.slug = this.route.snapshot.paramMap.get('slug') ?? '';
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    this.learnService.getCategoryBySlug(this.slug).subscribe({
      next: (detail) => {
        this.detail.set(detail);
        this.store.set(detail);
        this.state.set('loaded');
      },
      error: () => this.state.set('error'),
    });
  }
}
