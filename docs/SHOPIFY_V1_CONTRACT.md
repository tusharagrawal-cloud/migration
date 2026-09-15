# ONE77 V1 Shopify Product Reference Contract

Status: contract reference only. **No Shopify integration is implemented by this
document or by this milestone** — no OAuth, no credentials, no live store
connection, no webhooks, no sync, no cart/checkout/order code. This is the rule
Match, Product Enrichment, and Bundles are built against; the actual Shopify
integration layer is a later, dedicated milestone.

## A. Ownership boundary

Per Tushar's binding architecture instruction: *"For e-commerce functionality
we are using Shopify and for rest functionality we will use APIs to store data
on our server."*

**Shopify owns** (commerce/catalog facts — never duplicated into ONE77 SQL):
product identity/catalog, variants, title, handle, SKU, images/media, price,
compare-at price, inventory/availability, cart, checkout, orders, the
commerce lifecycle.

**ONE77 SQL owns** (non-commerce ONE77 functionality): product
specifications/enrichment needed by the ONE77 experience, Match, Bundle
curation, Learn, Webinar, and ONE77 admin configuration. ONE77 SQL must never
become a second, parallel Shopify product catalogue.

## B. Canonical references

- **Product ID**: a GraphQL global ID (GID), structure `gid://shopify/Product/{id}`
  — e.g. `gid://shopify/Product/7522653143105`. This is Shopify's own
  documented format (`shopify.dev/docs/api/usage/gids`): *"a global ID is an
  application-wide URI that uniquely identifies an object"*, following the
  pattern `gid://shopify/{ResourceType}/{ID}`.
- **Variant ID**: same structure, `gid://shopify/ProductVariant/{id}` — e.g.
  `gid://shopify/ProductVariant/1`. Not currently stored anywhere in the V1
  schema (see Section D — Match/Enrichment/Bundles are product-level only).
- **Opaque-string rule**: Shopify's own documentation states this explicitly —
  *"you should always treat the [GraphQL id] string as an opaque ID... there
  is no guarantee that GraphQL IDs will follow [a particular] structure, so
  you shouldn't generate IDs programmatically... applications should treat
  these values as opaque identifiers rather than extracting and depending on
  their numeric portions."* ONE77 follows this rule exactly: the full
  canonical GID string is stored verbatim as returned by whichever Shopify
  API is used for integration; ONE77 code must never parse, reconstruct, or
  depend on the numeric suffix.
- **SQL representation**: `NVARCHAR(64) NOT NULL`, opaque, non-parsed. A
  Product GID is `gid://shopify/Product/` (23 chars) + up to ~20 digits
  (Shopify resource IDs fit comfortably within a 64-bit range) ≈ 43
  characters worst case; `NVARCHAR(64)` gives ~20 characters of headroom
  without resorting to `NVARCHAR(MAX)`. Verified against the live Milestone 2
  schema (see Section H below) — all four existing Shopify reference columns
  already use this exact type.

## C. Routing

- **Shopify's product `handle`** is the customer-facing routing identifier —
  a unique, lowercase, URL-friendly string Shopify auto-generates from the
  product title (e.g. `blue-running-shoes`, used in `/products/{handle}`),
  auto-incremented on collision (`potion`, `potion-1`). It is fetched from
  Shopify at request time by the later integration layer, not stored in
  ONE77 SQL at this stage.
- **Shopify Product ID is ONE77's join key** — every ONE77 SQL table that
  references a product (ProductEnrichment, MatchRelationships, BundleItems)
  keys on the Shopify Product ID, never on a handle or a local identifier.
- **No independent ONE77 product slug system.** Verified: no `Slug` or
  `Handle` column exists on any product-referencing table in the current
  schema (`ProductEnrichment`, `MatchRelationships`, `BundleItems`) — the
  only `Slug` column anywhere in the V1 schema belongs to `LearnCategories`,
  a wholly Shopify-independent domain, and is unrelated to product routing.
  The handle is not cached into SQL "for convenience" at this stage either —
  that would just be a smaller, still-stale parallel catalogue.

## D. V1 granularity

- **Match = product level.** `MatchRelationships.SourceShopifyProductId` /
  `TargetShopifyProductId` reference Shopify Products only — no
  `ShopifyVariantId` column exists or is planned for V1.
- **Product Enrichment = product level.** `ProductEnrichment.ShopifyProductId`
  is the sole Shopify reference; enrichment data (Calibre, PowerplantType,
  WeightGrains, pellet-weight range, and the flexible spec table) is written
  once per product, not per variant.
- **Bundle curation = product level.** `BundleItems.ShopifyProductId`
  references Shopify Products only.
- **Variant-level business logic is deferred**, not designed around, until a
  concrete V1 requirement proves it necessary. Actual cart/checkout variant
  selection is explicitly out of scope here and belongs to the later Shopify
  integration milestone.

## E. Shopify fields expected later by the integration layer

Listed for forward reference only — none of this is implemented yet.

**Product**: `id`, `handle`, `title`, `featuredImage` / `images`, `variants`,
availability (e.g. via variant `availableForSale`), price range.

**Variant**: `id`, `title`, `sku`, `price`, `compareAtPrice`,
`availableForSale`, `selectedOptions`.

The exact fields, API (Admin GraphQL vs. Storefront), and query shapes will
be pinned when the dedicated Shopify integration milestone begins.

---

## Verification appendix — existing schema audit (Milestone 4)

**Shopify reference columns found** (via direct `sys.columns` inspection
against the live `One77` database):

| Table | Column | Type | Max length | Nullable | Complies |
|---|---|---|---|---|---|
| ProductEnrichment | ShopifyProductId | NVARCHAR | 64 chars | NOT NULL | Yes |
| MatchRelationships | SourceShopifyProductId | NVARCHAR | 64 chars | NOT NULL | Yes |
| MatchRelationships | TargetShopifyProductId | NVARCHAR | 64 chars | NOT NULL | Yes |
| BundleItems | ShopifyProductId | NVARCHAR | 64 chars | NOT NULL | Yes |

No `ShopifyVariantId` column exists anywhere in the schema. No product-level
`Slug`/`Handle` column exists anywhere in the schema.

**Commerce-duplication audit** (ProductEnrichment / ProductSpecifications /
ProductCompatiblePowerplants, column by column): no product name/title,
brand/vendor, price, compare-at price, inventory, image URL, description,
checkout, or order field is present. `Category` (airgun/pellet/accessory) is
a ONE77 experience taxonomy for Match resolution, not Shopify's
`product_type`. `IsActive` is a ONE77 Match/Bundle visibility toggle, not a
duplicate of Shopify's commerce lifecycle status. `Calibre`, `PowerplantType`,
`WeightGrains`, `RecommendedPelletWeightMin/Max`, and `ProductSpecifications`'
free-form key/value rows are ONE77-specific technical/compatibility
attributes with no Shopify equivalent. Result: **clean, no corrections
needed.**

**ProductCompatiblePowerplants sanity check**: this table stores exactly one
real ONE77 compatibility fact per row (which powerplants an accessory works
with) — the structural input the Match derived-fallback resolver needs for
set-membership comparison. It has not grown into a master taxonomy/config
subsystem: there is no separate "Powerplants" lookup table, no admin
CRUD API layer, no additional metadata columns. **Kept as-is.**

**Conclusion**: Milestone 2's schema already conforms to this contract in
full. This milestone required no DDL changes.
