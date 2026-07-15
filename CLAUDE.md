# CLAUDE.md

Guidance for working in this repo. Everything below is orientation + hard guardrails; the detail
lives in reference files, loaded on demand. **Read the one that matches your task:**

| When you're… | Read |
|--------------|------|
| running the app | `README.md` |
| writing feature code (backend or frontend) | `spec/conventions.md` — cross-cutting rules + gotchas |
| adding inline table editing | `spec/conventions.md` — "Inline table editing" + the overlay-editor gotcha |
| scaffolding a new feature | `spec/reference-features.md` — find the closest built feature to copy (by PK shape, FK/N-N needs, column types) |
| building a customized (non-standard-UI) feature | `spec/custom/{Feature}/` — hand-written spec + UI mockup PNGs (e.g. FeaturedPromoItem weekly board) |
| generating from a schema | `spec/code-gen.convention.md` + `spec/feature-spec.template.md` |
| wanting a worked example | `spec/sample1.spec.md` (Course) · `spec/sample2.spec.md` (SkillTrain) |

## What this is

A full-stack CMS generated from a SQL Server schema. Features are scaffolded from `database/*.sql`
following the specs above. No RowAudit — this repo has none.

```
database/          *.sql schema (source of truth for models)
spec/              conventions, feature-spec template, sample specs, reference-features.md, UI PNGs
  custom/          hand-written specs for customized features (one folder per feature, with mockups)
src/
  CMS.sln
  CMS.API/         .NET 9 Web API — Dapper (no EF), Swagger, CORS. Port 5000.
  CMS.API.Tests/   xUnit — controllers tested against in-memory fake repositories.
  CMS.NG/          Angular 20 standalone + PrimeNG (Aura). Port 4200.
global.json        pins the .NET 9 SDK (9.0.314)
```

## Commands

```bash
# Backend
cd src/CMS.API && dotnet run           # http://localhost:5000 (Swagger at /swagger)
dotnet test src/CMS.sln                # xUnit controller tests

# Frontend
cd src/CMS.NG && npm install           # first time
npm start                              # ng serve on http://localhost:4200
npm test                               # Karma + Jasmine (interactive)
ng test --watch=false --browsers=ChromeHeadless   # headless/CI
ng build                               # production build
```

## Version pins (do not casually bump)

- **.NET 9**, not 10 — the .NET 10 SDK is also installed; `global.json` forces 9.0.314. Projects
  target `net9.0`.
- **PrimeNG v20**, not v21 — v21 requires Angular 21; this app is on Angular 20. `npm install`
  PrimeNG as `primeng@^20 @primeng/themes@^20`.
