# ONE77 Sports — Migration Target (Angular / .NET / SQL Server / Shopify)

This directory holds the **new** target-stack implementation being built alongside the existing, untouched reference application (`../backend/` — FastAPI/MongoDB, `../frontend/` — React). The old app remains the functional reference throughout the migration; nothing here modifies it.

## Structure

```
migration/
  backend/
    One77.sln
    src/
      One77.Core/          .NET Standard 2.0 class library — domain models, services, SQL data access.
                            The reusable, runtime-portable core. Consumable unchanged by .NET
                            Framework 4.8 or .NET Core 5/8 — see portability notes below.
      One77.Api.DevHost/    net8.0 ASP.NET Core minimal API. TEMPORARY/DEV-ONLY — exists purely to
                            exercise One77.Core against a real SQL Server instance in this sandbox.
                            Not the production host. See the file's own header comment.
    tests/
      One77.Core.Tests/     xUnit, net8.0. Tests the portable Core layer.
  database/
    schema/                 T-SQL domain schema (empty — Milestone 2 owns this)
    scripts/                Small standalone operational/dev scripts only
  frontend/                 Reserved for the Angular app — not scaffolded until Milestone 8
```

## Portability principle (binding for every milestone before the runtime/auth checkpoint)

Tushar's confirmed production environment supports **.NET Framework 4.8** and **.NET Core 5**. Which one hosts the final Admin/auth surface is an open decision, reserved for an explicit Phase 3 checkpoint (Section 11 of the migration brief) — not decided here.

Until that checkpoint: all business/domain/data-access logic lives in **`One77.Core`**, targeting **`netstandard2.0`** — the one target framework moniker both .NET Framework 4.8 (≥ net461) and .NET Core/.NET 5+ can reference without modification. `One77.Api.DevHost` (net8.0) is a deliberately thin, swappable shell around it, used only because this development sandbox needs a concrete, runnable host to prove things actually work end-to-end. Replacing that host with the eventually-approved production host should require touching `Program.cs`-equivalent wiring only, never `One77.Core`.

Every NuGet package added to `One77.Core` is verified — via `dotnet add package`'s own compatibility check and inspection of the package's actual shipped `lib/` target folders, not assumed from memory — to ship a `netstandard2.0` (or lower, e.g. `net461`) asset before it's used. See the Milestone 1 report for the specific packages and evidence.

## What's deliberately NOT here yet

Match, Product Enrichment, Bundle, Learn, and Webinar schema/logic — Milestones 2–7. Angular — Milestone 8+. Final Admin auth — Milestone 10, after the Section 11 checkpoint. No Shopify integration code exists yet (Section 14 of the migration brief).
