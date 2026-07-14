# Build Spec for CourseGroup
- database schema: `.\database\course.sql`

---

## Summary

`CourseGroup` (課程分類) is a small lookup/reference table classifying courses into groups. It has a
**system-assigned `smallint` IDENTITY primary key** and a single required `Description` column. It
has no foreign keys and no N-N relationships. Two tables reference it as an FK target
(`Course.CourseGroup_pkid`, nullable, `ON DELETE CASCADE`; `PartnerCourseGroup.CourseGroup_pkid`,
required) but neither `Course` nor `PartnerCourseGroup` is built in this repo yet (only `AppRole` and
`PublishStatus` exist), so a lookup endpoint is provided for future consumption and in-app
Primary-Foreign links are deferred.

| Item | Detail |
|------|--------|
| Primary Key | `pkid` smallint IDENTITY(1,1) → C# `short` |
| Foreign Keys | None |
| Required Fields | `Description` |
| N-N Relationships | N/A |
| Primary-Foreign Links | `Course.CourseGroup_pkid`, `PartnerCourseGroup.CourseGroup_pkid` reference this table — neither feature is built yet, so N/A in-app for now (lookup endpoint provided) |
| Query Filters | keyword (Description) |
| Default Sort | `pkid ASC` |

This is the IDENTITY-PK analogue of the `PublishStatus` (user-assigned numeric PK) and `AppRole`
(string PK) reference features — the simplest possible shape: standard `SCOPE_IDENTITY()` INSERT, no
409 pre-check needed since the database assigns the key.

---

## Localization

### Chinese Table Name

- CourseGroup: 課程分類
- Description: 課程所屬的分類（用於課程列表分組與篩選）

### Chinese Column Names

- pkid: 主代碼
- Description: 分類說明

---

## Required Fields

Required (NOT NULL):
- `Description` (nvarchar(100))

Optional (nullable): none.

---

## Foreign Keys

**N/A** — `CourseGroup` has no foreign key columns.

---

## Foreign-Primary Links

**N/A** — no foreign keys.

---

## Primary-Foreign Links

- `Course.CourseGroup_pkid` references `CourseGroup.pkid` (nullable, `ON DELETE CASCADE`).
- `PartnerCourseGroup.CourseGroup_pkid` references `CourseGroup.pkid` (required).

Neither `Course` nor `PartnerCourseGroup` is built in this repo yet, so there are no child list
pages to link to. **N/A** for now — revisit when those features are scaffolded. The lookup endpoint
below is provided so they can consume it.

---

## N-N Relationships

**N/A** — no junction table has exactly two FK columns with one pointing to `CourseGroup`.
(`PartnerCourseGroup` references `CourseGroup` but is its own entity with additional columns
(`DisplayOrder`, `Description`) and its own IDENTITY `pkid`, so it is not a pure junction table.)

---

## Query Filters

`POST /api/course-groups/query` — body `CourseGroupQuery`:

- **keyword**: string
  - LIKE on `Description` (the only string column)

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/course-groups` | **New** | `LookupItem`: value = `CAST(pkid AS varchar)`, label = `Description`, ordered by `pkid ASC` |

This feature does not consume any lookups itself (no FKs). The endpoint is added because
`CourseGroup` is an FK target for `Course` and `PartnerCourseGroup`.

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/course-groups` | List all (ORDER BY pkid ASC) |
| `POST` | `/api/course-groups/query` | Filtered query (body: `CourseGroupQuery`) |
| `GET` | `/api/course-groups/{id}` | Get by pkid (`{id}` = short) |
| `POST` | `/api/course-groups` | Create (pkid is IDENTITY — assigned by the database) |
| `PUT` | `/api/course-groups` | Update (pkid from body) |
| `DELETE` | `/api/course-groups/{id}` | Delete by pkid |
| `GET` | `/api/lookups/course-groups` | Slim lookup list |

No auth attributes (matches AppRole / PublishStatus — none applied).

---

## Backend Notes

### Models

```csharp
// CourseGroup.cs — response model
public class CourseGroup
{
    public short Pkid { get; set; }                // IDENTITY PK
    public string Description { get; set; } = string.Empty;
}

// CourseGroupRequest.cs — write DTO. Pkid is ignored on create (DB-assigned); on update it
// identifies the row.
public class CourseGroupRequest
{
    public short Pkid { get; set; }

    [Required]
    [StringLength(100)]
    public string Description { get; set; } = string.Empty;
}

// CourseGroupQuery.cs — search DTO
public class CourseGroupQuery
{
    public string? Keyword { get; set; }       // LIKE on Description
}
```

### SQL — SELECT

