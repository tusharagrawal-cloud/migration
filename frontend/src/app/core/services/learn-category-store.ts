import { Injectable, signal } from '@angular/core';
import { LearnCategoryDetail } from '../models/learn.models';

/**
 * Holds the most recently fetched Learn category (with its entries) so the
 * entry-detail sub-route can render without a second API call when the
 * customer navigates from the category page. A direct deep link to an
 * entry (cache empty) falls back to fetching the category again — this is
 * a convenience cache, not a source of truth.
 */
@Injectable({ providedIn: 'root' })
export class LearnCategoryStore {
  private readonly current = signal<LearnCategoryDetail | null>(null);

  set(detail: LearnCategoryDetail): void {
    this.current.set(detail);
  }

  get(slug: string): LearnCategoryDetail | null {
    const detail = this.current();
    return detail && detail.category.slug === slug ? detail : null;
  }
}
