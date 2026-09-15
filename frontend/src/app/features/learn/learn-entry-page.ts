import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LearnService } from '../../core/services/learn.service';
import { LearnCategoryStore } from '../../core/services/learn-category-store';
import { LearnCategory, LearnEntry } from '../../core/models/learn.models';
import { PageContainer } from '../../shared/ui/page-container';
import { LoadingState } from '../../shared/ui/loading-state';
import { ErrorState } from '../../shared/ui/error-state';

type LoadState = 'loading' | 'loaded' | 'not-found' | 'error';

/**
 * Reuses the category detail already fetched by LearnCategoryPage via
 * LearnCategoryStore — no extra API call on the common navigation path.
 * A direct deep link (store empty) falls back to fetching the category.
 *
 * A deliberately lighter, paper-like surface for the reading experience
 * itself — long-form text reads better this way than on the site's default
 * near-black ground; the rest of the storefront stays dark. See
 * --color-paper-* in styles.css.
 */
@Component({
  selector: 'app-learn-entry-page',
  imports: [RouterLink, PageContainer, LoadingState, ErrorState],
  template: `
    <app-page-container>
      @switch (state()) {
        @case ('loading') {
          <app-loading-state label="Loading article…" />
        }
        @case ('error') {
          <app-error-state message="We couldn't load this article right now." (retry)="load()" />
        }
        @case ('not-found') {
          <app-error-state message="We couldn't find that article." [retryable]="false" />
        }
        @case ('loaded') {
          <a [routerLink]="['..']" class="back-link">&larr; {{ category()!.name }}</a>
          <article class="reading-surface">
            <h1>{{ entry()!.title }}</h1>
            <div class="body">{{ entry()!.body }}</div>
          </article>
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
      .reading-surface {
        background: var(--color-paper-surface);
        border: 1px solid var(--color-paper-border);
        border-radius: var(--radius-lg);
        padding: var(--space-7) var(--space-6);
        max-width: 72ch;
      }
      @media (min-width: 640px) {
        .reading-surface {
          padding: var(--space-8);
        }
      }
      h1 {
        color: var(--color-paper-text);
        font-family: var(--font-family-heading);
        font-size: var(--font-size-2xl);
        line-height: var(--line-height-tight);
        margin: 0 0 var(--space-6);
      }
      .body {
        color: var(--color-paper-text);
        font-family: var(--font-family-body);
        font-size: var(--font-size-md);
        white-space: pre-wrap;
        line-height: 1.75;
      }
    `,
  ],
})
export class LearnEntryPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly learnService = inject(LearnService);
  private readonly store = inject(LearnCategoryStore);

  protected state = signal<LoadState>('loading');
  protected category = signal<LearnCategory | null>(null);
  protected entry = signal<LearnEntry | null>(null);

  private slug = '';
  private entryId = 0;

  ngOnInit(): void {
    this.slug = this.route.snapshot.paramMap.get('slug') ?? '';
    this.entryId = Number(this.route.snapshot.paramMap.get('entryId'));
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    const cached = this.store.get(this.slug);
    if (cached) {
      this.applyDetail(cached);
      return;
    }

    this.learnService.getCategoryBySlug(this.slug).subscribe({
      next: (detail) => {
        this.store.set(detail);
        this.applyDetail(detail);
      },
      error: () => this.state.set('error'),
    });
  }

  private applyDetail(detail: { category: LearnCategory; entries: LearnEntry[] }): void {
    const entry = detail.entries.find((e) => e.id === this.entryId);
    if (!entry) {
      this.state.set('not-found');
      return;
    }
    this.category.set(detail.category);
    this.entry.set(entry);
    this.state.set('loaded');
  }
}
