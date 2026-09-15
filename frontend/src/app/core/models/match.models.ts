// Mirrors the ONE77 Match public API response shape exactly
// (migration/backend/src/One77.Core/Match — Milestone 6). Field names,
// bucketing (best_match_* / recommended_* / compatible_*) and the
// two-bucket-not-three priority grouping are load-bearing — see
// migration/docs/MATCH_PARITY_REPORT.md before changing anything here.

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

export interface MatchCard {
  product: MatchProductRef;
  status: string | null;
  priority: number | null;
  priority_label: string;
  use_cases: string[];
  reason: string;
  is_curated: boolean;
}

export interface PublicMatchResult {
  source: MatchProductRef;
  pellets: MatchCard[];
  accessories: MatchCard[];
  best_match_pellets: MatchCard[];
  recommended_pellets: MatchCard[];
  compatible_pellets: MatchCard[];
  best_match_accessories: MatchCard[];
  recommended_accessories: MatchCard[];
  compatible_accessories: MatchCard[];
  match_reason: string;
  reason_source: string;
}
