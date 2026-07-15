# Build Spec for Course
- database schema: `.\database\course.sql`

> **Scope note (generated 2026-07-14).** This spec targets the CRUD feature that is
> *buildable against the current codebase*: full scalar Course CRUD plus the three
> foreign-key dropdowns whose lookup endpoints already exist (`Partner`, `CourseGroup`,
> `PublishStatus`). The Course table also has two N-N relationships
> (`CourseInCertification`, `CourseJobCategories`) and several inbound child tables
> (`CourseFAQ`, `CourseRelatedLink`, `HotCourse`, `CourseRecomm`), but the `Certification`,
> `JobCategory`, and those child features are **not yet built** and have **no lookup
> endpoints**. Those sections are documented below under **Deferred** and are intentionally
> excluded from the build until their supporting features exist. This keeps the feature at
> the same complexity level as the existing Partner / PublishStatus / CourseGroup features.

---

## Summary

`Course` (課程) is the central content entity — a training course offered by a partner. It
carries descriptive metadata, scheduling dates, pricing, and display ordering, and links to
`Partner`, `CourseGroup` (nullable), and `PublishStatus` as foreign keys.

| Item | Detail |
|------|--------|
| Primary Key | `pkid` int IDENTITY |
| Foreign Keys | `Partner_pkid` → `Partner.pkid`; `CourseGroup_pkid` → `CourseGroup.pkid` (**nullable**); `PublishStatus_pkid` → `PublishStatus.pkid` |
| Required Fields | `Title`, `CourseId`, `ProdCourseId`, `FriendlyUrl`, `DisplayOrder`, `Partner_pkid`, `PublishStatus_pkid`, `ScheduleOn`, `ScheduleOff`, `Hour`, `ListPrice`, `LearningCredit`, `CanRepeat` |
| N-N Relationships | `CourseInCertification`, `CourseJobCategories` — **Deferred** (target features not built) |
| Primary-Foreign Links | `CourseFAQ`, `CourseRelatedLink`, `HotCourse`, `CourseRecomm` — **Deferred** (child features not built) |
| Query Filters | keyword (Title, OfficialTitle, CourseId, ProdCourseId, FriendlyUrl); Partner_pkid; CourseGroup_pkid; PublishStatus_pkid; ScheduleOn range; ScheduleOff range; CanRepeat |
| Default Sort | `DisplayOrder ASC` (then `pkid DESC` implicit) |

---

## Localization

### Chinese Table Name

- Course: 課程
- Description: 訓練課程主資料

### Chinese Column Names

- pkid: 主代碼
- Title: 課程名稱
- OfficialTitle: 官方課程名稱
- CourseId: 簡介代碼
- ProdCourseId: 科目代碼
- FriendlyUrl: 友善網址
- DisplayOrder: 顯示順序
- Partner_pkid: 原廠 (Partner.Name)
- CourseGroup_pkid: 課程群組 (CourseGroup.Description)
- PublishStatus_pkid: 上架狀態 (PublishStatus.Description)
- ScheduleOn: 上架日期
- ScheduleOff: 下架日期
- Hour: 時數
- ListPrice: 定價
- LearningCredit: 點數
- Material: 教材
- Objective: 課程目標
- Target: 適合對象
- Prerequisites: 先備知識
- Outline: 課程大綱
- TowardCertOrExam: 考試／認證說明
- Note: 備註
- OtherInfo: 其他資訊
- CanRepeat: 允許重聽

> Column display-name hints from the invocation are honored: `CourseId` → 簡介代碼,
> `ProdCourseId` → 科目代碼, `Partner_pkid` → 原廠, `LearningCredit` → 點數,
> `CanRepeat` → 允許重聽.

---

## Required Fields

Required (NOT NULL, excluding IDENTITY PK):

