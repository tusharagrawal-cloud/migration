# ONE77 Match — Migration Parity Report (Milestone 6)

Ground truth for this report is the actual source, read directly, not a
summary: `backend/match_routes.py`, `backend/models.py`
(`to_storefront_airgun`/`to_storefront_pellet`/`to_storefront_accessory`),
and `backend/tests/test_match_phase3.py`. Where the code and any earlier
Phase 1/2 summary disagreed, the code won.

## Relationship status mapping

Stored literally, lowercase/underscored, exactly as the reference system:
`compatible`, `recommended`, `not_recommended`. No new status values, no
translation layer.

## Priority behavior

Nullable; meaningful only when `Status = recommended`. 1 = Best Match,
2 = Recommended, 3 = Alternative — derived labels, never stored.

**Two response buckets exist for recommended items, not three**:
`best_match_*` (priority 1 only) and `recommended_*` (priority 2 **and** 3
combined, ordered priority-ascending). There is no separate "alternative"
bucket in the response shape — only the per-item `priority_label` field
distinguishes Recommended from Alternative within the shared bucket.

**Upsert priority normalization (silent, not a rejection)**: the create/
upsert endpoint mirrors `priority = body.priority or 2; if status !=
recommended: priority = None elif priority not in (1,2,3): priority = 2` —
any missing or out-of-range value is silently normalized to 2; this
endpoint never returns 400 for a bad priority.

**PATCH priority validation (a documented improvement)**: the reference
PATCH endpoint only forces `priority = None` when `status` is changed away
from `recommended` *in the same call* — it otherwise never validates
Priority at all, which would let an invalid value reach our SQL CHECK
constraint as a raw, unhandled 500. The new PATCH validates Priority on
every call (range 1-3, and only when the effective status is `recommended`)
and returns a clean 400 instead. This is a fix to a gap our stricter schema
exposes, not a new business rule — documented per Section 6.

## Curated-vs-derived rule

Per target category (pellet, accessory) **independently**: if any active,
non-`not_recommended` curated relationship exists for that category, use
curated results only for that category; otherwise use the derived fallback
for that category. Curated and derived are never mixed within one category.

**`reason_source` is response-level, not per-category**: it is `"curated"`
if *either* category has curated data (even if the other category fell
back to derived), else `"derived"`. Verified explicitly by fixtures F and G
below — this is easy to get wrong and is exactly what the old code does.

## not_recommended behavior

A real, stored, soft-deletable admin state. Excluded from public results
entirely (never counted as curated, never triggers/blocks derived fallback
for that specific relationship — the derived algorithm doesn't consult
relationship rows at all, so a `not_recommended` target can still appear
via derived fallback once no curated rows remain for that category; this
was confirmed live, see Section Q "Live HTTP verification" below). Excluded
from completeness counts. Remains fully visible/manageable via the admin
relationship list and PATCH/DELETE.

## Completeness rule (computed, never stored)

- Pellets: complete requires ≥1 compatible-or-better **and** ≥1 recommended.
- Accessories: complete requires only ≥1 compatible-or-better.
- `no_matches` when zero non-`not_recommended` relationships exist at all.
- `complete` when both category requirements are met; `needs_review` otherwise.

## Calibre mismatch / override rule

Only ever applies when the target is a **pellet** (never accessory).
Missing calibre on either side means **no mismatch detected** — the guard
passes with no default applied. This is deliberately different from the
public derived pellet resolver, which defaults missing calibre to `4.5mm`
on both sides before comparing — two different rules for two different call
sites, both preserved exactly as read from the source.

`calibre_override` bypasses the guard on the single-relationship
upsert/create endpoint only. **Bulk mark has no override option at all** —
`BulkMark` never had a `calibre_override` field in the reference system, so
a calibre-mismatched target under `status=recommended` is always silently
skipped (counted in `skipped_calibre`) in bulk, with no way to force it.

## Soft-delete / remove+re-add rule

DELETE always soft-deletes (`IsActive = 0`); the row is never physically
removed.

