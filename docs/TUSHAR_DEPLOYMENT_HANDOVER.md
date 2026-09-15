# ONE77 Deployment Handover

Written for someone who already knows Windows/IIS/SQL Server — this covers
only what's ONE77-specific, not general IIS operation.

## System shape

```
Angular (production build, static files)
        +
ASP.NET Web API 2 / .NET Framework 4.8, IIS-hosted
        +
SQL Server 2019
        +
Shopify — not connected yet (deliberately; see "What's intentionally not
          connected" below)
```

Business logic and data access live in `One77.Core` (netstandard2.0),
shared unchanged by both API hosts in this repo. Only one of those hosts is
your production target:

| | `One77.Api.WebApi48` | `One77.Api.DevHost` |
|---|---|---|
| Runtime | .NET Framework 4.8, ASP.NET Web API 2 | .NET 8, ASP.NET Core minimal APIs |
| Purpose | **Production — deploy this one** | Development/local-testing only |
| Where it's proven | Compiles here; IIS execution unverified until you run the smoke test | Fully runtime-exercised in this Linux sandbox throughout development |

**Never deploy `One77.Api.DevHost` as production.** It exists only so this
sandbox (no Windows/IIS/Mono available) could runtime-test the Angular
Admin against a real host during development. See "Source-only vs.
production artifacts" below for the complete list of what not to ship.

## Deployment order

