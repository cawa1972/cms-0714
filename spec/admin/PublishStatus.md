# Build Spec for PublishStatus
- database schema: `.\database\admin.sql`

## Summary

`PublishStatus` is a small lookup/reference table describing the lifecycle state of publishable
content (draft / published / discontinued). It has a **user-assigned `tinyint` primary key**
(`pkid` — NOT an IDENTITY column), a short `Description`, and three independent `bit` flags. It has
no foreign keys and no N-N relationships. It is referenced by `Course.PublishStatus_pkid` as an FK
target, so a slim lookup endpoint is provided for future features (the Course feature is not built
in this repo yet).

| Item | Detail |
|------|--------|
| Primary Key | `pkid` **tinyint, user-assigned** (NOT IDENTITY) → C# `byte` |
| Foreign Keys | None |
| Required Fields | `pkid`, `Description`, `IsDraft`, `IsPublished`, `IsDiscontinued` |
| N-N Relationships | N/A |
| Primary-Foreign Links | `Course.PublishStatus_pkid` references this table — Course feature not built yet, so N/A in-app for now (lookup endpoint provided) |
| Query Filters | keyword (Description); IsDraft, IsPublished, IsDiscontinued tri-state bool |
| Default Sort | `pkid ASC` |

> **The one non-obvious thing:** `pkid` is the primary key **but it is not an IDENTITY column** — it
> is supplied by the user on create (numeric analogue of the AppRole string-PK pattern). Therefore:
> the INSERT writes `pkid` explicitly (no `SCOPE_IDENTITY()`), create must 409 on a duplicate `pkid`,
> `pkid` is editable in the New form but disabled/immutable in Edit, and all get/update/delete
> operations key by `byte pkid`.

---

## Localization

### Chinese Table Name

- PublishStatus: 發布狀態
- Description: 內容發布狀態代碼（草稿／已發布／已停用）

### Chinese Column Names

- pkid: 主代碼
- Description: 狀態說明
- IsDraft: 草稿
- IsPublished: 已發布
- IsDiscontinued: 已停用

---

## Required Fields

Required (NOT NULL):
- `pkid` (byte — user-assigned PK, range 0–255)
- `Description` (nvarchar(50))
- `IsDraft` (bit)
- `IsPublished` (bit)
- `IsDiscontinued` (bit)

Optional (nullable): none.

---

## Foreign Keys

**N/A** — `PublishStatus` has no foreign key columns.

---

## Foreign-Primary Links

**N/A** — no foreign keys.

---

## Primary-Foreign Links

`Course.PublishStatus_pkid` references `PublishStatus.pkid`. The Course feature is **not built in
this repo** (only AppRole + this feature exist), so there is no child list page to link to yet.
**N/A** for now — revisit when Course is scaffolded. The lookup endpoint below is provided so Course
can consume it.

---

## N-N Relationships

**N/A** — no junction tables reference `PublishStatus`.

---

## Query Filters

`POST /api/publish-statuses/query` — body `PublishStatusQuery`:

- **keyword**: string
  - LIKE on `Description` (the only short identifying string column)

- **IsDraft**: `bool?` — tri-state (null = no filter, true = checked, false = unchecked). Exact match.
- **IsPublished**: `bool?` — tri-state. Exact match.
- **IsDiscontinued**: `bool?` — tri-state. Exact match.

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/publish-statuses` | **New** | `LookupItem`: value = `CAST(pkid AS varchar)`, label = `Description`, ordered by `pkid ASC` |

This feature does not consume any lookups itself (no FKs). The endpoint is added because
`PublishStatus` is an FK target for `Course`.

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/publish-statuses` | List all (ORDER BY pkid ASC) |
| `POST` | `/api/publish-statuses/query` | Filtered query (body: `PublishStatusQuery`) |
| `GET` | `/api/publish-statuses/{id}` | Get by pkid (`{id}` = byte) |
| `POST` | `/api/publish-statuses` | Create — **409 if pkid already exists** |
| `PUT` | `/api/publish-statuses` | Update (pkid from body; pkid immutable) |
| `DELETE` | `/api/publish-statuses/{id}` | Delete by pkid |
| `GET` | `/api/lookups/publish-statuses` | Slim lookup list |

No auth attributes (matches AppRole — none applied).

---

## Backend Notes

### Models

```csharp
// PublishStatus.cs — response model
public class PublishStatus
{
    public byte Pkid { get; set; }                 // user-assigned tinyint PK
    public string Description { get; set; } = string.Empty;
    public bool IsDraft { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDiscontinued { get; set; }
}

// PublishStatusRequest.cs — write DTO (pkid is the key: required on create, immutable on update)
public class PublishStatusRequest
{
    [Range(0, 255)]
    public byte Pkid { get; set; }

    [Required]
    [StringLength(50)]
    public string Description { get; set; } = string.Empty;

    public bool IsDraft { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDiscontinued { get; set; }
}

// PublishStatusQuery.cs — search DTO
public class PublishStatusQuery
{
    public string? Keyword { get; set; }       // LIKE on Description
    public bool? IsDraft { get; set; }
    public bool? IsPublished { get; set; }
    public bool? IsDiscontinued { get; set; }
}
```

### SQL — SELECT

