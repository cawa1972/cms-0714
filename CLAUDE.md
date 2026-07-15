# CLAUDE.md

Orientation + hard guardrails only — detail lives in reference files, loaded on demand.
**Read the one matching your task:**

| When you're… | Read |
|--------------|------|
| running or testing the app | `README.md` — commands, ports, connection string |
| writing feature code or adding a UI pattern | `spec/conventions.md` — cross-cutting rules + gotchas (inline table edit, sticky form toolbar, …) |
| adding a repository write, a detail/form toolbar, or touching error/authorization behaviour | `spec/cross-cutting.md` — Row Audit checklist, exception middleware, Admin-only controllers |
| scaffolding a new feature | `spec/reference-features.md` — copy the closest built feature (PK shape, FK/N-N needs, column types) |
| touching auth or a protected endpoint | `spec/auth/Auth.md` — JWT login/profile/change-password, global + role authorization, Angular guard/interceptor/shell, test patterns |
| building a customized (non-standard-UI) feature | `spec/custom/{Feature}/` — hand-written spec + UI mockup PNGs |
| generating from a schema | `spec/code-gen.convention.md` + `spec/feature-spec.template.md` · worked examples: `spec/sample1.spec.md` (Course), `spec/sample2.spec.md` (SkillTrain) |
| using gstack dev tooling | `spec/gstack.md` — skill catalogue |

## Layout

Full-stack CMS scaffolded from a SQL Server schema. Row Audit + global exception handling are
wired app-wide — `spec/cross-cutting.md` is required reading before building any feature.

```
database/           *.sql schema — source of truth for models
spec/               conventions, templates, sample specs, reference-features.md, custom/ specs
src/CMS.sln
  CMS.API/          .NET 9 Web API — Dapper (no EF), Swagger, CORS. :5000
  CMS.API.Tests/    xUnit — controllers vs. in-memory fake repositories
  CMS.NG/           Angular 20 standalone + PrimeNG (Aura). :4200
global.json         pins the .NET 9 SDK (9.0.314)
```

## Hard guardrails

- **Auth is global, opt-out.** Every controller requires an authenticated user via the global `AuthorizeFilter` — never add `[Authorize]` for that. `AuthController.Login` is the **only** `[AllowAnonymous]` endpoint. Self-service endpoints take the caller's UserId **from the JWT, never the request body**; role-restricted actions use `[Authorize(Roles = AppRoles.…)]`.
- **Management controllers are Admin-only, controller-wide** (`AppUsers`, `AppRoles`, `PublishStatuses` — class-level `[Authorize(Roles = AppRoles.Admin)]`); any new system/management entity follows suit. Non-admin pickers read `/api/lookups/*`, never an Admin-only CRUD controller. Detail + test pattern: `spec/cross-cutting.md`.
- **Cross-cutting, every feature:** repositories log Insert/Update/Delete to `RowAudit` via `IRowAuditWriter` **on the same transaction** as the change; every detail/form page toolbar carries `<app-row-audit-badge>`; no per-controller try/catch — the global exception middleware owns unexpected errors. Full checklist: `spec/cross-cutting.md`.
- **Signing key** = SysConfig `configKey='appConfig'` → `symmetricSecurityKey`, read via `ISigningKeyProvider` (the same key issues and validates). **Never hard-code it.** Frontend session lives in **session** storage (`cms.auth`), never local storage.
- **Version pins — do not casually bump:** **.NET 9**, not 10 (`global.json` forces 9.0.314; the 10 SDK is also installed) · **PrimeNG v20**, not v21 (v21 needs Angular 21; this app is Angular 20).
- **gstack:** use the `/browse` skill for **all** web browsing — never the `mcp__claude-in-chrome__*` tools.