1. Create/configure the SQL Server 2019 database
2. Apply the schema
3. Configure `Web.config` for production
4. Deploy and start `One77.Api.WebApi48`
5. Run the IIS smoke test (`IIS_FIRST_DEPLOY_SMOKE_TEST.md`)
6. Build and deploy Angular
7. Configure the SPA rewrite (already in `Web.config` if you follow the
   recommended structure below — confirm it's intact)
8. Configure CORS / connect the domain
9. Test Admin login end-to-end through the real domain
10. Confirm the domain is live and serving real traffic
11. **Then** Shopify integration (separate, later milestone)
12. **Then** payment gateway configuration (separate, later milestone)
13. Final live UAT once Shopify/payment are connected

Steps 1–10 are what this handover and this milestone prepare. Steps 11–13
are explicitly out of scope here — see below.

### 1–2. Database

```bash
# from migration/database/scripts/, against your real SQL Server:
ONE77_SQL_HOST=<your-host> ONE77_SQL_USER=<your-user> ONE77_SQL_PASSWORD=<your-password> \
  ./apply_schema.sh <your-database-name>
```

Applies all 7 numbered schema files in order
(`migration/database/schema/001`–`007`) — `ProductEnrichment`, `Match`,
`Bundles`, `Learn`, `Webinar`, `AdminUsers`, `HomepageConfig`. Every file is
idempotent (`DROP TABLE IF EXISTS` then `CREATE`), so re-running is safe. No
development/seed data is included — DDL only. The first six of these were
proven against a genuinely fresh, empty SQL Server 2019 database as part of
an earlier milestone (13 tables, 7 foreign keys, applied cleanly from
zero); `007_homepage.sql` was applied and exercised against that same
real database as part of this milestone.

If your environment doesn't have a Linux shell handy, the script is a thin
wrapper: apply the same 6 `.sql` files, in numeric order, with any SQL
client (`sqlcmd`, SSMS, Azure Data Studio).

### 3. Configure `Web.config`

See `PRODUCTION_CONFIGURATION.md` for the complete list of what to fill in.
Minimum: connection string, JWT signing secret, first-admin email/password,
CORS origin.

### 4. Deploy and start `One77.Api.WebApi48`

**Build:**
```bash
cd migration/backend
dotnet publish src/One77.Api.WebApi48/One77.Api.WebApi48.csproj -c Release -o <publish-output>
```

This produces the compiled assemblies (`One77.Api.WebApi48.dll`,
`One77.Core.dll`, and all NuGet dependencies) as a **flat folder** — it does
**not** copy `Web.config` or `Global.asax` (the SDK-style project doesn't
treat this class-library-shaped project as a classic ASP.NET web
project for publish purposes). Assemble the actual IIS site yourself:

```
<site physical root>/
    Web.config              <- from source, with real values filled in
    Global.asax             <- from source, copied as-is (unchanged)
    bin/
        One77.Api.WebApi48.dll
        One77.Core.dll
        ...(every other .dll from the publish output)
```

Point an IIS site (or Application) at `<site physical root>`, with an
Application Pool set to **.NET CLR Version v4.0** (this is the correct
label for .NET Framework 4.8), **Integrated** pipeline mode.

### 5. Smoke test

Run through `IIS_FIRST_DEPLOY_SMOKE_TEST.md` in full before moving on.
This is the step that turns "compiles" into "actually works" — nothing
about the real IIS/System.Web/`Global.asax` request pipeline has been
runtime-verified anywhere but here.

### 6–7. Build and deploy Angular; SPA rewrite

**Build:**
```bash
cd migration/frontend
npm ci
npm run build
```

Default configuration is `production` (confirmed in `angular.json`) — no
flag needed. Output: `dist/one77-storefront/browser/` — copy the contents
of that folder (not the folder itself) into the site.

**IIS hosting structure — the recommendation, and why:**

Every controller in `One77.Api.WebApi48` already has `api` (or `api/admin`)
baked into its own `[RoutePrefix]`, exactly matching `One77.Api.DevHost`'s
route contract (e.g. the real route is `api/learn/categories`, not
`learn/categories`). That's deliberate — it's what let this sandbox verify
API parity between the two hosts throughout development.

This has one consequence for IIS: if you mount `One77.Api.WebApi48` as a
**nested IIS Application** at virtual path `/api`, ASP.NET routing inside
that app resolves paths *relative to the app's own root* — so a request to
`/api/learn/categories` would need to match the internal route
`learn/categories`, not `api/learn/categories`, and would 404. Nesting at
`/api` would require stripping `api` from every `[RoutePrefix]` — a real
code change, not a deployment-config change.

**The simpler, zero-code-change structure — recommended:** one IIS site,
one Application, one physical root, containing **both** the Angular
`browser/` build output and `One77.Api.WebApi48`'s deployment layout
(step 4) merged together:

```
<site physical root>/
    Web.config          <- WebApi48's config; also carries the SPA rewrite rules (already added, see below)
    Global.asax
    bin/
        One77.Api.WebApi48.dll, One77.Core.dll, ...
    index.html           <- Angular
    main-*.js, chunk-*.js, styles-*.css   <- Angular
    assets/               <- Angular
    media/                 <- Admin-uploaded homepage hero photograph (created automatically on first upload; see below)
```

ASP.NET Web API's attribute routing claims `/api/*` and `/health*`;
everything else falls through to IIS's static file handler (real files:
`main.js`, `assets/logo.svg`, ...) or, for Angular deep links with no
matching physical file (`/airguns`, `/admin/bundles`, a refreshed or pasted
URL), to the SPA-fallback rewrite rule — already added to
`One77.Api.WebApi48/Web.config`'s `<system.webServer><rewrite>` section, so
no separate web.config for Angular is needed once the two outputs share a
root.

**This requires the IIS URL Rewrite Module** — a free, separate IIS
extension, not installed by default on a stock Windows Server. Install it
before deploying if it isn't already present
(https://www.iis.net/downloads/microsoft/url-rewrite — or via the Web
Platform Installer / IIS role features, whichever your environment uses).

If you'd rather keep Angular and the API as two separate IIS sites/bindings
instead (e.g. a subdomain per app), that works too — just set
`CorsAllowedOrigins` to the Angular origin and rebuild Angular with a
non-empty `apiBaseUrl` pointing at the API's real origin (see
`PRODUCTION_CONFIGURATION.md`). The merged single-site structure above is
simply the lower-complexity default this codebase is already shaped for.

### 7a. Homepage hero media persistence

