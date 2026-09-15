/**
 * DEVELOPMENT FIXTURES ONLY — NOT REAL SHOPIFY DATA.
 *
 * This file exists solely so the storefront foundation has something to
 * render before the real Shopify integration milestone lands. Every record
 * below is obviously placeholder content: no fabricated prices (priceLabel
 * is always null here — components must render a "price via Shopify"-style
 * placeholder, never a made-up number), no real product photography, and
 * titles/vendors that read as demo content rather than real inventory.
 *
 * Fixture variants are deliberately unavailableForSale so Add to Cart stays
 * disabled in fixture mode rather than silently pretending to add a fake
 * product to a real Shopify cart.
 *
 * This module is imported ONLY by ShopifyProductService below. No other
 * file should import from here directly — that keeps the swap to a real
 * Shopify adapter a one-file change.
 */
import { ShopifyProductDetail, ShopifyProductSummary, ShopifyProductVariant } from '../models/shopify-product.model';

function fixtureVariant(id: string): ShopifyProductVariant[] {
  return [
    {
      id: `gid://shopify/ProductVariant/${id}`,
      title: 'Default Title',
      availableForSale: false,
      priceLabel: null,
      selectedOptions: [],
    },
  ];
}

export const SHOPIFY_DEV_FIXTURE_PRODUCTS: ShopifyProductDetail[] = [
  {
    id: 'gid://shopify/Product/DEV-FIXTURE-1001',
    handle: 'dev-fixture-air-rifle-one',
    title: '[DEV FIXTURE] Sample Air Rifle One',
    vendor: 'Sample Vendor',
    imageUrl: null,
    priceLabel: null,
    availableForSale: true,
    category: 'airgun',
    images: [],
    descriptionHtml: null,
    compareAtPriceLabel: null,
    variants: fixtureVariant('DEV-1001'),
  },
  {
    id: 'gid://shopify/Product/DEV-FIXTURE-1002',
    handle: 'dev-fixture-air-rifle-two',
    title: '[DEV FIXTURE] Sample Air Rifle Two',
    vendor: 'Sample Vendor',
    imageUrl: null,
    priceLabel: null,
    availableForSale: true,
    category: 'airgun',
    images: [],
    descriptionHtml: null,
    compareAtPriceLabel: null,
    variants: fixtureVariant('DEV-1002'),
  },
  {
    id: 'gid://shopify/Product/DEV-FIXTURE-1003',
    handle: 'dev-fixture-air-rifle-three',
    title: '[DEV FIXTURE] Sample Air Rifle Three',
    vendor: 'Sample Vendor Two',
    imageUrl: null,
    priceLabel: null,
    availableForSale: true,
    category: 'airgun',
    images: [],
    descriptionHtml: null,
    compareAtPriceLabel: null,
    variants: fixtureVariant('DEV-1003'),
  },
  {
    id: 'gid://shopify/Product/DEV-FIXTURE-2001',
    handle: 'dev-fixture-pellet-tin-one',
    title: '[DEV FIXTURE] Sample Pellet Tin One',
    vendor: 'Sample Vendor',
    imageUrl: null,
    priceLabel: null,
    availableForSale: true,
    category: 'pellet',
    images: [],
    descriptionHtml: null,
    compareAtPriceLabel: null,
    variants: fixtureVariant('DEV-2001'),
  },
  {
    id: 'gid://shopify/Product/DEV-FIXTURE-2002',
    handle: 'dev-fixture-pellet-tin-two',
    title: '[DEV FIXTURE] Sample Pellet Tin Two',
    vendor: 'Sample Vendor Two',
    imageUrl: null,
    priceLabel: null,
    availableForSale: true,
    category: 'pellet',
    images: [],
    descriptionHtml: null,
    compareAtPriceLabel: null,
    variants: fixtureVariant('DEV-2002'),
  },
  {
    id: 'gid://shopify/Product/DEV-FIXTURE-2003',
    handle: 'dev-fixture-pellet-tin-three',
    title: '[DEV FIXTURE] Sample Pellet Tin Three',
    vendor: 'Sample Vendor',
    imageUrl: null,
    priceLabel: null,
    availableForSale: true,
    category: 'pellet',
    images: [],
    descriptionHtml: null,
    compareAtPriceLabel: null,
    variants: fixtureVariant('DEV-2003'),
  },
  {
    id: 'gid://shopify/Product/DEV-FIXTURE-3001',
    handle: 'dev-fixture-scope-one',
    title: '[DEV FIXTURE] Sample Scope',
    vendor: 'Sample Vendor',
    imageUrl: null,
    priceLabel: null,
    availableForSale: false,
    category: 'accessory',
    images: [],
    descriptionHtml: null,
    compareAtPriceLabel: null,
    variants: fixtureVariant('DEV-3001'),
  },
  {
    id: 'gid://shopify/Product/DEV-FIXTURE-3002',
    handle: 'dev-fixture-sling-one',
    title: '[DEV FIXTURE] Sample Sling',
    vendor: 'Sample Vendor Two',
    imageUrl: null,
    priceLabel: null,
    availableForSale: true,
    category: 'accessory',
    images: [],
    descriptionHtml: null,
    compareAtPriceLabel: null,
    variants: fixtureVariant('DEV-3002'),
  },
  {
    id: 'gid://shopify/Product/DEV-FIXTURE-3003',
    handle: 'dev-fixture-case-one',
    title: '[DEV FIXTURE] Sample Case',
    vendor: 'Sample Vendor',
    imageUrl: null,
    priceLabel: null,
    availableForSale: true,
    category: 'accessory',
    images: [],
    descriptionHtml: null,
    compareAtPriceLabel: null,
    variants: fixtureVariant('DEV-3003'),
  },
];

export function findDevFixtureByHandle(handle: string): ShopifyProductDetail | undefined {
  return SHOPIFY_DEV_FIXTURE_PRODUCTS.find((p) => p.handle === handle);
}

export function findDevFixtureById(id: string): ShopifyProductDetail | undefined {
  return SHOPIFY_DEV_FIXTURE_PRODUCTS.find((p) => p.id === id);
}

export function devFixturesByCategory(category: ShopifyProductSummary['category']): ShopifyProductSummary[] {
  return SHOPIFY_DEV_FIXTURE_PRODUCTS.filter((p) => p.category === category);
}