# Admin Shopify Product Picker — Readiness for Real Shopify

## What this document is

Milestone 11 built the Angular Admin's product-selection UX (Products list,
Match Admin candidate search, Bundle Primary/Pellets/Accessories pickers)
entirely against the dev-fixture Shopify boundary established in Milestone 8.
No live Shopify integration exists yet, by design — Milestone 11's own scope
explicitly excludes it.

This document names the exact, single swap point a later milestone must
change to move the whole Admin from fixture data to real Shopify data, and
confirms that no Admin screen or component requires any change to make that
swap.

## The swap point: one class, one file

**File:** `migration/frontend/src/app/core/services/shopify-product.service.ts`
**Class:** `ShopifyProductService`

```typescript
@Injectable({ providedIn: 'root' })
export class ShopifyProductService {
  getByCategory(category: ShopifyProductSummary['category']): Observable<ShopifyProductSummary[]>
  getByHandle(handle: string): Observable<ShopifyProductDetail | undefined>
  getById(id: string): Observable<ShopifyProductDetail | undefined>
}
```

Today, all three methods resolve synchronously from
`migration/frontend/src/app/core/services/shopify-dev-fixtures.ts` (9
`[DEV FIXTURE]`-labeled products: 3 airguns, 3 pellets, 3 accessories — no
real prices, no real inventory, no real images).

A real integration replaces the **body** of these three methods with calls
into Shopify (Storefront API, Admin API, or a thin backend proxy — an
implementation decision for that later milestone) that return the same
`ShopifyProductSummary[]` / `ShopifyProductDetail | undefined` shapes. The
class name, its three method signatures, and its `providedIn: 'root'`
registration do not need to change. Every consumer is injected against this
class, never against the fixture file directly.

## Everything in Admin that depends on this boundary

All of the following call only `ShopifyProductService` — none of them import
`shopify-dev-fixtures.ts` directly, and none of them hold Shopify-shaped
assumptions beyond the three method signatures above:

| Consumer | Methods used | Purpose |
|---|---|---|
| `admin/shared/shopify-product-picker.ts` (`ShopifyProductPicker`) | `getByCategory` | The one shared "find a product by name" UI used by Bundle editor (Primary/Pellets/Accessories) and available for reuse anywhere else a picker is needed |
| `admin/features/products/admin-products-list-page.ts` | `getByCategory` (×3, via `forkJoin`) | Lists all products across the three categories, cross-referenced against ONE77 Enrichment records |
| `admin/features/products/admin-product-detail-page.ts` | `getByHandle` | Resolves the product being edited from its route handle |
| `admin/features/match/admin-match-list-page.ts` | `getById` | Resolves each airgun's display name for the Match list (never shows the raw Shopify ID) |
| `admin/features/match/admin-match-editor-page.ts` | `getById` | Resolves the airgun's own name plus every candidate/relationship target's name |

The public storefront (`ProductListingPage`, `ProductDetailPage`,
`BundlesPage`, etc., all pre-existing from Milestone 8/9) depends on this
same service and is out of scope for this document, but is swapped by the
identical mechanism.

## Why the swap requires no Admin component changes

- Every consumer receives `ShopifyProductSummary` / `ShopifyProductDetail`
  objects — never a raw fixture record — so as long as the real adapter
  returns objects satisfying those same TypeScript interfaces
  (`migration/frontend/src/app/core/models/shopify-product.model.ts`), no
  consumer's template or logic needs to change.
- No Admin component reads `shopify-dev-fixtures.ts` directly or imports
  `SHOPIFY_DEV_FIXTURE_PRODUCTS` outside of test files (specs use the fixture
  export directly to build realistic assertions — that is a test-only
  concern and does not affect production code).
- No Admin component assumes synchronous resolution — every call site
  already treats the service as asynchronous (`Observable`), a network-backed
  Shopify call fits the exact same call sites without restructuring.
- IDs are already treated as opaque strings everywhere (per the
  Shopify-GID-as-query-param rule established in Milestone 6/10) — no
  component parses, reformats, or assumes a particular ID shape.

## What the swap will still need to address (not an Admin change, a new-adapter concern)

These are genuinely new work for whichever milestone implements the real
Shopify integration — noted here so they are not mistaken for "already
solved" by this document:

- Real Shopify auth/credentials (Storefront or Admin API token), and where
  those live in configuration (this repo's established pattern: template
  values only committed, real values supplied by the deployer — see
  `PRODUCTION_HOST_V1.md`).
- Real search semantics: the fixture's `getByCategory`/`getByHandle`/`getById`
  are simple in-memory lookups; a real adapter will need to decide how
  Shopify's own search/filtering maps onto `getByCategory`'s category
  argument (e.g., a Shopify product tag or collection convention for
  airgun/pellet/accessory).
- Rate limiting / caching considerations for a live external API, which the
  fixture obviously never needed.
- Confirming `ShopifyProductSummary`/`ShopifyProductDetail`'s current fields
  are sufficient, or extending them, without ever adding commerce fields
  (price, inventory, checkout) into ONE77's own SQL Server — the boundary
  rule established since Milestone 6 remains binding: Shopify owns all
  commerce data, ONE77 SQL owns only non-commerce enrichment/curation data.

None of the above requires touching any file under `admin/features/`,
`admin/shared/`, or `admin/layout/`.

## Conclusion

The Shopify product-picker abstraction is ready: the swap from dev fixtures
to real Shopify data is confined to the implementation of the three methods
inside `ShopifyProductService`. No Admin screen, no Bundle/Match/Products
component, and no shared picker UI requires modification for that swap to
take effect.
