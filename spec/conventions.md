# Architecture conventions

How this app is built — the cross-cutting rules every feature follows. Loaded on demand, not every
session. For scaffolding-specific patterns see `code-gen.convention.md`; for per-feature gotchas see
`reference-features.md`.

## Backend

- **Dapper only, no EF.** Repositories take `ISqlConnectionFactory` (injected; also lets tests use a
  fake). One repository + interface per aggregate; controllers are thin.
- **Routes:** `/api/{plural}` with the six standard verbs. `PUT` takes the key from the body (no
  route param). `POST /api/{plural}/query` for filtered search. Lookups under `/api/lookups/{plural}`.
- **Type handlers:** `DateOnlyTypeHandler` / `TimeOnlyTypeHandler` are registered in `Program.cs` —
  a baseline for future `date`/`time(7)` columns.
- **N-N relationships:** delete-then-reinsert inside a transaction on create/update; read via a
  separate query on the same connection. See `AppRoleRepository.SyncUsersAsync`.
- **`nchar(n)` columns:** always `RTRIM()` in SELECTs.
- **Tests** exercise the controllers against in-memory fake repositories (e.g.
  `FakeAppRoleRepository`), covering list/filter/view/add/edit/delete + 404/409 semantics with no
  SQL Server dependency.

## Frontend

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
- **Inline table editing** (reference: `features/courses/course-list`): double-click a cell to edit
  (single-click must not); persist on the editor losing focus by calling the row's existing update
  endpoint. Per-column editor types (text / `p-inputNumber` / `p-datepicker` / `p-select` /
  `p-checkbox`). Validation shows an inline error and keeps the cell in edit mode on failure; a failed
  save reverts the cell. FK-label and PK columns stay read-only. Numeric editors set per-column
  `min` / `max` / decimal precision matched to the DB column type (see `NUMBER_FIELD_CONFIG`),
  enforced in both the editor and the validator. **See the overlay-editor gotcha below.**
- **Form pages:** reactive forms, `forkJoin` for parallel lookups on init, `p-multiselect` for N-N.
- **Sticky form toolbar** (reference: `features/courses/course-form`): to freeze the Save/Cancel
  action toolbar while the form body scrolls, make the component **host** the scroll region
  (`:host { height: calc(100vh - 32px); overflow-y: auto }` — the `32px` is `.content`'s 16px
  top+bottom padding) and pin `.page-toolbar` with `position: sticky; top: 0; z-index`. The toolbar
  and `<form>` are already direct children of the host, so no template change is needed; the toolbar
  keeps its opaque background + shadow (`styles.css`) so fields scroll cleanly underneath. Confine
  this to the form's `:host` — don't make the shared `.page-toolbar` sticky globally. **See the
  `.content` overflow gotcha below** for why the host (not the window) must own the scroll.
- **Dates:** display API `datetime` values by appending `'Z'` to the ISO string before formatting
  (Dapper returns `Kind=Unspecified`). For `date` columns use `core/utils/date.util.ts`.

## Gotchas

- **Non-ASCII request bodies from a shell:** Git Bash mangles UTF-8 in inline `curl -d '...'`
  (Chinese text → invalid JSON → HTTP 400). Send bodies via `--data-binary @file.json` instead. The
  API itself round-trips UTF-8 correctly (verified end-to-end against the live DB).
- **Connection string** is in `src/CMS.API/appsettings.json` (`.\SQLEXPRESS`, database `CMS`,
  Trusted_Connection). Trust cert / no encrypt for local dev.
- **`.content` (`app.css`) is the window's scroll owner, not an internal scroll box.** It sets
  `overflow-x: hidden`, which by spec forces `overflow-y` to compute to `auto` — so any descendant
  `position: sticky` is measured against `.content`, which grows with its content and never scrolls
  internally (the window scrolls). A naive sticky element therefore just scrolls away. To pin
  something, give it a bounded, actually-scrolling ancestor (e.g. the sticky-form-toolbar pattern
  makes the component `:host` the scroll region). PrimeNG overlays already dodge `.content`'s clip
  via `appendTo="body"`.
- **Inline-edit overlay editors don't commit on blur.** `p-select` / `p-datepicker` options live in
  an `appendTo="body"` overlay, so the mousedown that picks a value blurs the input *before* the value
  lands — a blur-based commit fires with the stale value and tears the editor down before the pick
  registers. Commit these on `(onChange)` / `(onSelect)` and close them on `(onHide)` / `(onClose)`;
  only plain text/number inputs commit on `(blur)`.