**Remove-then-re-add reactivates the *same* row, it does not create a
second one.** The reference system's upsert looks up an existing
relationship for a `(source, target)` pair with **no active filter**
(`find_one` regardless of status) and updates that document in place,
including flipping `active` back to `true`. The new implementation mirrors
this exactly (`FindRelationshipIdByPairAsync` — any status — then
update-in-place or insert). Confirmed with the same relationship `Id`
before and after a delete+re-add cycle (fixture P, and live via HTTP).

Separately, Milestone 2's `UQ_MatchRelationships_ActivePair` filtered
unique index still exists and is still exercised by the Milestone 2 schema
tests — it protects the invariant defensively even though the app-level
upsert flow, as implemented, never actually needs to rely on it to avoid a
collision (it always reuses the existing row for a pair rather than
inserting a fresh one alongside an inactive one).

## Product-level Shopify GID behavior

Match is product-level only in V1: `SourceShopifyProductId`/
`TargetShopifyProductId` reference Shopify Products, never Variants. IDs
are treated as opaque strings throughout — never parsed, never assumed
numeric — per `SHOPIFY_V1_CONTRACT.md`. Confirmed end-to-end with fake
Shopify-format GIDs (`gid://shopify/Product/...`) in both automated tests
and live HTTP verification; the source/target GID strings round-trip
byte-for-byte through every response.

## Milestone 2 schema correction: ProductUseCases

Reading the actual derived pellet fallback surfaced a real gap: it gates on
a pellet's single `use_case` tag being present in the airgun's own
(0-2-valued) `match_use_cases` list — a fact Milestone 2's schema didn't
yet capture. Added one small child table, `ProductUseCases
(ProductEnrichmentId, UseCase)`, mirroring the existing
`ProductCompatiblePowerplants` pattern exactly (no CHECK constraint, no
taxonomy system — a plain relationship-owned tag). Applied and verified
against the real SQL Server 2019 instance (idempotent re-apply confirmed);
covered by new schema tests
(`ProductEnrichmentSchemaTests.CanAttach_MultipleUseCases`,
`OrphanUseCase_IsRejectedByForeignKey`) plus every Match fixture that
exercises the derived pellet path. No other Milestone 2 DDL changed.

## Old endpoint → new endpoint map

| Old (FastAPI) | New (.NET dev host) |
|---|---|
| `GET /api/airguns/{airgun_id}/matches?use_case=` | `GET /api/products/matches?shopify_product_id=&use_case=` |
| `GET /api/admin/match/airguns` | `GET /api/admin/match/airguns` |
| `GET /api/admin/match/{airgun_id}/relationships?target_category=` | `GET /api/admin/match/relationships?shopify_product_id=&target_category=` |
| `GET /api/admin/match/{airgun_id}/candidates?target_category=&...` | `GET /api/admin/match/candidates?shopify_product_id=&target_category=&...` |
| `POST /api/admin/match/relationships` | `POST /api/admin/match/relationships` |
| `PATCH /api/admin/match/relationships/{rid}` | `PATCH /api/admin/match/relationships/{id}` |
| `DELETE /api/admin/match/relationships/{rid}` | `DELETE /api/admin/match/relationships/{id}` |
| `POST /api/admin/match/bulk` | `POST /api/admin/match/bulk` |
| `GET /api/admin/match/{airgun_id}/summary` | `GET /api/admin/match/summary?shopify_product_id=` |

Every Shopify Product ID moved from a path segment to a query parameter —
see "Bug found and fixed" below for why.

