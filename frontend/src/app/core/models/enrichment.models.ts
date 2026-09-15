// Mirrors the ONE77 Product Enrichment public/internal read API response
// shape exactly (migration/backend/src/One77.Api.DevHost/Enrichment —
// Milestone 7). This is ONE77-owned technical data only — never a
// substitute for Shopify's own product record.

export interface EnrichmentSpecification {
  spec_key: string;
  spec_value: string;
  sort_order: number;
}

export interface PublicEnrichment {
  shopify_product_id: string;
  category: string;
  calibre: string | null;
  powerplant_type: string | null;
  weight_grains: number | null;
  recommended_pellet_weight_min: number | null;
  recommended_pellet_weight_max: number | null;
  compatible_powerplants: string[];
  use_cases: string[];
  specifications: EnrichmentSpecification[];
}