- `Title` (nvarchar 200)
- `CourseId` (varchar 50)
- `ProdCourseId` (varchar 50)
- `FriendlyUrl` (nvarchar 100)
- `DisplayOrder` (int)
- `Partner_pkid` (smallint)
- `PublishStatus_pkid` (tinyint)
- `ScheduleOn` (date)
- `ScheduleOff` (date)
- `Hour` (smallint, default 0)
- `ListPrice` (decimal(9,0), default 0)
- `LearningCredit` (decimal(9,1), default 0)
- `CanRepeat` (bit, default 0)

Optional (nullable):

- `OfficialTitle` (nvarchar 300)
- `CourseGroup_pkid` (smallint)
- `Material` (nvarchar 500)
- `Objective` (nvarchar 4000)
- `Target` (nvarchar 500)
- `Prerequisites` (nvarchar 4000)
- `Outline` (nvarchar max)
- `TowardCertOrExam` (nvarchar max)
- `Note` (nvarchar 4000)
- `OtherInfo` (nvarchar 4000)

---

## Foreign Keys

List and Detail pages use the FK to display the related label; Form pages use it to bind a
`p-select` of options.

- **Partner_pkid** → `Partner.pkid` (NOT NULL)
  - Alias as `PartnerPkid` in SELECT for the C# model property.
  - Multi-map join to display `Partner.Name` (nav column 原廠).
  - Lookup option label = `Name`, order by `DisplayOrder ASC` (`GET /api/lookups/partners`, exists).

- **CourseGroup_pkid** → `CourseGroup.pkid` (**nullable** — allow null / "無" option)
  - Alias as `CourseGroupPkid` in SELECT.
  - Multi-map join to display `CourseGroup.Description` (nav column 課程群組).
  - Lookup option label = `Description`, order by `pkid ASC` (`GET /api/lookups/course-groups`, exists).

- **PublishStatus_pkid** → `PublishStatus.pkid` (NOT NULL)
  - Alias as `PublishStatusPkid` in SELECT.
  - Multi-map join to display `PublishStatus.Description` (nav column 上架狀態).
  - Lookup option label = `Description`, order by `pkid ASC` (`GET /api/lookups/publish-statuses`, exists).

---

## Foreign-Primary Links

**Deferred.** Navigation from a Course row to the related Partner / CourseGroup /
PublishStatus detail pages is possible (all three detail routes exist), but is **not part of
this build** to keep parity with the existing features (none of which render cross-entity
link buttons). The list/detail pages display the resolved label text only. This can be added
later without schema changes.

---

## Primary-Foreign Links

**Deferred.** The following tables FK to `Course.pkid`, but none of their features exist yet,
so no navigation links are generated:

- `CourseFAQ` (`Course_pkid`)
- `CourseRelatedLink` (`Course_pkid`)
- `HotCourse` (`Course_pkid`)
- `CourseRecomm` (`CourseId`, string FK)

---

## N-N Relationships

**Deferred.** Course has two junction tables:

- `CourseInCertification` (`Course_pkid`, `Certification_pkid`) → `Certification`
- `CourseJobCategories` (`Course_pkid`, `JobCategory_pkid`) → `JobCategory`

Neither `Certification` nor `JobCategory` has a repository or lookup endpoint yet, so the
multiselect controls and junction-sync logic are **excluded from this build**. When those
features are added, wire up `CertificationPkids` / `JobCategoryPkids` on `CourseRequest`,
add `GET /api/lookups/certifications` + `/api/lookups/job-categories`, and use the
delete-then-reinsert-in-transaction sync pattern.

---

## Query Filters

`POST /api/courses/query` accepts `CourseQuery`:

- **keyword** (string): LIKE on `Title`, `OfficialTitle`, `CourseId`, `ProdCourseId`,
  `FriendlyUrl`. (Large text columns excluded.)
- **partnerPkid** (short?): exact match on `Partner_pkid`. Dropdown from
  `GET /api/lookups/partners`.
- **courseGroupPkid** (short?): exact match on `CourseGroup_pkid`. Dropdown from
  `GET /api/lookups/course-groups`.
- **publishStatusPkid** (byte?): exact match on `PublishStatus_pkid`. Dropdown from
  `GET /api/lookups/publish-statuses`.
