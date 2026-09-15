// Mirrors One77.Core.Enrichment.EnrichmentRecord and the Admin Enrichment
// request DTOs exactly (Milestone 7 / 10).

export interface EnrichmentSpecification {
  spec_key: string;
  spec_value: string;
  sort_order: number;
}

export interface EnrichmentRecord {
  id: number;
  shopify_product_id: string;
  category: string;
  is_active: boolean;
  calibre: string | null;
  powerplant_type: string | null;
  weight_grains: number | null;
  recommended_pellet_weight_min: number | null;
  recommended_pellet_weight_max: number | null;
  compatible_powerplants: string[];
  use_cases: string[];
  specifications: EnrichmentSpecification[];
  created_at: string;
  updated_at: string;
}

export interface SaveEnrichmentRequest {
  shopify_product_id: string;
  category: string;
  is_active: boolean;
  calibre: string | null;
  powerplant_type: string | null;
  weight_grains: number | null;
  recommended_pellet_weight_min: number | null;
  recommended_pellet_weight_max: number | null;
  compatible_powerplants: string[];
  use_cases: string[];
  specifications: EnrichmentSpecification[];
}
