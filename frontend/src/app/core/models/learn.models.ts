// Mirrors the ONE77 Learn public API response shapes exactly
// (migration/backend/src/One77.Api.DevHost/Learn — Milestone 3).

export interface LearnCategory {
  id: number;
  name: string;
  slug: string;
  description: string;
}

export interface LearnEntry {
  id: number;
  title: string;
  body: string;
}

export interface LearnCategoryDetail {
  category: LearnCategory;
  entries: LearnEntry[];
}
