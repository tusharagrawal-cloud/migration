const DEV_FIXTURE_PREFIX = /^\[DEV FIXTURE\]\s*/;

/**
 * Strips the "[DEV FIXTURE] " label from a product title for CUSTOMER
 * PRESENTATION only — the underlying fixture data in shopify-dev-fixtures.ts
 * keeps its label untouched, so the dev/fixture boundary stays obvious in
 * source code. Every customer-facing template must render titles through
 * this (never `product.title` directly) so no development label reaches a
 * real visitor. A no-op once real Shopify data replaces the fixture
 * provider, since real titles will never carry this prefix.
 */
export function displayProductTitle(title: string): string {
  return title.replace(DEV_FIXTURE_PREFIX, '');
}
