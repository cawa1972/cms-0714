# CLAUDE.md

Guidance for working in this repo. See `README.md` for run instructions and `spec/` for the
code-generation conventions that drive every feature.

## What this is

A full-stack CMS generated from a SQL Server schema. Features are scaffolded from `database/*.sql`
following `spec/code-gen.convention.md` + `spec/feature-spec.template.md`. Worked examples live in
`spec/sample1.spec.md` (Course) and `spec/sample2.spec.md` (SkillTrain).

```
database/          *.sql schema (source of truth for models)
spec/              conventions, feature-spec template, sample specs, UI reference PNGs
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
dotnet test src/CMS.sln                # 12 xUnit tests

# Frontend
cd src/CMS.NG && npm install           # first time
npm start                              # ng serve on http://localhost:4200
npm test                               # Karma + Jasmine (interactive)
ng test --watch=false --browsers=ChromeHeadless   # 28 tests, headless/CI
ng build                               # production build
```

## Version pins (do not casually bump)

- **.NET 9**, not 10 — the .NET 10 SDK is also installed; `global.json` forces 9.0.314. Projects
  target `net9.0`.
- **PrimeNG v20**, not v21 — v21 requires Angular 21; this app is on Angular 20. `npm install`
  PrimeNG as `primeng@^20 @primeng/themes@^20`.

## Backend conventions

- **Dapper only, no EF.** Repositories take `ISqlConnectionFactory` (injected; also lets tests use a
  fake). One repository + interface per aggregate; controllers are thin.
- **Routes:** `/api/{plural}` with the six standard verbs. `PUT` takes the key from the body (no
  route param). `POST /api/{plural}/query` for filtered search. Lookups under `/api/lookups/{plural}`.
- **Type handlers:** `DateOnlyTypeHandler` / `TimeOnlyTypeHandler` are registered in `Program.cs` —
  a baseline for future `date`/`time(7)` columns.
- **N-N relationships:** delete-then-reinsert inside a transaction on create/update; read via a
  separate query on the same connection. See `AppRoleRepository.SyncUsersAsync`.
- **`nchar(n)` columns:** always `RTRIM()` in SELECTs (none in AppRole yet, but the rule stands).
- **Tests** exercise the controllers against `FakeAppRoleRepository` (no live DB needed), so they
  cover list/filter/view/add/edit/delete + 404/409 semantics without a SQL Server dependency.

## Frontend conventions

- **Standalone components**, signals for local state, `inject()` over constructor injection.
- **Feature layout:** `src/app/features/{plural}/{entity}-list | -detail | -form`. Shared services and
  models under `src/app/core/`.
- **tsconfig path aliases:** `@env/*`, `@app/*`, `@core/*`, `@features/*`.
- **Environments, not proxy:** API base URL is in `src/environments/environment*.ts`
  (`environment.development.ts` swaps in for dev via `angular.json` fileReplacements). No dev proxy.
- **PrimeNG:** provided in `app.config.ts` (Aura preset, `provideAnimationsAsync`,
  `MessageService`/`ConfirmationService`). Root `<p-toast>` + `<p-confirmdialog>` live in `app.html`.
- **Sidebar** (`app.ts`/`app.html`/`app.css`): styled after the Ultima analytics template
  (<https://ultima.primeng.org/dashboards/analytics>) — light panel, uppercase gray section headers,
  icon+label items, expandable parents with a rotating chevron, emerald-tinted active highlight, a
  collapsible 68px icon rail, and a bottom user card. Nav data is a `NavSection[]` signal; add new
  features as items/children there (built routes link, unbuilt ones render as muted `disabled`).
- **List pages:** sortable/paginated `p-table`, `p-drawer` filter, and session-storage keys
  `{entity}-list-filters` / `-sort` / `-page`. `p-select` in drawers uses `appendTo="body"`.
- **Form pages:** reactive forms, `forkJoin` for parallel lookups on init, `p-multiselect` for N-N.

## AppRole — the reference feature

CRUD for `AppRole` (角色), sidebar **系統管理 Admin › 角色 AppRole**. Use it as the template for the
next feature. Two things about it are non-obvious and easy to get wrong:

- **`RoleId` (nvarchar) is the primary key**, not `pkid`. `pkid` is an IDENTITY surrogate shown as
  主代碼. All get/update/delete operations key by `RoleId`; the client wraps it in
  `encodeURIComponent`. This is the general "string PK" pattern from the convention.
- **The N-N to AppUser (`AppUserRole`) joins on string keys** (`UserId`, `RoleId`), not pkids. The
  user multiselect carries `UserId` strings; option label is `UserName (UserId)`.

## Gotchas

- **Non-ASCII request bodies from a shell:** Git Bash mangles UTF-8 in inline `curl -d '...'`
  (Chinese text → invalid JSON → HTTP 400). Send bodies via `--data-binary @file.json` instead. The
  API itself round-trips UTF-8 correctly (verified end-to-end against the live DB).
- **Connection string** is in `src/CMS.API/appsettings.json` (`.\SQLEXPRESS`, database `CMS`,
  Trusted_Connection). Trust cert / no encrypt for local dev.