- **scheduleOnFrom / scheduleOnTo** (DateOnly?): `ScheduleOn BETWEEN` (inclusive).
- **scheduleOffFrom / scheduleOffTo** (DateOnly?): `ScheduleOff BETWEEN` (inclusive).
- **canRepeat** (bool?): tri-state exact match on `CanRepeat` (null = 全部).

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/partners` | **Exists** | Partner (value=pkid, label=Name) |
| `GET /api/lookups/course-groups` | **Exists** | CourseGroup (value=pkid, label=Description) |
| `GET /api/lookups/publish-statuses` | **Exists** | PublishStatus (value=pkid, label=Description) |

No new lookup endpoints needed. (Course is itself an FK target for the deferred child
features; a `GET /api/lookups/courses` endpoint can be added when those are built.)

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/courses` | List all (with FK nav labels), `ORDER BY DisplayOrder ASC` |
| `POST` | `/api/courses/query` | Filtered search (body: `CourseQuery`) |
| `GET` | `/api/courses/{id}` | Get by pkid (with FK nav labels) |
| `POST` | `/api/courses` | Create → 201 with new pkid |
| `PUT` | `/api/courses` | Update (pkid from body) → 200 / 404 |
| `DELETE` | `/api/courses/{id}` | Delete → 204 / 404 |

`pkid` is int IDENTITY → numeric route param (no `encodeURIComponent`). No auth attributes
(consistent with existing controllers).

---

## Backend Notes

### Models

`Models/Course.cs` (response — includes resolved FK label fields):

```csharp
public class Course
{
    public int Pkid { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? OfficialTitle { get; set; }
    public string CourseId { get; set; } = string.Empty;
    public string ProdCourseId { get; set; } = string.Empty;
    public string FriendlyUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public short PartnerPkid { get; set; }
    public short? CourseGroupPkid { get; set; }
    public byte PublishStatusPkid { get; set; }
    public DateOnly ScheduleOn { get; set; }
    public DateOnly ScheduleOff { get; set; }
    public short Hour { get; set; }
    public decimal ListPrice { get; set; }
    public decimal LearningCredit { get; set; }
    public string? Material { get; set; }
    public string? Objective { get; set; }
    public string? Target { get; set; }
    public string? Prerequisites { get; set; }
    public string? Outline { get; set; }
    public string? TowardCertOrExam { get; set; }
    public string? Note { get; set; }
    public string? OtherInfo { get; set; }
    public bool CanRepeat { get; set; }

    // Resolved FK display labels (from JOINs; not persisted):
    public string? PartnerName { get; set; }
    public string? CourseGroupDescription { get; set; }
    public string? PublishStatusDescription { get; set; }
}
```

`Models/CourseRequest.cs` (write DTO — no label fields):

```csharp
public class CourseRequest
{
    public int Pkid { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(300)] public string? OfficialTitle { get; set; }
    [Required, MaxLength(50)] public string CourseId { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string ProdCourseId { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string FriendlyUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public short PartnerPkid { get; set; }
    public short? CourseGroupPkid { get; set; }
    public byte PublishStatusPkid { get; set; }
    public DateOnly ScheduleOn { get; set; }
    public DateOnly ScheduleOff { get; set; }
    public short Hour { get; set; }
    public decimal ListPrice { get; set; }
    public decimal LearningCredit { get; set; }
    [MaxLength(500)] public string? Material { get; set; }
    [MaxLength(4000)] public string? Objective { get; set; }
    [MaxLength(500)] public string? Target { get; set; }
    [MaxLength(4000)] public string? Prerequisites { get; set; }
    public string? Outline { get; set; }           // nvarchar(max)
    public string? TowardCertOrExam { get; set; }   // nvarchar(max)
    [MaxLength(4000)] public string? Note { get; set; }
    [MaxLength(4000)] public string? OtherInfo { get; set; }
    public bool CanRepeat { get; set; }
}
```

`Models/CourseQuery.cs`:

