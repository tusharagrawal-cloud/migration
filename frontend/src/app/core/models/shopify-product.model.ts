/**
 * The read shape a future Shopify adapter will provide. This is a CONTRACT
 * PLACEHOLDER, informed by migration/docs/SHOPIFY_V1_CONTRACT.md Section E
 * (Product: id, handle, title, images, variants, price range; Variant:
 * availableForSale, price) — not a live integration. Until the dedicated
 * Shopify integration milestone, every implementation of
 * ShopifyProductService returns clearly-labelled development fixture data
 * (see core/services/shopify-product.service.ts) shaped to satisfy this
 * exact interface, so swapping in the real adapter later requires no
 * changes to any component that consumes it.
 */
export interface ShopifyProductSummary {
  /** Opaque Shopify GID — never parsed, never assumed numeric. */
  id: string;
  /** Customer-facing routing segment, e.g. used at /airguns/:handle. */
  handle: string;
  title: string;
  vendor: string | null;
  imageUrl: string | null;
  /** Human-readable price string once Shopify is connected; null while unavailable (never fabricated). */
  priceLabel: string | null;
  availableForSale: boolean;
  /** ONE77 classification, used for listing/filtering — not a Shopify field. */
  category: 'airgun' | 'pellet' | 'accessory';
}

/**
 * Cart/checkout (Shopify Cart API) always operates on a Variant ID, never a
 * Product ID — per SHOPIFY_V1_CONTRACT.md Section D, variant-level selection
 * was explicitly deferred until "a concrete V1 requirement proves it
 * necessary"; the Add to Cart flow is that requirement. `id` here is the
 * opaque ProductVariant GID (gid://shopify/ProductVariant/{id}), never
 * parsed or reconstructed, same rule as the Product GID.
 */
export interface ShopifyProductVariant {
  id: string;
  title: string;
  availableForSale: boolean;
  priceLabel: string | null;
  /** e.g. [{ name: 'Caliber', value: '.177' }] — empty for single-variant products. */
  selectedOptions: { name: string; value: string }[];
}

export interface ShopifyProductDetail extends ShopifyProductSummary {
  images: string[];
  descriptionHtml: string | null;
  /** Original ("was") price for a strikethrough display, e.g. Shopify's compareAtPriceRange. Null when there's no discount. */
  compareAtPriceLabel: string | null;
  variants: ShopifyProductVariant[];
}