An Admin can upload a homepage hero photograph directly from
`/admin/homepage`. It is stored as an ordinary file, not in SQL Server —
SQL only holds a URL-relative reference
(`dbo.HomepageConfig.HeroImagePath`).

**Where it lives:** `<site physical root>/media/`, a sibling of `bin/` and
the Angular build output shown above — created automatically the first
time an Admin uploads an image (`Directory.CreateDirectory` on demand, no
manual setup required). Configurable via `HomepageMediaPhysicalRoot` in
`Web.config` if media should live outside the site root entirely (e.g. a
separate drive); see `PRODUCTION_CONFIGURATION.md`.

**What URL exposes it:** `/media/<file>` — served by IIS's own static file
handler, the exact same mechanism already serving Angular's own
`assets/*` and the ONE77 brand images. No new IIS handler mapping, no new
rewrite rule: the existing SPA-fallback rule in `Web.config` only rewrites
requests that do **not** correspond to a real file on disk, so a genuine
uploaded photograph is left alone and served directly.

**What folder permission IIS requires:** the Application Pool identity
needs **write** access to `<site physical root>/media/` (read access is
implied by IIS's normal static-file serving, which needs no special
grant). If the identity can already write `Web.config`/`bin/` during your
deployment process, it very likely already has the access needed for
`media/` too, since it's a sibling of those.

**What must be preserved during redeploy — read this before scripting a
deploy:** a normal WebApi48 redeploy touches only `Web.config`, `Global.asax`,
and `bin/`; a normal Angular redeploy copies only `dist/browser`'s own
files (`index.html`, `main-*.js`, `assets/`, ...) into the site. Neither
step, done as documented above, ever touches `media/`. The one way to lose
uploaded media by accident is a deploy tool's "mirror/clean" mode (e.g.
`robocopy /MIR`, an MSDeploy sync that removes files not present in the
source) — **do not** use a destination-cleaning deploy mode against the
site root, or explicitly exclude `media/` from it if you do.

**What should be in backup:** `<site physical root>/media/` should be
included in whatever file-system backup already covers the rest of the
site, alongside the regular SQL Server database backup. There is currently
at most one file in it (the current hero photograph, if any) — small,
but not reconstructable from the database alone if lost, since the binary
itself lives only on disk.

### 8. CORS / domain

Set `CorsAllowedOrigins` in `Web.config` to the real domain(s) once known.
With the merged single-site structure, Angular's own calls to `/api/*` are
same-origin and never hit CORS at all — this setting mainly matters if you
later split Angular onto a different origin.

### 9. Test Admin login end-to-end

Log in through the real domain with the first-admin credentials from step 3
(smoke test steps 12–19 cover this in detail).

### 10. Domain

Once the smoke test and Admin login both pass, the domain is ready to serve
real traffic — **before** Shopify or payment are connected. That's not an
incomplete migration; it's the agreed sequence (see below).

## First-Admin Bootstrap

On first deployment, provide `InitialAdminEmail` / `InitialAdminPassword` /
`InitialAdminName` in `Web.config`. ONE77 creates the first administrator
automatically on startup. After the account exists, startup does not
overwrite it — `EnsureSeedAdminAsync` checks whether any `AdminUsers` row
already exists and does nothing if so; it never re-hashes or resets an
existing account's password on a later restart, redeploy, or app-pool
recycle. As routine hygiene (not a correctness requirement), blank those
two values out of the live config after confirming the first login works.

No registration, invite, or password-reset flow exists or is needed for
this — V1 is deliberately a single/small fixed set of admin accounts,
managed by whoever controls server configuration.

## What's intentionally not connected

**Shopify is not integrated.** The Angular storefront and Admin are built
against `ShopifyProductService`, currently backed by 9 clearly
`[DEV FIXTURE]`-labeled products with no fabricated price or inventory —
the storefront's Buy button is disabled ("Buy — coming soon") rather than
pretending to work. This is the swap boundary for later real Shopify
integration: replace `ShopifyProductService`'s implementation (one class,
three methods: `getByCategory`/`getByHandle`/`getById`) — no Admin or
storefront component needs to change. Full detail:
`ADMIN_SHOPIFY_PICKER_READINESS.md`.