```csharp
public class CourseQuery
{
    public string? Keyword { get; set; }
    public short? PartnerPkid { get; set; }
    public short? CourseGroupPkid { get; set; }
    public byte? PublishStatusPkid { get; set; }
    public DateOnly? ScheduleOnFrom { get; set; }
    public DateOnly? ScheduleOnTo { get; set; }
    public DateOnly? ScheduleOffFrom { get; set; }
    public DateOnly? ScheduleOffTo { get; set; }
    public bool? CanRepeat { get; set; }
}
```

### SQL — SELECT (shared, with FK label JOINs)

```sql
SELECT c.pkid AS Pkid, c.Title, c.OfficialTitle, c.CourseId, c.ProdCourseId, c.FriendlyUrl,
       c.DisplayOrder,
       c.Partner_pkid       AS PartnerPkid,
       c.CourseGroup_pkid   AS CourseGroupPkid,
       c.PublishStatus_pkid AS PublishStatusPkid,
       c.ScheduleOn, c.ScheduleOff, c.Hour, c.ListPrice, c.LearningCredit,
       c.Material, c.Objective, c.Target, c.Prerequisites, c.Outline,
       c.TowardCertOrExam, c.Note, c.OtherInfo, c.CanRepeat,
       p.Name         AS PartnerName,
       cg.Description AS CourseGroupDescription,
       ps.Description AS PublishStatusDescription
FROM Course c
     INNER JOIN Partner p        ON p.pkid  = c.Partner_pkid
     LEFT  JOIN CourseGroup cg   ON cg.pkid = c.CourseGroup_pkid
     INNER JOIN PublishStatus ps ON ps.pkid = c.PublishStatus_pkid
```

(`LEFT JOIN` for CourseGroup because it is nullable. Simple single-row map — the label
columns map directly onto the `Course` model, no Dapper multi-map splitOn needed.)

- `GetAllAsync`: append `ORDER BY c.DisplayOrder ASC, c.pkid DESC`.
- `QueryAsync`: build `WHERE` from `CourseQuery`, then same `ORDER BY`.
- `GetByIdAsync`: append `WHERE c.pkid = @pkid`.

### SQL — INSERT

```sql
INSERT INTO Course
    (Title, OfficialTitle, CourseId, ProdCourseId, FriendlyUrl, DisplayOrder,
     Partner_pkid, CourseGroup_pkid, PublishStatus_pkid, ScheduleOn, ScheduleOff,
     Hour, ListPrice, LearningCredit, Material, Objective, Target, Prerequisites,
     Outline, TowardCertOrExam, Note, OtherInfo, CanRepeat)
VALUES
    (@Title, @OfficialTitle, @CourseId, @ProdCourseId, @FriendlyUrl, @DisplayOrder,
     @PartnerPkid, @CourseGroupPkid, @PublishStatusPkid, @ScheduleOn, @ScheduleOff,
     @Hour, @ListPrice, @LearningCredit, @Material, @Objective, @Target, @Prerequisites,
     @Outline, @TowardCertOrExam, @Note, @OtherInfo, @CanRepeat);
SELECT CAST(SCOPE_IDENTITY() AS int);
```

### SQL — UPDATE

`SET` every writable column above `WHERE pkid = @Pkid`; return affected rows > 0.

### SQL — DELETE

`DELETE FROM Course WHERE pkid = @pkid`; return affected rows > 0.

### Special Column Notes

- **`DateOnly` columns** (`ScheduleOn`, `ScheduleOff`): `DateOnlyTypeHandler` is already
  registered in `Program.cs` — no change needed.
- **`decimal` precision**: `ListPrice` is `decimal(9,0)` (whole numbers), `LearningCredit`
  is `decimal(9,1)` (one decimal). Dapper maps both to C# `decimal`; the frontend enforces
  the fraction digits.
- No `nchar` columns on Course → no `RTRIM()` needed.
- No RowAudit infrastructure exists in this repo; not used (consistent with existing repos).

---

## Frontend Notes

### Angular Model — `core/models/course.model.ts`

