# ONE77 Admin V1 — Usability Contract

Status: a UX contract for the future Angular Admin, not an implementation.
No UI is built by this document or this milestone.

## The principle

Future Admin screens speak the business owner's language, not the
database's. A normal ONE77 staff member — not a programmer, not a database
administrator — must be able to manage products, matches, and bundles
without ever seeing what powers them underneath.

## Do not show

| Technical term | Because |
|---|---|
| `ShopifyProductId` / any GID | Meaningless to a merchandiser — they think "this rifle," not a string starting `gid://` |
| `ProductEnrichment` / `ProductSpecifications` / `ProductUseCases` / `ProductCompatiblePowerplants` | Four SQL tables that are one concept to the user: "this product's ONE77 details" |
| `MatchRelationshipId` / any surrogate `Id` | An internal row number, not something a person reasons about |
| "Foreign Key" / "Status enum" / "CHECK constraint" | Database vocabulary, not business vocabulary |
| Raw JSON | Editing a document is a developer task, not a merchandiser task |

## Prefer instead

| Business language | Replaces |
|---|---|
| "Product" | ShopifyProductId (shown as the product's name/photo once Shopify integration exists — an ID is never the primary way a person identifies a product) |
| "ONE77 Details" | ProductEnrichment + its child tables, presented as one edit form |
| "Specifications" | ProductSpecifications rows, shown as a simple list of label/value pairs |
| "Matches" | MatchRelationships — framed as "what pellets/accessories go with this airgun," not "relationships" |
| "Bundle" | Unchanged — already a plain word |
| "Show on Website" / "Hide from Website" | Publish / Archive — a toggle or a clear button, not a status dropdown showing "Draft/Published/Archived" as raw strings |

## What the future screens should feel like

- **Choose Product → Edit ONE77 Details → Save.** One screen, one save
  action — not four separate table-management screens for Enrichment,
  Specifications, Use Cases, and Compatible Powerplants. This milestone's
  `EnrichmentRecord`/`EnrichmentService` already assemble exactly this
  shape server-side, specifically so the future Admin doesn't have to
  orchestrate multiple API calls to show one edit form.
- **Create Bundle → Select Products → Arrange Products → Save.** Item order
  in the bundle is just the order the admin arranges them in — the API
  assigns `SortOrder` from array position, so the future UI is a plain
  reorderable list (e.g. drag-and-drop), never a numeric field the admin
  has to type into.
- Product selection for both Enrichment and Bundles can reuse the same
  "pick a product" control — `GET /api/admin/enrichment` already returns
  every known product, so there is no need for a second, Bundle-specific
  product-search endpoint.

## Interaction patterns to use

- Human-readable labels on every field.
- Dropdowns for closed vocabularies (Category: Airgun/Pellet/Accessory;
  Status: Draft/Published/Archived) — never a free-text field for a value
  the schema constrains to a fixed set.
- Toggles for boolean states ("Show on Website").
- Search/select controls for choosing a product — never require typing or
  pasting a Shopify ID by hand.
- Plain Save/Cancel actions.
- Validation messages in plain language ("Please choose a primary
  product for this bundle," not "primary_count must equal 1").
- A confirmation step before Archive/Hide — it's reversible in this system
  (Unarchive returns to Draft, never straight back to Published — the
  admin always reviews before re-publishing), but the action should still
  read as deliberate, not accidental.

## Avoid

- Raw ID fields where a name/search control will do once Shopify data is
  available.
- JSON editing of any kind.
- Technical configuration forms (no "edit the specification array" UI —
  specifications are a simple key/value list with add/remove rows).
- Developer terminology anywhere in copy, labels, or error messages.
- Any assumption that the admin understands what a "Shopify Product ID,"
  "GraphQL GID," or "opaque identifier" is — that vocabulary belongs to
  `SHOPIFY_V1_CONTRACT.md` and this migration's own code, never to a
  business user's screen.
