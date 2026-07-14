# Build Spec for Partner
- database schema: `.\database\course.sql`

## Summary

`Partner` represents a course-provider / partner organization (合作廠商). It is a small reference
table with an auto-assigned identity key, a display name, an application key, two context-specific
display names (one for the partner menu, one for the course detail page), a sort order, and an
optional logo image filename. Several course-domain tables (`Course`, `Certification`,
`PartnerCourseGroup`) reference `Partner` as a foreign-key target, so a lookup endpoint is provided;
`Partner` itself has no outbound foreign keys and no N-N relationships.

| Item | Detail |
|------|--------|
| Primary Key | `pkid` **smallint IDENTITY** (auto-assigned by the DB) |
| Foreign Keys | None |
| Required Fields | `Name`, `AppKey`, `NameOnPartnerMenu`, `NameOnCourseDetailPage`, `DisplayOrder` |
| N-N Relationships | N/A |
| Primary-Foreign Links | `Course` (`Partner_pkid`), `Certification` (`Partner_pkid`), `PartnerCourseGroup` (`Partner_pkid`) — documented for future navigation; none of these child features are built yet |
| Query Filters | keyword (Name, AppKey, NameOnPartnerMenu, NameOnCourseDetailPage) |
| Default Sort | `DisplayOrder ASC` |

---

## Localization

### Chinese Table Name

- Partner: 合作廠商
- Description: 課程合作廠商 / 提供者主資料

### Chinese Column Names

- pkid: 主代碼
- Name: 名稱
- AppKey: 應用金鑰
- NameOnPartnerMenu: 廠商選單顯示名稱
- NameOnCourseDetailPage: 課程詳細頁顯示名稱
- DisplayOrder: 顯示順序
- ImageFilename: 圖片檔名

---

## Required Fields

Required (NOT NULL, excluding the IDENTITY PK):

- `Name` NOT NULL — nvarchar(50)
- `AppKey` NOT NULL — varchar(10)
- `NameOnPartnerMenu` NOT NULL — nvarchar(200)
- `NameOnCourseDetailPage` NOT NULL — nvarchar(50)
- `DisplayOrder` NOT NULL — int

Optional (nullable):

- `ImageFilename` NULL — varchar(50)

`pkid` is `smallint IDENTITY(1,1)` — assigned by the database, never supplied by the client. It is
**not** shown on the New form, is read-only in detail/list, and is used only as the route key for
view/edit/delete.

---

## Foreign Keys

`Partner` has no foreign key columns.

**N/A**

---

## Foreign-Primary Links

`Partner` has no foreign key columns.

**N/A**

---

## Primary-Foreign Links

The following tables reference `Partner.pkid` as a foreign key:

- **Course** (`Partner_pkid`) — 對應課程 → `/courses?partnerPkid={pkid}`
- **Certification** (`Partner_pkid`) — 對應認證 → `/certifications?partnerPkid={pkid}`
- **PartnerCourseGroup** (`Partner_pkid`) — 對應廠商課程群組 → `/partner-course-groups?partnerPkid={pkid}`

These are documented for future cross-entity navigation. **None of the child features
(Course, Certification, PartnerCourseGroup) are built yet**, so the Partner detail page will *not*
render link buttons to non-existent routes in this build — they will be added when those features
land. (Consistent with the existing AppRole / PublishStatus detail pages, which have no
primary-foreign link buttons.)

---

## N-N Relationships

`PartnerCourseGroup` carries its own `pkid` IDENTITY plus payload columns (`DisplayOrder`,
`Description`) in addition to its two FKs, so it is a first-class child entity, **not** a pure
junction table. `Partner` therefore has no N-N relationships to manage inline.

**N/A**

---

## Query Filters

- **keyword**: string
  - LIKE on `Name`, `AppKey`, `NameOnPartnerMenu`, `NameOnCourseDetailPage`
  - (`ImageFilename` is excluded — a stored filename is not a useful search target.)

No FK filters, boolean toggles, or date ranges apply.

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/partners` | **New** | Partner options: value = `pkid` (as string), label = `Name`, ordered by `DisplayOrder ASC`. Needed because `Course.Partner_pkid` / `Certification.Partner_pkid` target this table. |

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/partners` | List all (ORDER BY DisplayOrder ASC) |
| `POST` | `/api/partners/query` | Filtered query (body: `PartnerQuery`) |
| `GET` | `/api/partners/{id}` | Get by pkid (smallint route param) |
| `POST` | `/api/partners` | Create — pkid auto-assigned via `SCOPE_IDENTITY()`; returns 201 |
| `PUT` | `/api/partners` | Update (pkid from body); 404 if not found |
| `DELETE` | `/api/partners/{id}` | Delete; 404 if not found |
| `GET` | `/api/lookups/partners` | Slim lookup list (new) |