No JOINs, no aliases needed (column names already match C# properties, PascalCase-insensitive):

```sql
SELECT cg.pkid AS Pkid, cg.Description
FROM CourseGroup cg
ORDER BY cg.pkid ASC
```

`QueryAsync` builds a `WHERE` list: `Description LIKE @kw`.

### SQL — INSERT

**pkid is IDENTITY — the database assigns it; `SCOPE_IDENTITY()` returns the new value:**

```sql
INSERT INTO CourseGroup (Description)
VALUES (@Description);
SELECT CAST(SCOPE_IDENTITY() AS smallint);
```

`CreateAsync` returns `Task<short>` (the new pkid). No 409 pre-check is needed — the database always
assigns a fresh key.

### SQL — UPDATE

```sql
UPDATE CourseGroup
   SET Description = @Description
 WHERE pkid = @Pkid;
```

Returns `affected > 0` → 404 when the row is missing.

### Special Column Notes

- `pkid` is `smallint` **IDENTITY PK** → C# `short`, excluded from INSERT column list (DB-assigned),
  present in UPDATE `WHERE` only (not `SET`).
- No `nchar`, `date`, `time`, computed, or FK columns — no `RTRIM()` / type handlers / aliases needed.

---

## Frontend Notes

### Model (`course-group.model.ts`)

```typescript
export interface CourseGroup {
  pkid: number;
  description: string;
}
export interface CourseGroupRequest {
  pkid: number;
  description: string;
}
export interface CourseGroupQuery {
  keyword?: string | null;
}
```

### Service (`course-group.service.ts`)

Standard six methods against `${apiBaseUrl}/course-groups`. PK is numeric — `getById(pkid: number)`
and `delete(pkid: number)` interpolate the number directly (no `encodeURIComponent` needed).

### List page (`course-group-list`)

- Sortable/paginated `p-table`. Columns: 主代碼, 分類說明, 操作.
- Filter drawer: keyword `pInputText` only.
- Session keys: `course-group-list-filters`, `course-group-list-sort`, `course-group-list-page`.
- Delete confirm: `確定要刪除主代碼 <b>${item.pkid}</b>「${item.description}」？`

### Detail page (`course-group-detail`)

- Read-only rows for both fields. Back + Edit toolbar buttons.

### Form page (`course-group-form`)

- Reactive form. `pkid` is **not shown as an input** in New mode (IDENTITY, DB-assigned); in Edit
  mode it may be displayed read-only (disabled control, `getRawValue()` on save) for reference, since
  the request DTO still carries it for the update `WHERE` clause.
- `description` = `pInputText` (required, maxlength 100).
- No `forkJoin` lookups needed (no FKs / N-N); load only the existing record in edit mode.

### Sidebar

`app.ts`'s `課程管理 Course` entry is currently a flat, routeless (disabled) top-level item under
`選單 MENU`. Convert it into an expandable parent (mirroring `系統管理 Admin`) with a new child:
`{ label: '課程分類 CourseGroup', icon: 'pi pi-sitemap', route: '/course-groups' }`.

### Routes (`app.routes.ts`)

`course-groups` (list), `course-groups/new` (form), `course-groups/:id` (detail),
`course-groups/:id/edit` (form) — `/new` before `/:id`.

---

## Tests

### Backend (`CMS.API.Tests`)

- `FakeCourseGroupRepository` implementing `ICourseGroupRepository` (in-memory list, keyword
  filtering, auto-incrementing pkid on create, not-found semantics).
- `CourseGroupsControllerTests`: GetAll, Query by keyword, Query empty returns all, GetById
  (found + 404), Create (201 + persists + DB-assigned pkid), Update (200 + fields), Update missing
  (404), Delete (204), Delete missing (404).

### Frontend (Karma + Jasmine)

- `course-group.service.spec.ts` — each method hits the right URL/verb; body shapes match.
- `course-group-list.spec.ts` — loads on init; applyFilters/clearFilters persist + reload;
  add/view/edit navigation; remove-on-confirm.
- `course-group-detail.spec.ts` — loads by id; back/edit navigation.
- `course-group-form.spec.ts` — add mode, edit mode (loaded), invalid form not saved, valid save
  calls create/update + navigates.

---

## Files to Create / Modify

| Side | File | Action |
|------|------|--------|
| API | `Models/CourseGroup.cs`, `CourseGroupRequest.cs`, `CourseGroupQuery.cs` | create |
| API | `Repositories/ICourseGroupRepository.cs`, `CourseGroupRepository.cs` | create |
| API | `Controllers/CourseGroupsController.cs` | create |
| API | `Repositories/ILookupRepository.cs`, `LookupRepository.cs`, `Controllers/LookupsController.cs` | modify (add course-groups lookup) |
| API | `Program.cs` | modify (register repository) |
| Tests | `Fakes/FakeCourseGroupRepository.cs`, `CourseGroupsControllerTests.cs` | create |
| NG | `core/models/course-group.model.ts`, `core/services/course-group.service.ts` | create |
| NG | `features/course-groups/course-group-list \| -detail \| -form` (+ `.html/.css/.spec.ts`) | create |
| NG | `app.routes.ts`, `app.ts`, `app.html` | modify (routes + sidebar) |
