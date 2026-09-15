// Mirrors the ONE77 Homepage config public/admin read API response shape
// exactly (migration/backend/.../Homepage — Milestone 14). The one
// admin-controlled homepage content field: the hero photograph. Never
// styling/layout — those stay frontend-owned.
export interface HomepageConfig {
  hero_image_url: string | null;
}