No auth exceptions. **No 409 path on create** — the key is IDENTITY, so duplicates are impossible.

---

## Backend Notes

### Models

```csharp
// Partner.cs
public class Partner
{
    public short Pkid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
    public string NameOnPartnerMenu { get; set; } = string.Empty;
    public string NameOnCourseDetailPage { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? ImageFilename { get; set; }
}

// PartnerRequest.cs  (pkid omitted on create; supplied only for update)
public class PartnerRequest
{
    public short Pkid { get; set; }                       // ignored on create (IDENTITY)

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string AppKey { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string NameOnPartnerMenu { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string NameOnCourseDetailPage { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    [StringLength(50)]
    public string? ImageFilename { get; set; }
}

// PartnerQuery.cs
public class PartnerQuery
{
    public string? Keyword { get; set; }   // LIKE on Name, AppKey, NameOnPartnerMenu, NameOnCourseDetailPage
}
```

### SQL — SELECT

No FK JOINs and no `nchar` columns (all string columns are `nvarchar`/`varchar`, so no `RTRIM()`).

```sql
SELECT p.pkid AS Pkid,
       p.Name,
       p.AppKey,
       p.NameOnPartnerMenu,
       p.NameOnCourseDetailPage,
       p.DisplayOrder,
       p.ImageFilename
FROM Partner p
-- GetAll / Query: ORDER BY p.DisplayOrder ASC
-- GetById:        WHERE p.pkid = @pkid
```

Query keyword clause:

```sql
WHERE (p.Name LIKE @kw OR p.AppKey LIKE @kw
       OR p.NameOnPartnerMenu LIKE @kw OR p.NameOnCourseDetailPage LIKE @kw)
```

### SQL — INSERT (IDENTITY, returns new pkid)

```sql
INSERT INTO Partner (Name, AppKey, NameOnPartnerMenu, NameOnCourseDetailPage, DisplayOrder, ImageFilename)
VALUES (@Name, @AppKey, @NameOnPartnerMenu, @NameOnCourseDetailPage, @DisplayOrder, @ImageFilename);
SELECT CAST(SCOPE_IDENTITY() AS smallint);
```

`CreateAsync` returns `Task<short>` (the new pkid). The controller returns `CreatedAtAction` with
that pkid. No `ExistsAsync` pre-check.

### SQL — UPDATE

```sql
UPDATE Partner
   SET Name = @Name,
       AppKey = @AppKey,
       NameOnPartnerMenu = @NameOnPartnerMenu,
       NameOnCourseDetailPage = @NameOnCourseDetailPage,
       DisplayOrder = @DisplayOrder,
       ImageFilename = @ImageFilename
 WHERE pkid = @Pkid;
```

Returns `affected > 0`.

### SQL — DELETE

```sql
DELETE FROM Partner WHERE pkid = @pkid;
```

### Repository interface

```csharp
Task<IEnumerable<Partner>> GetAllAsync();
Task<IEnumerable<Partner>> QueryAsync(PartnerQuery query);
Task<Partner?> GetByIdAsync(short pkid);
Task<short> CreateAsync(PartnerRequest request);   // returns new IDENTITY pkid
Task<bool> UpdateAsync(PartnerRequest request);
Task<bool> DeleteAsync(short pkid);
```

### Lookup

Add `GetPartnersAsync()` to `ILookupRepository` / `LookupRepository`:

```sql
SELECT CAST(pkid AS varchar(6)) AS Value,
       Name AS Label
FROM Partner
ORDER BY DisplayOrder ASC
```

Expose as `GET /api/lookups/partners` in `LookupsController`; add `getPartners()` to the frontend
`LookupService`.

### DI

Register `IPartnerRepository` / `PartnerRepository` in `Program.cs`.

### Special Column Notes

- `pkid` is `smallint IDENTITY` → C# `short`; excluded from INSERT; returned via `SCOPE_IDENTITY()`.
- No `nchar` columns → no `RTRIM()` needed.
- No `date` / `time` columns → no Dapper type-handler concerns.
- Numeric route param (`smallint`) → **no** `encodeURIComponent` needed on the client.

---

## Frontend Notes

### Model (`core/models/partner.model.ts`)

```ts
export interface Partner {
  pkid: number;
  name: string;
  appKey: string;
  nameOnPartnerMenu: string;
  nameOnCourseDetailPage: string;
  displayOrder: number;
  imageFilename: string | null;
}
export interface PartnerRequest {
  pkid: number;                 // 0 on create; DB assigns the real value
  name: string;
  appKey: string;
  nameOnPartnerMenu: string;
  nameOnCourseDetailPage: string;
  displayOrder: number;
  imageFilename: string | null;
}
export interface PartnerQuery {
  keyword?: string | null;
}
```

