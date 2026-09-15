The real ONE77 domain schema, one numbered file per milestone, applied in order:

- `001_product_enrichment.sql` — `ProductEnrichment`, `ProductSpecifications`, `ProductUseCases`, `ProductCompatiblePowerplants`
- `002_match.sql` — `MatchRelationships`, `MatchRelationshipUseCases`
- `003_bundles.sql` — `Bundles`, `BundleItems`
- `004_learn.sql` — `LearnCategories`, `LearnEntries`
- `005_webinar.sql` — `WebinarEvents`, `WebinarRegistrations`
- `006_admin_users.sql` — `AdminUsers`
- `007_homepage.sql` — `HomepageConfig` (a singleton row — the one admin-controlled homepage content field: the hero photograph reference)

Every file is self-idempotent (`DROP TABLE IF EXISTS` in dependency order,
then `CREATE`), so re-running the full set against a database that already
has this schema is safe. No file has a cross-file foreign key dependency on
another numbered file — the numeric order is a deployment convention, not a
strict constraint requirement, but scripts should still be applied in order.

No development/test/seed data is included in any schema file — these are
DDL only. See `migration/docs/TUSHAR_DEPLOYMENT_HANDOVER.md` for the full
deployment sequence and `scripts/apply_schema.sh` for the deployment runner.

The first six files were verified against a genuinely fresh, empty SQL
Server 2019 database as part of the Phase 3 server-readiness consolidation
milestone (13 tables, 7 foreign keys, applied cleanly from zero).
`007_homepage.sql` was added later and re-verified against that same real
database — the full set now applies cleanly to 14 tables and 7 foreign
keys (`HomepageConfig` adds no foreign key).
