// Mirrors One77.Core.Match's admin-facing shapes exactly (Milestone 6 / 10).
// See migration/docs/MATCH_PARITY_REPORT.md before changing field names.

export interface MatchProductRef {
  shopify_product_id: string;
  category: string;
  is_active: boolean;
  calibre: string | null;
  powerplant_type: string | null;
  weight_grains: number | null;
  recommended_pellet_weight_min: number | null;
  recommended_pellet_weight_max: number | null;
  use_cases: string[];
  compatible_powerplants: string[];
}

export interface MatchCompleteness {
  compat_pellets: number;
  rec_pellets: number;
  compat_acc: number;
  rec_acc: number;
  state: 'complete' | 'needs_review' | 'no_matches';
}

export interface MatchAirgunSummaryItem {
  shopify_product_id: string;
  calibre: string | null;
  powerplant_type: string | null;
  counts: MatchCompleteness;
}

export interface MatchRelationshipView {
  id: number;
  source_shopify_product_id: string;
  target_shopify_product_id: string;
  target_category: string;
  status: 'compatible' | 'recommended' | 'not_recommended';
  priority: number | null;
  priority_label: string;
  reason: string;
  admin_notes: string;
  calibre_override: boolean;
  is_active: boolean;
  created_at: string;
  updated_at: string;
  use_cases: string[];
  target: MatchProductRef;
}

export interface MatchCandidateItem {
  product: MatchProductRef;
  current_relationship: MatchRelationshipView | null;
}

export interface UpsertMatchRelationshipRequest {
  source_shopify_product_id: string;
  target_shopify_product_id: string;
  status: string;
  priority: number | null;
  use_cases: string[];
  reason: string | null;
  admin_notes: string | null;
  calibre_override: boolean;
}

export interface PatchMatchRelationshipRequest {
  status?: string;
  priority?: number | null;
  use_cases?: string[];
  reason?: string;
  admin_notes?: string;
  calibre_override?: boolean;
  active?: boolean;
}

export interface MatchBulkResult {
  created: number;
  updated: number;
  skipped_calibre: number;
}