**Payment is not integrated.** No payment gateway, no checkout, no cart —
none of this exists in the delivered build. It's added alongside Shopify in
a later milestone.

**The Admin is structurally ready regardless.** Product/Match/Bundle admin
screens all work today against the dev-fixture product source; the later
Shopify swap changes only where product data comes from, not how the Admin
presents or edits ONE77's own data (Match relationships, Bundle curation,
enrichment specs) around it.

Later stages, in order, once the domain above is live:

1. Replace the dev-fixture `ShopifyProductService` with a real Shopify
   provider.
2. Verify the real Shopify product/variant model against ONE77's current
   product-level (not variant-level) Match/Bundle/Enrichment design —
   determine whether any product needs a variant selector.
3. Connect commerce: cart, checkout.
4. Configure the payment gateway.
5. Live commerce UAT.

None of this is implemented now, by design.

## Source-only vs. production artifacts

Do not deploy these — they exist for development/testing only:

- `One77.Api.DevHost` (the .NET 8 host) — never production.
- `migration/frontend/src/app/core/services/shopify-dev-fixtures.ts` and
  the `ShopifyProductService` it backs — stays in the production bundle
  today (there's no other product source until Shopify is connected), but
  is not itself a deployment artifact to configure or run separately.
- `migration/backend/tests/`, `migration/frontend/**/*.spec.ts` — test
  code, not deployed.
- Playwright/dev tooling used during development — not part of the
  deployment.

Deploy only:
- `One77.Api.WebApi48`'s publish output + `Web.config` + `Global.asax`
  (step 4).
- `migration/frontend/dist/one77-storefront/browser/`'s contents (step 6).
- The SQL schema (step 1–2).

## Rollback

Kept deliberately simple for a V1 with no live production data yet:

- **Angular:** keep the previous `dist/.../browser/` output (or the
  previous build's source commit) until the new deployment is confirmed
  working. If something's wrong, swap the site's static files back.
- **API:** keep the previous publish output folder until the new one is
  confirmed working via the smoke test. If something's wrong, point the
  IIS site back at the previous `bin/` + `Web.config` + `Global.asax`.
- **Database:** schema scripts are forward-only deployment artifacts, not a
  migration framework with down-scripts. Take a SQL Server backup **before**
  any future schema change or before any real-data migration — restoring
  that backup is the rollback path for a failed schema change. For this
  initial deployment (empty database, no real data yet), there's nothing to
  back up beforehand; start backing up once real data exists.
- If a deployment fails before real data exists in the new environment,
  the simplest recovery is: fix the issue, redeploy — there's no data to
  lose yet.

No deployment orchestration tooling is introduced — this is a manual,
documented process appropriate to a V1 handover.

## Data migration — deliberately deferred

No MongoDB → SQL data migration has been performed. The old application
never reached a stable live-production state on its original stack, and
the value of migrating its data hasn't been assessed. The agreed sequence:
prove the new application runs correctly on the real server/domain first;
**then** decide whether any old data (Match relationships, Learn content,
etc.) is worth migrating. This is a deliberate decision, not an oversight.

## Honesty about verification

Everything in `One77.Api.WebApi48` — routing, `Global.asax` startup, the
JWT auth attribute, IIS's own handler pipeline — has been **compile
verified** in this Linux sandbox (an actual `dotnet build`/`dotnet publish`
of the real project, not a stand-in) but **not runtime verified**, because
this sandbox has no Windows, no IIS, and no Mono to execute .NET Framework
binaries. `IIS_FIRST_DEPLOY_SMOKE_TEST.md` is the single largest
unresolved verification item in this handover — it exists specifically to
close that gap on your machine. Everything reachable from Linux (all
business logic in `One77.Core`, the Angular application, the SQL schema
against a real SQL Server 2019 instance) has been genuinely executed and
tested, not just compiled — see the Phase 3 server-readiness report for
the full test totals.
