# ONE77 Production Configuration Reference

The single list of every value Tushar must supply to run the delivered
build in production. Nothing here is inferred from source — every value is
either read from `Web.config` at runtime or baked into the Angular build at
build time, as noted per item. No real value for any of these is committed
anywhere in this repository (see the Secrets Scan section of the Phase 3
server-readiness report for the verification method).

Where a value lives: `migration/backend/src/One77.Api.WebApi48/Web.config`
unless stated otherwise. That file, as committed, contains only
`CHANGE_ME`-style placeholders.

## Database

| Key | Where | What it is | Secret? |
|---|---|---|---|
| `connectionStrings/Default` | `Web.config` | SQL Server 2019 connection string (`Server=...;Database=One77;User Id=...;Password=...;TrustServerCertificate=True;`) | **Yes** |

## Auth (JWT)

| Key | Where | What it is | Secret? |
|---|---|---|---|
| `appSettings/JwtSigningSecret` | `Web.config` | Long random string signing every issued token. Generate a fresh one for production — never reuse the development value. | **Yes** |
| `appSettings/JwtIssuer` | `Web.config` | Token issuer string. Default `one77-api` is fine to keep. | No |
| `appSettings/JwtAudience` | `Web.config` | Token audience string. Default `one77-admin` is fine to keep. | No |
| `appSettings/JwtExpiryMinutes` | `Web.config` | Session length in minutes. Default `720` (12 hours), matching the old application. | No |

## First admin (first deployment only)

| Key | Where | What it is | Secret? |
|---|---|---|---|
| `appSettings/InitialAdminEmail` | `Web.config` | Email for the first Admin account. Leave blank after the first successful deployment. | **Yes, while set** |
| `appSettings/InitialAdminPassword` | `Web.config` | Password for the first Admin account. Leave blank after the first successful deployment. | **Yes, while set** |
| `appSettings/InitialAdminName` | `Web.config` | Display name for the first Admin account. | No |

See "First-Admin Bootstrap" in `TUSHAR_DEPLOYMENT_HANDOVER.md` for the exact
procedure and why leaving these blank afterward is hygiene, not a
correctness requirement — `EnsureSeedAdminAsync` is already a safe no-op
once any admin exists.

## CORS

| Key | Where | What it is | Secret? |
|---|---|---|---|
| `appSettings/CorsAllowedOrigins` | `Web.config` | Comma-separated list of allowed origins for the Angular Admin/storefront to call this API from, e.g. `https://one77sports.example`. Blank ships by default — the API will reject cross-origin calls until this is set. No wildcard is supported by design. | No |

If Angular and the API are deployed to the **same origin** (the recommended
structure — see "IIS Hosting Structure" in the handover doc), CORS is not
exercised in production at all (same-origin requests aren't cross-origin),
but the value should still be set to the real domain as a matter of
correctness/defense-in-depth, and is required if Angular is ever served
from a different origin than the API.

## Homepage hero media

| Key | Where | What it is | Secret? |
|---|---|---|---|
| `appSettings/HomepageMediaPhysicalRoot` | `Web.config` | Physical folder the Admin-uploaded homepage hero photograph is written to. Leave blank to default to `<site physical root>/media` — a sibling of `bin/` and the Angular build output. Set to an absolute path only if media should live somewhere else (e.g. a separate drive). | No |
| `appSettings/HomepageMediaMaxBytes` | `Web.config` | Maximum accepted upload size, in bytes. Default `8388608` (8 MB). Keep in sync with `httpRuntime/maxRequestLength` and `requestFiltering/maxAllowedContentLength` below if changed. | No |
| `system.web/httpRuntime/maxRequestLength` | `Web.config` | ASP.NET's own request-size cap, in **KB**. Ships at `10240` (10 MB) — comfortably above the 8 MB app-level check. Raise this first if `HomepageMediaMaxBytes` is raised. | No |
| `system.webServer/security/requestFiltering/requestLimits/maxAllowedContentLength` | `Web.config` | IIS's own request-size cap, in **bytes**, enforced ahead of ASP.NET. Ships at `10485760` (10 MB), matching the above. | No |

**No new database/cloud storage was introduced.** The photograph is a plain
file on the same server, referenced from SQL Server by a URL-relative path
only (`dbo.HomepageConfig.HeroImagePath`, e.g. `/media/hero-<guid>.jpg`) —
never the image binary itself, never a physical file-system path. See
"Homepage Media Persistence" in `TUSHAR_DEPLOYMENT_HANDOVER.md` for the full
deployment/backup picture.

## Angular

Angular has **no runtime configuration file** — its API base URL is a
build-time decision, already resolved correctly for production:

| Setting | Where | Value | Secret? |
|---|---|---|---|
| `apiBaseUrl` | `src/environments/environment.ts`, compiled into every production build | `''` (empty string — relative URLs, e.g. `/api/admin/login`) | No |

This already matches the recommended same-origin IIS structure and requires
**no change and no rebuild** for a standard deployment. It should only be
touched if Tushar deploys the API to a genuinely different origin than the
Angular app, in which case `environment.ts` would need that origin baked in
and Angular rebuilt — not expected for a standard single-site deployment.

The separate `environment.development.ts` (pointing at
`http://localhost:5299`, the dev-only `One77.Api.DevHost`) is excluded from
every production build by Angular's own `fileReplacements` mechanism
(`angular.json`, `development` configuration only) — confirmed by
inspecting the production bundle output directly; see the server-readiness
report's Angular production audit.

## Summary: what Tushar must decide/generate before first deploy

1. A real SQL Server 2019 connection string for a database he creates (see
   `migration/database/README.md` for schema deployment).
2. A freshly generated JWT signing secret (any long random string — a
   password generator or `openssl rand -base64 48` is sufficient).
3. The first Admin's email/password/name.
4. The real production domain, for `CorsAllowedOrigins`.

Everything else in this list already ships with a safe, correct default.
