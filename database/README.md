# ONE77 Database — SQL Server 2019

This folder holds plain T-SQL for the new ONE77 migration target. No ORM-generated migrations, no SQL Server features newer than 2019, no database deployment framework.

- `schema/` — the real ONE77 domain schema: `ProductEnrichment`, `Match`, `Bundles`, `Learn`, `Webinar`, `AdminUsers` (see `schema/README.md` for the full ordered file list). 13 tables, 7 foreign keys total.
- `scripts/` — small, standalone operational scripts. `000_connectivity_check.sql` is a Milestone-1-only smoke test, not part of the domain schema; safe to run against any empty database. `apply_schema.sh` applies every numbered schema file in order against a target database — this is the deployment runner; see its own header comment for usage.

No production credentials live in this folder or anywhere in this repository. Connection strings are supplied by the consuming host's configuration/environment, never committed.

For the full deployment sequence (create database → apply schema → configure the API → first-admin bootstrap), see `migration/docs/TUSHAR_DEPLOYMENT_HANDOVER.md`.
