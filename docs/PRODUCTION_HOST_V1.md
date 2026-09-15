# ONE77 Production Host V1

Status: implements the runtime/auth decision approved at the Phase 3 Section 11
checkpoint (Milestone 9). This document describes the production host that
exists as of Milestone 10 — `migration/backend/src/One77.Api.WebApi48`.

## Runtime

- **Production runtime:** .NET Framework 4.8
- **Host:** ASP.NET Web API 2, IIS-hosted (`Microsoft.AspNet.WebApi.Core` /
  `.WebHost` / `.Cors`)
- **Business logic / data access:** `One77.Core` (netstandard2.0) — unchanged,
  reused as-is. No business logic lives in the host project.
- **Dev/local host:** `One77.Api.DevHost` (.NET 8, ASP.NET Core minimal APIs)
  remains for local Linux development, live HTTP testing, and Angular
  development against a real running API. It is **not** the production host
  and is not going away — the two hosts coexist deliberately. If the two
  ever appear to disagree on a route's behavior, `One77.Api.WebApi48` is the
  one that matters for production; DevHost differences should be reported
  and fixed, not shipped.

## Database

Microsoft SQL Server 2019, unchanged from Milestone 2's schema. New in this
milestone: `migration/database/schema/006_admin_users.sql` (`AdminUsers`).

## Auth

JWT bearer authentication, approved at the Section 11 checkpoint:

- Stateless — no server-side session store, no session table.
- No refresh tokens, no roles/permissions, no MFA, no token blacklist.
- Algorithm: **HMAC-SHA256 (HS256)**, fixed. A symmetric algorithm is
  appropriate here because this API is the only consumer of its own tokens —
  there's no need for asymmetric key distribution to a third party.
- Claims: subject (admin id), email, a fixed `"admin"` role marker, issued-at,
  expiry. Never the password hash, never other business data.
- **Session duration: 720 minutes (12 hours)**, matching the old application's
  session length. No concrete security reason was found to choose a
  different V1 duration; changeable via `JwtExpiryMinutes` in `Web.config`
  without a code change.
- **Logout:** the Angular client discards the token. No server-side
  revocation — an admin disabled mid-session keeps a still-valid token until
  it naturally expires (up to 12 hours). This is a deliberate, accepted V1
  tradeoff: the alternative (a blacklist/session table) is exactly the
  server-side session infrastructure the approved decision ruled out.

### Where the logic lives

- `One77.Core/Admin/JwtTokenService.cs` — issues and validates tokens. Pure,
  host-agnostic (no HTTP, no System.Web) — fully unit-tested under the net8
  dev tooling today and reused unchanged by the net48 host.
- `One77.Core/Admin/AdminBearerAuthenticator.cs` — parses an `Authorization`
  header value and validates the token it carries; also pure/portable and
  unit-tested.
- `One77.Api.WebApi48/Auth/JwtAuthorizeAttribute.cs` — the only genuinely
  host-specific piece: a thin `System.Web.Http` adapter that reads the
  request's Authorization header, calls `AdminBearerAuthenticator`, and
  either sets the request principal or returns 401. This keeps the
  untested-by-necessity surface (see "What requires Windows/IIS" below) as
  small as possible.

### Password hashing

**BCrypt** (`BCrypt.Net-Next`), chosen over hand-rolled PBKDF2 because it
generates and embeds its own random salt automatically — no salt-handling
code to get wrong. Ships netstandard2.0/net462+ assemblies, so it lives in
`One77.Core` without compromising net48/net5 portability. Passwords are never
stored plaintext, reversibly encrypted, or logged; only the BCrypt hash is
persisted (`AdminUsers.PasswordHash`).

### Login error behavior

`POST /api/admin/login` returns the same generic `401 "Incorrect email or
password."` for an unknown email, a wrong password, and a disabled account —
`AdminAuthService.LoginAsync` throws one exception type
(`AdminAuthenticationException`) with a fixed message regardless of which
case applied, so the controller layer has no way to leak which one it was.
Missing/invalid/expired tokens on protected routes all produce a bare
`401 "Authentication required."` with no further detail.

## AdminUsers model

```sql
AdminUsers (
    AdminUserId    INT IDENTITY(1,1)  NOT NULL,
    Email          NVARCHAR(256)      NOT NULL,
    PasswordHash   NVARCHAR(256)      NOT NULL,
    Name           NVARCHAR(200)      NOT NULL,
    IsActive       BIT                NOT NULL DEFAULT (1),
    CreatedAt      DATETIME2(3)       NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt      DATETIME2(3)       NOT NULL DEFAULT (SYSUTCDATETIME()),
    PK (AdminUserId), UNIQUE (Email)
)
```

No roles/permissions/claims table, no sessions table, no refresh-token table,
no audit/login-history table, no password-reset/invitation workflow — none
are required by a stateless-JWT, single/small-admin V1, and the approved
decision explicitly excludes them.

## First admin / seeding