### Service (`core/services/partner.service.ts`)

Standard six methods against `${apiBaseUrl}/partners`. Numeric pkid — **no** `encodeURIComponent`.

### List component (`features/partners/partner-list/`)

- Columns: 主代碼 (pkid), 名稱 (name), 應用金鑰 (appKey), 廠商選單顯示名稱 (nameOnPartnerMenu),
  課程詳細頁顯示名稱 (nameOnCourseDetailPage), 顯示順序 (displayOrder), 操作 (view/edit/delete).
- Sortable/paginated `p-table`; rows-per-page `[10, 20, 50]`, default 20.
- Filter drawer (`p-drawer`, right): single **關鍵字** text input bound to `filters.keyword`.
- Session storage keys: `partner-list-filters`, `partner-list-sort`, `partner-list-page`.
- Delete confirm: `確定要刪除主代碼 <b>${item.pkid}</b>「${item.name}」？`

### Detail component (`features/partners/partner-detail/`)

- Read-only card showing all seven fields. `ImageFilename` shows the filename text (or 「—」 when null).
- No primary-foreign link buttons in this build (child features not yet built — see above).

### Form component (`features/partners/partner-form/`)

- Reactive form. **New mode: no pkid field** (IDENTITY). Edit mode: pkid shown read-only (plain
  text in the header/first row, not an editable control), route param used as key.
- Controls:
  - 名稱 (name) — `pInputText`, required, maxlength 50
  - 應用金鑰 (appKey) — `pInputText`, required, maxlength 10
  - 廠商選單顯示名稱 (nameOnPartnerMenu) — `pInputText`, required, maxlength 200
  - 課程詳細頁顯示名稱 (nameOnCourseDetailPage) — `pInputText`, required, maxlength 50
  - 顯示順序 (displayOrder) — `p-inputNumber`, required, `[useGrouping]="false"`, min 0
  - 圖片檔名 (imageFilename) — `pInputText`, optional, maxlength 50
- No `forkJoin` lookups (no FKs, no N-N).
- On save: create → navigate to the new detail page using the returned pkid; update → navigate to
  the detail page.

### Sidebar placement

Menu group **課程管理 Course**. This is currently a flat, route-less item in the 選單 MENU section.
Convert it to an expandable parent (`expanded: true`) with one child:

- 合作廠商 Partner (icon `pi pi-building`) → `/partners`

### Route table (`app.routes.ts`)

```
/partners            → PartnerList
/partners/new        → PartnerForm         (before /:id)
/partners/:id        → PartnerDetail
/partners/:id/edit   → PartnerForm
```

---

## Files to Create / Modify

### Backend (CMS.API)

| File | Action |
|------|--------|
| `Models/Partner.cs` | create |
| `Models/PartnerRequest.cs` | create |
| `Models/PartnerQuery.cs` | create |
| `Repositories/IPartnerRepository.cs` | create |
| `Repositories/PartnerRepository.cs` | create |
| `Controllers/PartnersController.cs` | create |
| `Repositories/ILookupRepository.cs` | modify (add `GetPartnersAsync`) |
| `Repositories/LookupRepository.cs` | modify (add `GetPartnersAsync`) |
| `Controllers/LookupsController.cs` | modify (add `partners` route) |
| `Program.cs` | modify (register `IPartnerRepository`) |

### Frontend (CMS.NG)

| File | Action |
|------|--------|
| `core/models/partner.model.ts` | create |
| `core/services/partner.service.ts` | create |
| `features/partners/partner-list/{ts,html,css}` | create |
| `features/partners/partner-detail/{ts,html,css}` | create |
| `features/partners/partner-form/{ts,html,css}` | create |
| `core/services/lookup.service.ts` | modify (add `getPartners`) |
| `app.routes.ts` | modify (add four partner routes) |
| `app.ts` / `app.html` | modify (課程管理 Course → parent w/ Partner child) |

### Tests

| File | Action |
|------|--------|
| `CMS.API.Tests/Fakes/FakePartnerRepository.cs` | create |
| `CMS.API.Tests/PartnersControllerTests.cs` | create (list, filter, get 200/404, create 201, update 200/404, delete 204/404) |
| `core/services/partner.service.spec.ts` | create |
| `features/partners/partner-list/partner-list.spec.ts` | create |
| `features/partners/partner-detail/partner-detail.spec.ts` | create |
| `features/partners/partner-form/partner-form.spec.ts` | create |
