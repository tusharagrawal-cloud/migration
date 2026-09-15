// Mirrors the ONE77 Bundle public API response shape exactly
// (migration/backend/src/One77.Api.DevHost/Bundles — Milestone 7).
// Deliberately no price/savings/media fields — Shopify owns commerce data.

export interface PublicBundleItem {
  shopify_product_id: string;
  item_role: string;
  sort_order: number;
}

export interface PublicBundle {
  id: number;
  name: string;
  tagline: string;
  sort_priority: number;
  items: PublicBundleItem[];
}