```typescript
export interface Course {
  pkid: number;
  title: string;
  officialTitle: string | null;
  courseId: string;
  prodCourseId: string;
  friendlyUrl: string;
  displayOrder: number;
  partnerPkid: number;
  courseGroupPkid: number | null;
  publishStatusPkid: number;
  scheduleOn: string;   // ISO date 'yyyy-MM-dd'
  scheduleOff: string;
  hour: number;
  listPrice: number;
  learningCredit: number;
  material: string | null;
  objective: string | null;
  target: string | null;
  prerequisites: string | null;
  outline: string | null;
  towardCertOrExam: string | null;
  note: string | null;
  otherInfo: string | null;
  canRepeat: boolean;
  partnerName: string | null;
  courseGroupDescription: string | null;
  publishStatusDescription: string | null;
}

export interface CourseRequest { /* same as Course minus the three *Name/*Description labels */ }

export interface CourseQuery {
  keyword?: string | null;
  partnerPkid?: number | null;
  courseGroupPkid?: number | null;
  publishStatusPkid?: number | null;
  scheduleOnFrom?: string | null;
  scheduleOnTo?: string | null;
  scheduleOffFrom?: string | null;
  scheduleOffTo?: string | null;
  canRepeat?: boolean | null;
}
```

### Service — `core/services/course.service.ts`

Standard six methods against `${apiBaseUrl}/courses`. `pkid` numeric → no
`encodeURIComponent`.

### Lookup service additions

Add `getCourseGroups()` and `getPublishStatuses()` to `core/services/lookup.service.ts`
(only `getPartners()` and `getAppUsers()` exist today). All three back the list filter
drawer and the form dropdowns.

### List page — `features/courses/course-list/`

