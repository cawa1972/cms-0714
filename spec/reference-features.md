# Reference features

The catalogue of already-built CRUD features. When scaffolding a new one, **copy the closest match**
and adapt. Each entry lists only the non-obvious things — the parts easy to get wrong. General
conventions live in `code-gen.convention.md` and `../CLAUDE.md`; this file is loaded on demand, not
every session.

Quick index (which feature to copy):

| Feature | Copy it for | Spec |
|---------|-------------|------|
| **Partner** | standard `int`/`smallint` IDENTITY PK — the baseline | `course/Partner.md` |
| **AppRole** | string (`nvarchar`) PK + N-N joined on string keys | *(no spec — original reference; read the code)* |
| **AppUser** | backend-only, server-managed column (password) | `auth/AppUser.md` |
| **PublishStatus** | user-assigned (non-IDENTITY) numeric PK + `bit` flags | `admin/PublishStatus.md` |
| **Course** | multiple FKs resolved via JOIN labels + `date` columns | `course/Course.md` |

Common to all: no RowAudit (this repo has none); N-N is delete-then-reinsert in a transaction;
list pages carry `{entity}-list-filters` / `-sort` / `-page` session keys.

---

## Partner — the standard IDENTITY-PK baseline

CRUD for `Partner` (合作廠商), sidebar **課程管理 Course › 合作廠商 Partner**. Spec at
`course/Partner.md`. The plain-vanilla pattern: `pkid` is a `smallint`/`int` IDENTITY primary key,
INSERT uses `SCOPE_IDENTITY()`, numeric route params (no `encodeURIComponent`). Start here for any
feature whose PK is a normal auto-increment surrogate.

---

## AppRole — the string-PK + N-N reference

CRUD for `AppRole` (角色), sidebar **系統管理 Admin › 角色 AppRole**. Two non-obvious things:

- **`RoleId` (nvarchar) is the primary key**, not `pkid`. `pkid` is an IDENTITY surrogate shown as
  主代碼. All get/update/delete operations key by `RoleId`; the client wraps it in
  `encodeURIComponent`. This is the general "string PK" pattern from the convention.
- **The N-N to AppUser (`AppUserRole`) joins on string keys** (`UserId`, `RoleId`), not pkids. The
  user multiselect carries `UserId` strings; option label is `UserName (UserId)`. See
  `AppRoleRepository.SyncUsersAsync` for the delete-then-reinsert sync.

## AppUser — the backend-only server-managed column reference

CRUD for `AppUser` (使用者), sidebar **系統管理 Admin › 使用者 AppUser**. Spec at `auth/AppUser.md`.
Structurally the **mirror of AppRole** (string PK `UserId` + `pkid` surrogate; N-N to AppRole via
`AppUserRole` joined on string keys; role multiselect label `RoleName (RoleId)` from a new
`GET /api/lookups/app-roles`). The one pattern worth copying:

- **`PasswordHash` is a backend-only column.** It is omitted from the response model, the write DTO,
  and **every** Angular model; never SELECTed, never accepted from the client. On **create** the repo
  reads `SysConfig.configValue` WHERE `configKey='appConfig'` (a JSON blob), extracts
  `defaultPassword`, SHA-256 hashes it (`Convert.ToHexStringLower(SHA256.HashData(utf8))`, a .NET 9
  API), and INSERTs it + stamps `PasswordUpdatedTime = GETDATE()`. On **update** the SQL omits the
  password columns entirely. A dedicated `POST /api/app-users/{id}/reset-password` re-reads the
  default, re-hashes, re-stamps (confirm-guarded 重設密碼 button on the detail page). Tests can't see
  the hash, so they assert only the observable trace — `PasswordUpdatedTime` gets stamped.
- `IsActive` `bit` and the read-only `PasswordUpdatedTime` `datetime` follow the same widgets as
  PublishStatus / the date-display convention (append `'Z'` to the ISO string before formatting).

## PublishStatus — the user-assigned numeric-PK reference

CRUD for `PublishStatus` (發布狀態), sidebar **系統管理 Admin › 發布狀態 PublishStatus**. Spec at
`admin/PublishStatus.md`.

- **`pkid` is a `tinyint` primary key that is NOT an IDENTITY column** — user-assigned. The numeric
  analogue of AppRole's string-PK pattern. Consequences: the INSERT writes `pkid` explicitly (no
  `SCOPE_IDENTITY()`); `CreateAsync` returns `Task`, and the controller pre-checks `ExistsAsync(pkid)`
  → **409** on a duplicate; `pkid` is editable in the New form but `.disable()`d in Edit (use
  `getRawValue()` on save); C# `byte`, TS `number` (no `encodeURIComponent` for numeric route params).
- No FKs and no N-N, so the form skips `forkJoin` lookups. The three `bit` flags render as
  `p-checkbox [binary]` in the form, colored `p-tag` (是/否) in list + detail, and tri-state
  `p-select` (全部/是/否 → `null`/`true`/`false`) filters in the list drawer.
- A **lookup endpoint** `GET /api/lookups/publish-statuses` (value = pkid string, label =
  Description) is provided because `Course.PublishStatus_pkid` targets this table, even though the
  feature has no FKs of its own.

## Course — the multiple-FKs + date-columns reference

CRUD for `Course` (課程), sidebar **課程管理 Course › 課程 Course**. Spec at `course/Course.md`.
`pkid` is a plain `int` IDENTITY (the standard Partner pattern). Two things worth copying:

- **Three FKs resolved to display labels via JOINs in one shared SELECT.** `Partner_pkid`
  (INNER JOIN), `CourseGroup_pkid` (nullable → **LEFT JOIN**), `PublishStatus_pkid` (INNER JOIN)
  each map to a label column on the `Course` model (`PartnerName`, `CourseGroupDescription`,
  `PublishStatusDescription`). It's a plain single-row map — **no Dapper multi-map / `splitOn`**.
  The FK `_pkid` columns are aliased (`c.Partner_pkid AS PartnerPkid`). Form dropdowns reuse the
  existing `/api/lookups/{partners,course-groups,publish-statuses}` endpoints; because
  `LookupItem.value` is a **string**, the list/form map it to a number (`Number(o.value)`) for the
  FK `p-select` `optionValue`.
- **First feature with `date` columns** (`ScheduleOn` / `ScheduleOff`, C# `DateOnly`). Backend uses
  the already-registered `DateOnlyTypeHandler` (no new registration). Frontend added the canonical
  `src/app/core/utils/date.util.ts` — `toIso()` builds `yyyy-MM-dd` from **local** components (never
  `toISOString().split('T')[0]`, which shifts UTC+8 dates back a day); `fromIso()` parses at local
  midnight (`new Date(iso + 'T00:00:00')`). The list filter drawer carries two date-range pairs; the
  form binds `p-datepicker` ↔ `Date`.

Deliberately **deferred** (target features/endpoints don't exist yet, documented in the spec's
"Deferred" sections): the N-N relations `CourseInCertification` / `CourseJobCategories`
(Certification & JobCategory have no repos/lookups), and inbound child links (`CourseFAQ`,
`CourseRelatedLink`, `HotCourse`, `CourseRecomm`).
