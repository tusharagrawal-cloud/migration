// Mirrors One77.Core.Learn's admin-facing shapes exactly (Milestone 3 / 10).
// Status vocabulary is Draft/Live/Hidden — preserved verbatim, never
// translated to Published/Archived (that's Bundle's vocabulary, not Learn's).

export type LearnStatus = 'Draft' | 'Live' | 'Hidden';

export interface AdminLearnCategory {
  id: number;
  name: string;
  slug: string;
  description: string;
  sort_priority: number;
  status: LearnStatus;
  created_at: string;
  updated_at: string;
}

export interface AdminLearnCategoryWithCount extends AdminLearnCategory {
  entry_count: number;
}

export interface AdminLearnEntry {
  id: number;
  category_id: number;
  title: string;
  body: string;
  sort_priority: number;
  status: LearnStatus;
  created_at: string;
  updated_at: string;
}

export interface AdminLearnEntryWithCategoryName extends AdminLearnEntry {
  category_name: string;
}

export interface SaveLearnCategoryRequest {
  name: string;
  slug?: string | null;
  description: string;
  sort_priority: number;
  status: LearnStatus;
}

export interface SaveLearnEntryRequest {
  category_id: number;
  title: string;
  body: string;
  sort_priority: number;
  status: LearnStatus;
}
