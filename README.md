# CMS

Full-stack CMS scaffolded from the SQL schema in `database/`, following `spec/code-gen.convention.md`.

- **Backend** — `src/CMS.API` — .NET 9 Web API, Dapper (no EF), Swagger, CORS. Runs on **:5000**.
- **Tests** — `src/CMS.API.Tests` — xUnit.
- **Frontend** — `src/CMS.NG` — Angular 20 (standalone) + PrimeNG (Aura). Runs on **:4200**.

The SDK is pinned to .NET 9 via `global.json`.

## First feature: AppRole (角色)

CRUD for `AppRole` with list/filter, view, add, edit, and an N-N user assignment
(`AppUserRole`). Sidebar: **系統管理 Admin › 角色 AppRole**.

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/app-roles` | List all |
| POST | `/api/app-roles/query` | Filter (keyword, permissionLevel) |
| GET | `/api/app-roles/{roleId}` | View (with assigned user ids) |
| POST | `/api/app-roles` | Create (409 on duplicate RoleId) |
| PUT | `/api/app-roles` | Update (RoleId from body, immutable) |
| DELETE | `/api/app-roles/{roleId}` | Delete |
| GET | `/api/lookups/app-users` | AppUser options for the user multiselect |

`RoleId` is the natural (string) primary key; `pkid` is the IDENTITY surrogate shown as 主代碼.

## Run

### Backend (port 5000)
```bash
cd src/CMS.API
dotnet run
# Swagger UI: http://localhost:5000/swagger
```
Connection string lives in `src/CMS.API/appsettings.json` (`SQLEXPRESS`, database `CMS`).

### Frontend (port 4200)
```bash
cd src/CMS.NG
npm install   # first time only
npm start     # ng serve on http://localhost:4200
```
API base URL is set in `src/CMS.NG/src/environments/environment*.ts` (no dev proxy).
Shorthand tsconfig paths: `@env/*`, `@app/*`, `@core/*`, `@features/*`.

## Test

```bash
# Backend
dotnet test src/CMS.sln

# Frontend
cd src/CMS.NG && npm test        # Karma + Jasmine (ng test)
# CI/headless: ng test --watch=false --browsers=ChromeHeadless
```

Current status: backend **12/12** xUnit tests pass; frontend **28/28** Karma tests pass.
