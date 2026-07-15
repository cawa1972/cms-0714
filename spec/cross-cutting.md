# Cross-cutting conventions — Row Audit · exception handling · Admin-only authorization

Checklist for **every** new feature. These mechanisms are wired app-wide — follow them, never
reinvent or duplicate them. (CLAUDE.md points here; this file carries the detail.)

## Row Audit (change trail)

Writer: `CMS.API/Audit/IRowAuditWriter` + `RowAuditWriter` (reflection-based, scoped in DI).
Read side: `GET /api/rowaudit?tableName=&pkid=` → `RowAuditController` + `IRowAuditRepository`,
newest first.

Backend — every repository:

- Log to `RowAudit` on **Insert / Update / Delete** via
  `LogInsertAsync` / `LogUpdateAsync` / `LogDeleteAsync(tableName, …, db, tx)`, passing the
  **real DB table name** (e.g. `"Course"`).
- Audit writes go on the **same connection/transaction** as the data change: the repo opens
  conn + tx, performs the change, logs, then commits — a failed or rolled-back change must leave
  no audit row.
- **Update**: load the existing row (*before*) first, apply the UPDATE, re-load *after*, then
  `LogUpdateAsync(before, after)`. ActionDesc = comma-separated **changed column names**; a
  no-change update writes no audit row. **Delete**: load the row first (its first string column
  feeds ActionDesc), delete, then log.
- Audit snapshots use **raw-column selects** — no JOIN label columns, no derived counts — so the
  changed-column list only names real table columns. For N-N entities, load the linked-id list
  into the snapshot (the writer compares collections by content; see AppRole/AppUser repos).
- ActionDesc rules: Insert/Delete = the row's **first string-type column value**; Update = changed
  column names (truncated at 1000). PrimaryKeyValues = **pkid as a string** (found by reflection).
  UserName = the JWT `userName` claim, fallback `"system"`. Never insert `RowAudit.pkid` (IDENTITY).

Frontend — every detail page AND form page:

- Place `<app-row-audit-badge tableName="…" [pkid]="…" />` (`core/components/row-audit-badge`) at
  the start (`#start` slot) of the `.page-toolbar`. It shows the latest change inline and opens the
  full-trail dialog on click.
- Bind `pkid` as `null` in create mode — the badge hides itself until the record exists. For
  natural-key entities (AppRole/AppUser style), bind the **numeric surrogate `pkid`**, not the
  string business key (see the `auditPkid` signals in those forms).

Tests to copy: `RowAuditWriterTests` (reflection rules) · `PublishStatusRepositoryAuditTests`
(repo wiring end-to-end on in-memory SQLite) · `RowAuditRepositoryTests` + `RowAuditControllerTests`
(read side) · `row-audit-badge.spec.ts` (Angular).

## Exception handling

- `ExceptionHandlingMiddleware` (`CMS.API/Middleware/`, outermost in the pipeline) catches every
  unhandled exception: full detail (message + stack trace) is logged server-side; the client gets
  one generic `500 { "message": "An unexpected error occurred." }` (the `GenericErrorMessage`
  constant).
- **Do not add per-controller try/catch for unexpected errors**, and never return stack traces,
  SQL text, or connection details to the client.
- Meaningful responses stay as-is: 401 (unauthenticated), 403 (role-forbidden), 400 validation
  problems.
- Angular: `authInterceptor` already surfaces 5xx responses as an error toast (safe message from
  the body, bilingual fallback) and clears session + redirects to Login on 401; validation 400s
  stay form-level. Don't duplicate any of this in components or services.

Tests to copy: `ExceptionHandlingIntegrationTests` (leak-free 500; 401/403/400 untouched) ·
`auth.interceptor.spec.ts`.

## Admin-only management controllers

- `AppUsersController`, `AppRolesController`, and `PublishStatusesController` carry **class-level**
  `[Authorize(Roles = AppRoles.Admin)]`: every action returns 403 for an authenticated non-Admin,
  401 without a token. A hidden menu is **not** enforcement — the backend attribute is.
- Any new system/management entity (users, roles, system-wide definitions) gets the same
  class-level attribute.
- Dropdown/picker options that non-admin pages need come from the **unrestricted
  `LookupsController`** (`/api/lookups/*`) — never from an Admin-only CRUD controller.
- Lock every new restriction in with 403/401 integration tests over the real pipeline (JWT logins
  as Admin + non-Admin) — copy `AdminOnlyControllersTests` / `AppUsersAdminOnlyTests`.