Field naming: request/response bodies use `shopify_product_id`/
`source_shopify_product_id`/`target_shopify_product_id` (not the old
system's bare `product_id`), matching this migration's own SQL column
names and the Milestone 4 contract's terminology. A deliberate naming
clarification, not a behavioral change.

## Parity test scenarios (A-S)

All 19 fixtures live in
`migration/backend/tests/One77.Core.Tests/Match/MatchParityFixtureTests.cs`,
each with an inline INPUT / OLD EXPECTED / NEW ACTUAL comment. All 19
passed, genuinely executed against the real SQL Server 2019 `One77`
database.

| # | Scenario | Result |
|---|---|---|
| A | Curated pellets only | PASS |
| B | Curated accessories only | PASS |
| C | Curated both | PASS |
| D | No curated pellets → derived pellets (calibre + use_case + weight range, sorted by weight) | PASS |
| E | No curated accessories → derived accessories (powerplant compatibility) | PASS |
| F | Curated pellets + derived accessories (response-level `reason_source="curated"`) | PASS |
| G | Derived pellets + curated accessories (same response-level rule, other direction) | PASS |
| H | not_recommended target excluded | PASS |
| I | recommended priority 1 → Best Match | PASS |
| J | recommended priority 2 → Recommended | PASS |
| K | recommended priority 3 → Alternative | PASS |
| L | compatible | PASS |
| M | calibre mismatch blocked (recommended, no override) | PASS |
| N | calibre_override accepted | PASS |
| O | soft-delete relationship | PASS |
| P | remove + re-add (same row reactivated) | PASS |
| Q | completeness = complete | PASS |
| R | completeness = needs_review | PASS |
| S | completeness = no_matches | PASS |

## Intentional differences (approved simplifications, not defects)

- **Target commerce-lifecycle filter deferred.** The reference system
  filters public Match targets by `status=published, active=true` sourced
  from the local Mongo products collection — a Shopify-owned commerce
  field in the new architecture that isn't cached locally by design (see
  `SHOPIFY_V1_CONTRACT.md`). The new system substitutes ONE77's own
  `ProductEnrichment.IsActive` toggle for this purpose. Live Shopify
  availability filtering is deferred to the later Shopify Product Detail
  merge milestone.
- **Candidate/airgun search has no name/brand text search.** The old
  admin candidate and airgun-list endpoints support a free-text `q` filter
  against product name/brand — fields Shopify now owns and this milestone
  doesn't cache locally. Omitted for V1 rather than faked; the later
  Shopify merge milestone can add name-aware search once product data is
  actually available to the API layer.
- **Airgun-list sort order.** Old: most-recently-updated product first
  (`updated_at desc` on the full product document). New: ordered by
  Shopify Product ID — a stable, deterministic substitute with no
  behavioral significance to Match's actual logic; cosmetic only.
- **Derived accessory ordering.** Old: unspecified/incidental Mongo
  iteration order (no explicit sort). New: ordered by Shopify Product ID —
  deterministic, cosmetic only.
- **`match_reason` use-case phrasing.** Old interpolates a Python list
  directly into an f-string for the derived-branch reason text
  (`"...use case ['target_10m', 'plinking']."`), an accidental formatting
  artifact rather than a deliberate design choice. New joins the same
  values with `", "` for a clean sentence. Cosmetic only — the `reason`
  values on individual derived cards ("Derived from calibre and weight
  compatibility." / "Derived from category compatibility.") are preserved
  verbatim.
- **DELETE on a missing relationship now returns 404** instead of the old
  system's silent no-op (Mongo `update_one` on a non-existent id succeeds
  trivially). A minor, documented hygiene improvement consistent with the
  Learn/Webinar precedent already established in this migration.
- **PATCH priority validation** — see "Priority behavior" above.

## Bug found and fixed: Shopify GIDs cannot be path segments

Discovered during mandatory live HTTP verification (Section 22), not by
the 154 automated tests — those call the service layer directly and never
exercise real ASP.NET Core routing. A canonical Shopify Product GID
(`gid://shopify/Product/123...`) contains three literal `/` characters.
The initial design put it directly in a route template
(`/api/admin/match/{shopifyProductId}/summary`). Over real HTTP, even with
the value properly percent-encoded by the client, ASP.NET Core's router
did not reliably decode `%2F` back to `/` when binding a `{routeParam}`
path segment — the request returned `200` with an empty/wrong result
instead of finding the real data, which is worse than an outright 404
because it fails silently. Confirmed by direct comparison: a slash-free
test identifier round-tripped correctly through the same code path; a
real-shaped GID did not.

**Fix**: every Shopify Product ID moved from a path segment to a
`shopify_product_id` query parameter across all five affected endpoints
(public matches, admin relationships/candidates/summary — the relationship
PATCH/DELETE endpoints were never affected, since they key on the local
integer relationship ID, never a GID). Query-string parsing has no such
restriction. Re-verified end-to-end over live HTTP after the fix — full
curated + derived + not_recommended + soft-delete + completeness flow all
confirmed correct with real percent-encoded GIDs.

## Behavior that could not be verified

None. Every rule enumerated above was verified either by a real SQL Server
2019 integration test, live HTTP verification, or both.