No JOINs, no aliases needed (column names already match C# properties, PascalCase-insensitive):

```sql
SELECT ps.pkid AS Pkid, ps.Description, ps.IsDraft, ps.IsPublished, ps.IsDiscontinued
FROM PublishStatus ps
ORDER BY ps.pkid ASC
```

`QueryAsync` builds a `WHERE` list: `Description LIKE @kw`, and exact matches on each provided bit.

### SQL — INSERT

**pkid is written explicitly (user-assigned); no `SCOPE_IDENTITY()`:**

```sql
INSERT INTO PublishStatus (pkid, Description, IsDraft, IsPublished, IsDiscontinued)
VALUES (@Pkid, @Description, @IsDraft, @IsPublished, @IsDiscontinued);
```

`CreateAsync` returns `Task` (void). Controller does an `ExistsAsync(request.Pkid)` pre-check → 409.

### SQL — UPDATE

```sql
UPDATE PublishStatus
   SET Description = @Description,
       IsDraft = @IsDraft,
       IsPublished = @IsPublished,
       IsDiscontinued = @IsDiscontinued
 WHERE pkid = @Pkid;
```

Returns `affected > 0` → 404 when the row is missing.

### Special Column Notes

- `pkid` is `tinyint` **user-assigned PK** → C# `byte`, included in INSERT, immutable in UPDATE.
- No `nchar`, `date`, `time`, computed, or FK columns — no `RTRIM()` / type handlers / aliases needed.

---

## Frontend Notes

### Model (`publish-status.model.ts`)

```typescript
export interface PublishStatus {
  pkid: number;
  description: string;
  isDraft: boolean;
  isPublished: boolean;
  isDiscontinued: boolean;
}
export interface PublishStatusRequest { /* same shape */ }
export interface PublishStatusQuery {
  keyword?: string | null;
  isDraft?: boolean | null;
  isPublished?: boolean | null;
  isDiscontinued?: boolean | null;
}
```

### Service (`publish-status.service.ts`)

Standard six methods against `${apiBaseUrl}/publish-statuses`. PK is numeric — `getById(pkid: number)`
and `delete(pkid: number)` interpolate the number directly (no `encodeURIComponent` needed).

### List page (`publish-status-list`)

- Sortable/paginated `p-table`. Columns: 主代碼, 狀態說明, 草稿, 已發布, 已停用, 操作.
- Bit columns render as `p-tag` (是/否) or check/dash icons.
- Filter drawer: keyword `pInputText`; three tri-state `p-select`s
  (options `[{label:'全部',value:null},{label:'是',value:true},{label:'否',value:false}]`, `appendTo="body"`).
- Session keys: `publish-status-list-filters`, `publish-status-list-sort`, `publish-status-list-page`.
- Delete confirm: `確定要刪除主代碼 <b>${item.pkid}</b>「${item.description}」？`

### Detail page (`publish-status-detail`)

- Read-only rows for all five fields (booleans as 是/否). Back + Edit toolbar buttons.

### Form page (`publish-status-form`)

- Reactive form. `pkid` = `p-inputNumber` (required, min 0, max 255) — **disabled in edit mode**
  (`this.form.controls.pkid.disable()` after load, mirroring `roleId`).
- `description` = `pInputText` (required, maxlength 50).
- Three flags = `p-checkbox [binary]="true"` (or `p-toggleswitch`).
- `getRawValue()` on save so the disabled `pkid` is included.
- 409 handling: on create, show 「主代碼 X 已存在」.
- No `forkJoin` lookups needed (no FKs / N-N); load only the existing record in edit mode.

### Sidebar

Add under existing **系統管理 Admin** group (`app.ts` `navSections` + `app.html` render already
handles `route`/disabled): new child `{ label: '發布狀態 PublishStatus', icon: 'pi pi-flag', route: '/publish-statuses' }`.

### Routes (`app.routes.ts`)

`publish-statuses` (list), `publish-statuses/new` (form), `publish-statuses/:id` (detail),
`publish-statuses/:id/edit` (form) — `/new` before `/:id`.

---

## Tests

### Backend (`CMS.API.Tests`)

- `FakePublishStatusRepository` implementing `IPublishStatusRepository` (in-memory list, keyword +
  tri-state bit filtering, exists/not-found semantics).
- `PublishStatusesControllerTests`: GetAll, Query by keyword, Query by each bit flag, GetById
  (found + 404), Create (201 + persists), Create duplicate pkid (409), Update (200 + fields),
  Update missing (404), Delete (204), Delete missing (404).

### Frontend (Karma + Jasmine)

- `publish-status.service.spec.ts` — each method hits the right URL/verb; body shapes match.
- `publish-status-list.spec.ts` — loads on init; applyFilters/clearFilters persist + reload;
  add/view/edit navigation; remove-on-confirm.
- `publish-status-detail.spec.ts` — loads by id; back/edit navigation.
- `publish-status-form.spec.ts` — add mode (pkid enabled), edit mode (pkid disabled, loaded),
  invalid form not saved, valid save calls create/update + navigates, 409 handled.

---

## Files to Create / Modify

| Side | File | Action |
|------|------|--------|
| API | `Models/PublishStatus.cs`, `PublishStatusRequest.cs`, `PublishStatusQuery.cs` | create |
| API | `Repositories/IPublishStatusRepository.cs`, `PublishStatusRepository.cs` | create |
| API | `Controllers/PublishStatusesController.cs` | create |
| API | `Repositories/ILookupRepository.cs`, `LookupRepository.cs`, `Controllers/LookupsController.cs` | modify (add publish-statuses lookup) |
| API | `Program.cs` | modify (register repository) |
| Tests | `Fakes/FakePublishStatusRepository.cs`, `PublishStatusesControllerTests.cs` | create |
| NG | `core/models/publish-status.model.ts`, `core/services/publish-status.service.ts` | create |
| NG | `features/publish-statuses/publish-status-list \| -detail \| -form` (+ `.html/.css/.spec.ts`) | create |
| NG | `app.routes.ts`, `app.ts`, `app.html` | modify (routes + sidebar) |
