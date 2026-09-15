# IIS First-Deploy Smoke Test

Run this once, in order, the first time `One77.Api.WebApi48` + the Angular
production build are deployed to a real Windows/IIS/SQL Server 2019
machine. This is the step Claude Code cannot perform — this sandbox is
Linux with no Mono, so none of `One77.Api.WebApi48`'s actual IIS/Web API 2
request pipeline has been runtime-exercised. Everything up to this point is
**COMPILE VERIFIED**; everything below is **WINDOWS/IIS RUNTIME VERIFIED**,
and only Tushar can complete that column.

Assumes the recommended single-site structure from
`TUSHAR_DEPLOYMENT_HANDOVER.md` (Angular `browser/` build output and
`One77.Api.WebApi48`'s publish output merged into one IIS site's physical
root) and a real `Web.config` with every `CHANGE_ME` replaced per
`PRODUCTION_CONFIGURATION.md`.

| # | Step | Expected result | If it fails |
|---|---|---|---|
| 1 | Browse to the site in IIS Manager and confirm the app pool started without a startup exception (check Windows Event Viewer → Application log if it didn't) | App pool status: Started, no crash-on-start event | An unhandled exception in `Application_Start` — check the Event Viewer message; a missing/invalid `Web.config` value is the most likely cause |
| 2 | Confirm the app pool's .NET CLR version | `.NET CLR Version v4.0` (this is correct for a .NET Framework 4.8 app — IIS names it v4.0), Managed Pipeline Mode: Integrated | If set to "No Managed Code", the app pool was created for a static/PHP site by mistake |
| 3 | `GET /health` | `200 OK`, JSON `{"status":"ok","host":"webapi48"}` | 404 = routing/handler mapping problem; 500 = check Event Viewer |
| 4 | `GET /health/db` | `200 OK`, JSON `{"status":"ok","database":"reachable"}` | `503` with an error message = connection string is wrong or SQL Server is unreachable from this machine (firewall, wrong host/port) |
| 5 | With a freshly created, empty database and `InitialAdminEmail`/`InitialAdminPassword` set in `Web.config`: confirm the app started once (step 1) | Querying `AdminUsers` shows exactly one row with the configured email and a `PasswordHash` that is a `$2a$`/`$2b$`-prefixed 60-character string, never the plaintext password | Zero rows = `InitialAdminEmail`/`Password` were blank or the DB wasn't reachable; check step 4 first |
| 6 | `POST /api/admin/login` with the first-admin credentials, JSON body `{"email":"...","password":"..."}` | `200 OK`, JSON body containing `access_token` | `401` with `"Incorrect email or password."` = wrong credentials or the seed (step 5) didn't run; `500` = check Event Viewer |
| 7 | `GET /api/admin/me` with `Authorization: Bearer <token from step 6>` | `200 OK`, JSON with the admin's `email`/`name` | `401` = token wasn't accepted — check `JwtSigningSecret` didn't change between issuing and validating (e.g. app pool recycled with a different config) |
| 8 | `GET /api/admin/me` — or any other `/api/admin/*` route — with **no** `Authorization` header | `401 Unauthorized`, JSON `{"error":"Authentication required."}` | `200 OK` here is a serious problem — an admin route is not actually protected; stop and report before proceeding |
| 9 | `GET /api/learn/categories` with no token | `200 OK`, a JSON array (possibly empty) | `401` here is wrong — this is a public route |
| 10 | `GET /api/webinar/events` with no token | `200 OK`, a JSON array | Same as above |
| 11 | `GET /api/products/matches` with no token (needs a valid `shopify_product_id` query parameter to return real data, but the route itself should not require auth) | `200 OK` or a plain 400 validation message — not `401`/`404` | `401` = route incorrectly protected |
| 12 | From the Angular Admin's actual origin (browser dev tools, not curl), repeat step 6's login call | Succeeds with no CORS error in the browser console | A CORS error means `CorsAllowedOrigins` doesn't exactly match the origin the browser sent (scheme + host + port, no trailing slash) |
| 13 | Browse to the site's root URL | The Angular storefront home page renders | A blank page or IIS static-file listing means the Angular `browser/` build output wasn't placed in the site's physical root correctly |
| 14 | Browse to `/admin/login` directly (not by clicking a link) | The Angular Admin login page renders | A `404` here is the SPA-fallback rewrite not working — confirm the URL Rewrite Module is installed and the `<rewrite>` section in `Web.config` is intact |
| 15 | Log in from the Angular Admin login page using the first-admin credentials | Redirects to the Admin dashboard | If step 6/7 passed but this fails, check the browser console for a CORS or network error |
| 16 | While logged into the Angular Admin, refresh the browser on a deep route, e.g. `/admin/bundles` | The page reloads showing the Bundles list — no IIS 404 | A 404 here means the SPA-fallback rewrite rule isn't matching this path; re-check rule order in `Web.config` |
| 17 | While logged into the Angular Admin, refresh the browser on `/admin/match/<any-id>` (a deep admin route with a path segment) | Same as step 16 — no 404 | Same failure clue as step 16 |
| 18 | With dev tools' Network tab open, confirm requests to `/api/admin/...` were **not** rewritten to return `index.html`'s HTML content | Responses under `/api/*` are JSON, not HTML | If an API call is returning HTML, the "Preserve API requests" rewrite rule isn't matching — check it's listed before the SPA fallback rule in `Web.config` |
| 19 | Click "Log out" in the Angular Admin | Returns to `/admin/login`; a subsequent direct browse to `/admin/dashboard` also redirects to `/admin/login` | If a logged-out browser can still reach `/admin/dashboard`'s content, the client-side guard or the token clearing is broken — stop and report |
| 20 | Look at the rendered storefront's product listings | Product titles are visibly prefixed `[DEV FIXTURE]` and no price/Buy action pretends to work (a disabled "Buy — coming soon" button) | If any product looks like a real, purchasable item with a real price, something is wrong — Shopify is intentionally not connected yet; stop and report rather than treating this as commerce-ready |
| 21 | While logged into the Angular Admin, open `/admin/homepage` and upload a test landscape photograph, then Save | The upload succeeds, the page shows the new photograph with Replace/Remove actions | A failure here that mentions file size is `httpRuntime/maxRequestLength` or `requestFiltering/maxAllowedContentLength` being too low — see `PRODUCTION_CONFIGURATION.md`. A permissions error means the app pool identity cannot write to `<site physical root>/media/` — see "Homepage hero media persistence" in `TUSHAR_DEPLOYMENT_HANDOVER.md` |
| 22 | Browse to the site's root URL again | The homepage hero now shows the uploaded photograph full-bleed, with the headline/CTAs still legible over it, and no decorative "77" mark | If the mark is still showing, the config didn't save — recheck step 21. Then return to `/admin/homepage` and click "Remove image" to restore the standard hero before handing the site over — do not leave a test photograph live |

## After this checklist passes

1. Blank out `InitialAdminEmail`/`InitialAdminPassword` in the live
   `Web.config` (routine hygiene — see `PRODUCTION_CONFIGURATION.md`).
2. Proceed to domain connection, per `TUSHAR_DEPLOYMENT_HANDOVER.md`'s
   deployment order — Shopify and payment remain deliberately deferred
   until after this checklist and domain connection are both done.
