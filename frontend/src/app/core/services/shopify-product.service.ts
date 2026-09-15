import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ShopifyProductDetail, ShopifyProductSummary, ShopifyProductVariant } from '../models/shopify-product.model';
import { devFixturesByCategory, findDevFixtureByHandle, findDevFixtureById } from './shopify-dev-fixtures';

/**
 * Real boundary for the Shopify Storefront integration (read-only product
 * data). Calls the Storefront GraphQL API directly from the browser using
 * environment.shopifyStoreDomain / environment.shopifyStorefrontToken — this
 * is Shopify's intended usage of a Storefront token (unlike an Admin API
 * token, it is safe to ship in a public client bundle).
 *
 * Per SHOPIFY_V1_CONTRACT.md: Shopify owns all commerce/catalog facts, so
 * this service must never write anything back to ONE77 SQL, and IDs are
 * always treated as opaque strings — never parsed or reconstructed.
 *
 * ONE77's 'airgun' | 'pellet' | 'accessory' categories are not a native
 * Shopify field. This implementation uses Shopify product **tags**
 * (tag:airgun / tag:pellet / tag:accessory) rather than "Type" or the
 * standardized "Category" field, because on the real store those two
 * fields turned out to be inconsistently filled (some products have
 * Type = "None", others have a single combined value like "Pellets,
 * Optica & Accessories" covering more than one ONE77 category). Tags are
 * freely combinable and fully under the merchant's control, so every
 * product in the store needs the right one of these three tags added
 * manually (Shopify Admin → product → Tags → Add tags). Products with
 * none of the three tags will not appear in any ONE77 category listing.
 *
 * If shopifyStoreDomain/shopifyStorefrontToken are left blank (e.g. a
 * fresh dev checkout with no token supplied yet), every method falls back
 * to shopify-dev-fixtures.ts so the app keeps working out of the box.
 */
@Injectable({ providedIn: 'root' })
export class ShopifyProductService {
  private readonly http = inject(HttpClient);

  private readonly endpoint = environment.shopifyStoreDomain
    ? `https://${environment.shopifyStoreDomain}/api/${environment.shopifyStorefrontApiVersion}/graphql.json`
    : null;

  private readonly isConfigured = !!(environment.shopifyStoreDomain && environment.shopifyStorefrontToken);

  getByCategory(category: ShopifyProductSummary['category']): Observable<ShopifyProductSummary[]> {
    if (!this.isConfigured) {
      return of(devFixturesByCategory(category));
    }

    const query = `
      query ProductsByType($query: String!) {
        products(first: 50, query: $query) {
          edges {
            node {
              id
              handle
              title
              vendor
              tags
              featuredImage { url }
              priceRange { minVariantPrice { amount currencyCode } }
              availableForSale
            }
          }
        }
      }
    `;

    return this.graphql<{ products: { edges: { node: ShopifyRawProduct }[] } }>(query, {
      query: `tag:${category}`,
    }).pipe(
      map((data) => data.products.edges.map((edge) => this.toSummary(edge.node))),
      catchError((err) => {
        console.error(`Shopify getByCategory(${category}) failed, falling back to dev fixtures`, err);
        return of(devFixturesByCategory(category));
      }),
    );
  }

  getByHandle(handle: string): Observable<ShopifyProductDetail | undefined> {
    if (!this.isConfigured) {
      return of(findDevFixtureByHandle(handle));
    }

    const query = `
      query ProductByHandle($handle: String!) {
        productByHandle(handle: $handle) {
          id
          handle
          title
          vendor
          tags
          descriptionHtml
          featuredImage { url }
          images(first: 10) { edges { node { url } } }
          priceRange { minVariantPrice { amount currencyCode } }
          compareAtPriceRange { maxVariantPrice { amount currencyCode } }
          availableForSale
          variants(first: 25) {
            edges {
              node {
                id
                title
                availableForSale
                price { amount currencyCode }
                selectedOptions { name value }
              }
            }
          }
        }
      }
    `;

    return this.graphql<{ productByHandle: ShopifyRawProductDetail | null }>(query, { handle }).pipe(
      map((data) => (data.productByHandle ? this.toDetail(data.productByHandle) : undefined)),
      catchError((err) => {
        console.error(`Shopify getByHandle(${handle}) failed, falling back to dev fixtures`, err);
        return of(findDevFixtureByHandle(handle));
      }),
    );
  }

