// Mirrors One77.Core.Bundles.Bundle/BundleItem exactly (Milestone 7 / 10).

export interface BundleItem {
  id: number;
  shopify_product_id: string;
  item_role: 'Primary' | 'Pellet' | 'Accessory';
  sort_order: number;
}

export interface AdminBundle {
  id: number;
  name: string;
  tagline: string | null;
  status: 'Draft' | 'Published' | 'Archived';
  sort_priority: number;
  created_at: string;
  updated_at: string;
  items: BundleItem[];
}

export interface BundleItemInput {
  shopify_product_id: string;
  item_role: 'Primary' | 'Pellet' | 'Accessory';
}

export interface SaveBundleRequest {
  name: string;
  tagline: string | null;
  status: string;
  sort_priority: number;
  items: BundleItemInput[];
}
