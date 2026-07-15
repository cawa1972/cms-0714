# CLAUDE.md

Orientation + hard guardrails. Detail lives in reference files, loaded on demand —
**read the one matching your task:**

| When you're… | Read |
|--------------|------|
| running or testing the app | `README.md` — commands, ports, connection string |
| writing feature code (backend or frontend) | `spec/conventions.md` — cross-cutting rules + gotchas |
| adding a UI pattern (inline table edit, sticky form toolbar) | `spec/conventions.md` — the matching section + its gotcha |
| scaffolding a new feature | `spec/reference-features.md` — copy the closest built feature (by PK shape, FK/N-N needs, column types) |
| adding or testing an authenticated / protected endpoint | `spec/auth/Auth.md` — JWT login, global authorization, Angular guard/interceptor/shell |
| building a customized (non-standard-UI) feature | `spec/custom/{Feature}/` — hand-written spec + UI mockup PNGs (e.g. FeaturedPromoItem weekly board) |
| generating from a schema | `spec/code-gen.convention.md` + `spec/feature-spec.template.md` |
| wanting a worked example | `spec/sample1.spec.md` (Course) · `spec/sample2.spec.md` (SkillTrain) |

## Layout

Full-stack CMS scaffolded from a SQL Server schema (`database/*.sql` = source of truth for models).
No RowAudit — this repo has none.

```
database/           *.sql schema (source of truth for models)
spec/               conventions, code-gen + feature-spec templates, sample specs, reference-features.md
  custom/           hand-written specs for customized features (one folder per feature, with UI PNGs)
src/CMS.sln
  CMS.API/          .NET 9 Web API — Dapper (no EF), Swagger, CORS. :5000
  CMS.API.Tests/    xUnit — controllers vs. in-memory fake repositories
  CMS.NG/           Angular 20 standalone + PrimeNG (Aura). :4200
global.json         pins the .NET 9 SDK (9.0.314)
```

## Auth — global JWT (a guardrail, not opt-in)

- The API requires an **authenticated user on every controller by default** (a global `AuthorizeFilter` in `Program.cs`). A new controller is protected automatically — you do **not** add `[Authorize]`. `AuthController.Login` is the **only** `[AllowAnonymous]` endpoint (its `UpdateProfile` / `ChangePassword` siblings are protected like everything else — they read the caller's UserId from the JWT, never the body).
- Tokens are HS256, signed with the `symmetricSecurityKey` from `SysConfig` (`configKey='appConfig'`), read via `ISigningKeyProvider` — the **same key issues and validates**. Never hard-code it. Auth code lives in `CMS.API/Security/`.
- A backend test that calls a protected endpoint needs a bearer token (see `AuthorizationIntegrationTests`, which fakes the repo + signing key). The Angular app stores the profile in **session** storage (`cms.auth`, never local storage) and attaches the token via an HTTP interceptor; routes sit behind a guard. Detail: `spec/auth/Auth.md`.

## Version pins (do not casually bump)

- **.NET 9**, not 10 — the .NET 10 SDK is also installed; `global.json` forces 9.0.314; projects target `net9.0`.
- **PrimeNG v20**, not v21 — v21 needs Angular 21; this app is on Angular 20 (`primeng@^20 @primeng/themes@^20`).