  getById(id: string): Observable<ShopifyProductDetail | undefined> {
    if (!this.isConfigured) {
      return of(findDevFixtureById(id));
    }

    // `id` is the opaque GID exactly as previously returned by Shopify
    // (e.g. gid://shopify/Product/123...) — passed straight through, never
    // parsed or rebuilt, per the contract's opaque-string rule.
    const query = `
      query ProductById($id: ID!) {
        node(id: $id) {
          ... on Product {
            id
            handle
            title
            vendor
            tags
            descriptionHtml
            featuredImage { url }
            images(first: 10) { edges { node { url } } }
            priceRange { minVariantPrice { amount currencyCode } }
            compareAtPriceRange { maxVariantPrice { amount currencyCode } }
            availableForSale
            variants(first: 25) {
              edges {
                node {
                  id
                  title
                  availableForSale
                  price { amount currencyCode }
                  selectedOptions { name value }
                }
              }
            }
          }
        }
      }
    `;

    return this.graphql<{ node: ShopifyRawProductDetail | null }>(query, { id }).pipe(
      map((data) => (data.node ? this.toDetail(data.node) : undefined)),
      catchError((err) => {
        console.error(`Shopify getById(${id}) failed, falling back to dev fixtures`, err);
        return of(findDevFixtureById(id));
      }),
    );
  }

  // ---- internals ---------------------------------------------------------

  private graphql<T>(query: string, variables: Record<string, unknown>): Observable<T> {
    return this.http
      .post<{ data: T; errors?: { message: string }[] }>(this.endpoint!, { query, variables }, {
        headers: {
          'Content-Type': 'application/json',
          'X-Shopify-Storefront-Access-Token': environment.shopifyStorefrontToken,
        },
      })
      .pipe(
        map((res) => {
          if (res.errors?.length) {
            throw new Error(res.errors.map((e) => e.message).join('; '));
          }
          return res.data;
        }),
      );
  }

  private toSummary(node: ShopifyRawProduct): ShopifyProductSummary {
    return {
      id: node.id,
      handle: node.handle,
      title: node.title,
      vendor: node.vendor || null,
      imageUrl: node.featuredImage?.url ?? null,
      priceLabel: node.priceRange?.minVariantPrice
        ? formatPrice(node.priceRange.minVariantPrice.amount, node.priceRange.minVariantPrice.currencyCode)
        : null,
      availableForSale: node.availableForSale,
      category: categoryFromTags(node.tags),
    };
  }

  private toDetail(node: ShopifyRawProductDetail): ShopifyProductDetail {
    const price = node.priceRange?.minVariantPrice;
    const compareAt = node.compareAtPriceRange?.maxVariantPrice;
    const showCompareAt = !!(price && compareAt && Number(compareAt.amount) > Number(price.amount));
    return {
      ...this.toSummary(node),
      images: node.images?.edges.map((e) => e.node.url) ?? [],
      descriptionHtml: node.descriptionHtml ?? null,
      compareAtPriceLabel: showCompareAt ? formatPrice(compareAt!.amount, compareAt!.currencyCode) : null,
      variants: node.variants?.edges.map((e): ShopifyProductVariant => ({
        id: e.node.id,
        title: e.node.title,
        availableForSale: e.node.availableForSale,
        priceLabel: e.node.price ? formatPrice(e.node.price.amount, e.node.price.currencyCode) : null,
        selectedOptions: e.node.selectedOptions,
      })) ?? [],
    };
  }
}

// ---- Storefront API response shapes (subset actually used) ---------------

interface ShopifyRawProduct {
  id: string;
  handle: string;
  title: string;
  vendor: string;
  tags: string[];
  featuredImage: { url: string } | null;
  priceRange: { minVariantPrice: { amount: string; currencyCode: string } } | null;
  availableForSale: boolean;
}

interface ShopifyRawProductDetail extends ShopifyRawProduct {
  descriptionHtml: string | null;
  images: { edges: { node: { url: string } }[] } | null;
  compareAtPriceRange: { maxVariantPrice: { amount: string; currencyCode: string } | null } | null;
  variants: {
    edges: {
      node: {
        id: string;
        title: string;
        availableForSale: boolean;
        price: { amount: string; currencyCode: string } | null;
        selectedOptions: { name: string; value: string }[];
      };
    }[];
  } | null;
}

function formatPrice(amount: string, currencyCode: string): string {
  const value = Number(amount);
  if (Number.isNaN(value)) return amount;
  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency: currencyCode }).format(value);
  } catch {
    return `${amount} ${currencyCode}`;
  }
}

// ---- ONE77 category <-> Shopify tag mapping -------------------------------
//
// Every product in the store needs exactly one of these three tags added
// in Shopify Admin (product page → Tags → Add tags): airgun, pellet,
// accessory. If a product has none of the three, it falls back to
// 'accessory' with a console.warn so the gap is visible in dev tools
// instead of silently misclassifying.
function categoryFromTags(tags: string[] | undefined): ShopifyProductSummary['category'] {
  const lower = (tags ?? []).map((t) => t.toLowerCase());
  if (lower.includes('airgun')) return 'airgun';
  if (lower.includes('pellet')) return 'pellet';
  if (lower.includes('accessory')) return 'accessory';
  console.warn(
    `Shopify product has none of the tags airgun/pellet/accessory (tags: [${(tags ?? []).join(', ')}]) — defaulting to 'accessory'. Add the right tag in Shopify Admin.`,
  );
  return 'accessory';
}