Columns (exactly the invocation's list, in order): 主代碼, 顯示順序, 簡介代碼, 科目代碼,
課程名稱, 原廠 (`partnerName`), 課程群組 (`courseGroupDescription`), 上架狀態
(`publishStatusDescription`), 上架日期, 下架日期, 時數, 定價, 點數, 允許重聽 (`p-tag` 是/否)
+ 操作 (view/edit/delete).

- FK columns render the resolved label text (fall back to `—` for null course group).
- `scheduleOn` / `scheduleOff` displayed with `| date:'yyyy/MM/dd'`.
- Load partner / course-group / publish-status lookups via `forkJoin` on init to populate
  the filter drawer dropdowns (`p-select`, `appendTo="body"`, `[filter]="true"`).
- Filter drawer: keyword input; three FK `p-select`s; two date-range pairs (`p-datepicker`);
  tri-state `p-select` for 允許重聽 (全部/是/否).
- Session-storage keys: `course-list-filters`, `course-list-sort`, `course-list-page`.
- Delete confirm: `確定要刪除主代碼 <b>${item.pkid}</b>「${item.courseId} ${item.title}」？`

### Detail page — `features/courses/course-detail/`

Read-only card mirroring the Partner detail layout: all scalar fields, FK label rows
(原廠/課程群組/上架狀態), dates formatted, 允許重聽 as a `p-tag`. Large text fields
(課程大綱 etc.) shown when present.

### Form page — `features/courses/course-form/`

Reactive form; `forkJoin` of the three lookups on init. Widgets:

| Field | Widget |
|-------|--------|
| 主代碼 | read-only display (edit mode only; IDENTITY) |
| 課程名稱 Title | `input pInputText` (required, max 200) |
| 官方課程名稱 OfficialTitle | `input pInputText` (max 300) |
| 簡介代碼 CourseId | `input pInputText` (required, max 50) |
| 科目代碼 ProdCourseId | `input pInputText` (required, max 50) |
| 友善網址 FriendlyUrl | `input pInputText` (required, max 100) |
| 顯示順序 DisplayOrder | `p-inputNumber` (required, min 0) |
| 原廠 Partner | `p-select` (required, `appendTo="body"`, `[filter]="true"`) |
| 課程群組 CourseGroup | `p-select` (optional, `[showClear]="true"` → null) |
| 上架狀態 PublishStatus | `p-select` (required) |
| 上架日期 ScheduleOn | `p-datepicker` (required, `dateFormat="yy/mm/dd"`) |
| 下架日期 ScheduleOff | `p-datepicker` (required) |
| 時數 Hour | `p-inputNumber` (min 0) |
| 定價 ListPrice | `p-inputNumber` (`[maxFractionDigits]="0"`, min 0) |
| 點數 LearningCredit | `p-inputNumber` (`[minFractionDigits]="1" [maxFractionDigits]="1"`, min 0) |
| 允許重聽 CanRepeat | `p-checkbox [binary]="true"` |
| 教材 Material | `input pInputText` (max 500) |
| 課程目標 Objective | `textarea pInputTextarea` (max 4000) |
| 適合對象 Target | `textarea` (max 500) |
| 先備知識 Prerequisites | `textarea` (max 4000) |
| 課程大綱 Outline | `textarea` (max) |
| 考試／認證說明 TowardCertOrExam | `textarea` (max) |
| 備註 Note | `textarea` (max 4000) |
| 其他資訊 OtherInfo | `textarea` (max 4000) |

### Date handling

Course is the first feature with `date` columns. Handle inline in the form/list (no shared
util exists yet):
- **Load**: `new Date(iso + 'T00:00:00')` to build the `Date` for `p-datepicker` (append
  local midnight time, never parse the bare ISO as UTC).
- **Save (`toIso`)**: build from local components —
  `` `${d.getFullYear()}-${pad(d.getMonth()+1)}-${pad(d.getDate())}` `` — never
  `toISOString().split('T')[0]` (that shifts UTC+8 dates back a day).

### Sidebar placement

Add **課程 Course** (`route: /courses`, icon `pi pi-book`) as the **first** child of the
existing **課程管理 Course** group in `app.ts` / `app.html`, above 合作廠商 Partner and
課程分類 CourseGroup.

### Routes — `app.routes.ts`

Add `courses`, `courses/new`, `courses/:id`, `courses/:id/edit` (new before `:id`), matching
the existing four-route lazy pattern.

---

## Tests

### Backend — `CMS.API.Tests/`

- `Fakes/FakeCourseRepository.cs` — in-memory `ICourseRepository` (keyword + FK + date-range
  + canRepeat filtering, auto-increment pkid, resolves label fields from seeded lookups or
  leaves them for controller tests that don't assert on them).
- `CoursesControllerTests.cs` — GetAll; Query by keyword; Query by partnerPkid; Query by
  canRepeat; GetById found + not-found (404); Create → 201 with assigned pkid; Update
  existing → 200; Update missing → 404; Delete existing → 204; Delete missing → 404.

### Frontend

- `course.service.spec.ts` — `HttpTestingController` asserts each method hits the right
  URL/verb (numeric pkid, no encoding).
- `course-list.spec.ts`, `course-detail.spec.ts`, `course-form.spec.ts` — mount with a
  mocked `CourseService` + `LookupService`; assert render and that the form flags required
  fields (Title, CourseId, ProdCourseId, FriendlyUrl, Partner, PublishStatus, dates).

---

## Files to create / modify

### Backend (create)
- `Models/Course.cs`, `Models/CourseRequest.cs`, `Models/CourseQuery.cs`
- `Repositories/ICourseRepository.cs`, `Repositories/CourseRepository.cs`
- `Controllers/CoursesController.cs`
- `CMS.API.Tests/Fakes/FakeCourseRepository.cs`, `CMS.API.Tests/CoursesControllerTests.cs`

### Backend (modify)
- `Program.cs` — register `ICourseRepository` / `CourseRepository`.

### Frontend (create)
- `core/models/course.model.ts`, `core/services/course.service.ts` (+ `.spec.ts`)
- `features/courses/course-list/` (ts/html/css/spec)
- `features/courses/course-detail/` (ts/html/css/spec)
- `features/courses/course-form/` (ts/html/css/spec)

### Frontend (modify)
- `core/services/lookup.service.ts` — add `getCourseGroups()`, `getPublishStatuses()`.
- `app.routes.ts` — four course routes.
- `app.ts` + `app.html` — sidebar 課程 Course entry.