`Global.asax.cs`'s `Application_Start` calls
`AdminAuthService.EnsureSeedAdminAsync(email, password, name)` on every
application start, reading `InitialAdminEmail` / `InitialAdminPassword` /
`InitialAdminName` from `Web.config`. This is **safe to run on every boot**:
`EnsureSeedAdminAsync` is a no-op the moment any `AdminUsers` row exists — it
never re-hashes or overwrites an existing account's credentials. It also does
nothing at all if `InitialAdminEmail`/`InitialAdminPassword` are blank.

**What Tushar does at first deployment:**
1. Set `InitialAdminEmail` and `InitialAdminPassword` in the real (not
   committed) `Web.config` to the desired first-admin credentials.
2. Deploy and start the site once — the first admin account is created.
3. Log in, confirm access.
4. As routine hygiene, remove (or blank) `InitialAdminEmail` /
   `InitialAdminPassword` from the live config. This is not required for
   correctness (the seed logic is already a safe no-op once an admin
   exists) but avoids leaving a plaintext password sitting in server
   configuration indefinitely.

## Protected routes

Every `/api/admin/*` route requires a valid bearer token **except**
`POST /api/admin/login` (you cannot present a token before you have one).
Public routes (`/api/learn/*` read routes, `/api/webinar/events`,
`/api/webinar/register`, `/api/products/matches`, `/api/products/enrichment`,
`/api/bundles`) carry no auth requirement, matching the DevHost's existing
public/admin boundary exactly.

This is verified two ways:
- `One77.Api.WebApi48.Tests/RouteProtectionTests.cs` — a reflection-only
  check over every controller action's `[Route]`/`[JwtAuthorize]` attributes,
  asserting every `admin/` route (other than login) carries `[JwtAuthorize]`
  and no non-admin route does. Compiles; execution requires Windows (see
  below), but the check itself needs no HTTP hosting to run once it can.
- Enforcement is server-side in the API (via `JwtAuthorizeAttribute`), not
  only in the future Angular route guard.

## CORS

Allowed origins are entirely configuration-driven —
`Web.config`'s `appSettings/CorsAllowedOrigins`, a comma-separated list.
Nothing is hardcoded in code, there is no wildcard origin, and no
localhost-only assumption is baked into production behavior. The committed
template ships this **blank** — Tushar supplies the real Angular Admin
origin(s) at deployment.

## Required production configuration (`Web.config`)

All placeholders — **no real secret is committed**:

| Key | What it is | Secret? |
|---|---|---|
| `connectionStrings/Default` | SQL Server 2019 connection string | Yes |
| `appSettings/JwtSigningSecret` | Long random string signing every JWT | Yes |
| `appSettings/JwtIssuer` / `JwtAudience` | Token issuer/audience strings | No |
| `appSettings/JwtExpiryMinutes` | Session length in minutes (default 720) | No |
| `appSettings/CorsAllowedOrigins` | Comma-separated allowed Angular origin(s) | No |
| `appSettings/InitialAdminEmail` / `InitialAdminPassword` / `InitialAdminName` | First-admin seed, first deployment only | Yes (while set) |

## IIS / Web.config readiness

`Web.config` includes: `system.web` (`compilation`/`httpRuntime
targetFramework="4.8"`), the standard `system.webServer` handler mapping for
attribute-routed Web API 2 under IIS integrated mode, and a `<runtime>
<assemblyBinding>` block with the binding redirects the build actually
computed for this project's dependency graph (see the file's own comment —
extracted from the real build output, not guessed; re-extract whenever a
package version changes).

**COMPILE VERIFIED, not WINDOWS/IIS RUNTIME VERIFIED.** This sandbox is
Linux with no Mono, so `One77.Api.WebApi48` (and its test project) can be
restored and compiled here — proven, not assumed, by an actual `dotnet
build` of the real project — but cannot be executed or hosted here. IIS
routing, the `Global.asax` startup sequence, and the exact runtime behavior
of the auth attribute have not been exercised end-to-end and require an
actual Windows/IIS/.NET Framework 4.8 machine to confirm.

## What is NOT included

- Shopify credentials/OAuth/live integration of any kind.
- WhatsApp integration.
- Domain/SSL/deployment-specific secrets — all deployment-specific values
  are placeholders in `Web.config`, supplied by Tushar per environment.
- Roles/permissions, MFA, refresh tokens, token blacklist, customer
  accounts, audit/login-history — all explicitly out of scope for V1 per
  the approved decision.
- The Angular Admin UI itself (a later milestone).

## Carry-forward requirements (unchanged from Milestone 9)

- **Shopify product picker:** the future Admin must let a human choose
  Shopify products by name/image, resolving to the GID internally — an
  admin must never type a GID. Not implemented; nothing in this host design
  makes it harder later.
- **Shopify variant decision:** whether one Shopify Product = one sellable
  ONE77 item, or some products need a variant selector, is still to be
  determined against the real Shopify catalogue during the live Shopify
  integration milestone. Match/Bundle/Enrichment remain product-level.
- **Accessibility/SEO watch items** (semantic landmarks, keyboard nav,
  accessible mobile menu, image alt-text convention, route titles/meta
  descriptions) carry forward into later Angular work, unaddressed here.
