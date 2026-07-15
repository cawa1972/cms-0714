# Build Spec for AppUser
- database schema: `.\database\auth.sql`

`AppUser` (使用者) stores the CMS login accounts. Like `AppRole`, its clustered primary key is a
**string natural key** — `UserId` (nvarchar) — while `pkid` is an IDENTITY surrogate shown as 主代碼.
Each user is linked N-N to `AppRole` through the junction table `AppUserRole`, so this feature is the
mirror image of the AppRole reference feature (which owns the same junction from the role side).

The password column (`PasswordHash`) is **backend-only**: it is never sent to or received from the
frontend, is set to a hashed default on create, and is only rewritten via a dedicated
reset-password endpoint.

---

## Summary

| Item | Detail |
|------|--------|
| Primary Key | `UserId` nvarchar(200) (natural, string PK). `pkid` int IDENTITY is a surrogate 主代碼. |
| Foreign Keys | None on `AppUser` itself |
| Required Fields | `UserId`, `UserName`, `IsActive` (`PasswordHash` is required in DB but set server-side, never from the client) |
| N-N Relationships | `AppUserRole` — AppUser ↔ AppRole (join on string keys `UserId` / `RoleId`) |
| Primary-Foreign Links | N/A — the only inbound FK is `AppUserRole`, managed via the N-N section |
| Query Filters | keyword (UserId, UserName); `IsActive` tri-state bool |
| Default Sort | `UserId ASC` |

---

## Localization

### Chinese Table Name

- AppUser: 使用者
- Description: 系統登入帳號

### Chinese Column Names

- pkid: 主代碼
- UserId: 使用者代碼
- UserName: 使用者名稱
- IsActive: 啟用
- PasswordHash: 密碼雜湊  （後端專用，不對前端顯示或輸入）
- PasswordUpdatedTime: 密碼更新時間

---

## Required Fields

Required (NOT NULL, client-supplied):

- `UserId` — the natural key; required on create, immutable on update.
- `UserName`
- `IsActive` — `bit`, defaults to `true` (DB default `1`).

Server-managed (NOT NULL in DB, but NOT part of the client request):

- `PasswordHash` — set on create from the SysConfig default password (see Special Column Notes);
  never accepted from the client.

Optional (nullable):

- `PasswordUpdatedTime` — `datetime`; stamped server-side when the password is (re)set. Read-only in
  the UI.

---

## Foreign Keys

`AppUser` has no outbound foreign-key columns.

N/A

---

## Foreign-Primary Links

`AppUser` has no outbound foreign-key columns.

N/A

---

## Primary-Foreign Links

The only table that references `AppUser` is the junction `AppUserRole` (FK `UserId`). It is handled
inline through the N-N Relationships section below rather than as a separate child-list link.

N/A

---

## N-N Relationships

### AppUser ↔ AppRole via `AppUserRole`

| Column | Type | Notes |
|--------|------|-------|
| pkid | int IDENTITY | surrogate PK of the junction row |
| UserId | nvarchar(200) NOT NULL | FK → AppUser.UserId (part of composite unique key) |
| RoleId | nvarchar(200) NOT NULL | FK → AppRole.RoleId (part of composite unique key) |

- The junction joins on **string keys** (`UserId`, `RoleId`), not pkids — same pattern as the AppRole
  feature.
- **List page**: show a `角色數` (role count) column = `COUNT(*)` over `AppUserRole` for the user.
- **Detail page**: show the assigned roles as a read-only chip list; each chip label is
  `RoleName (RoleId)`, ordered by `RoleId`.
- **Form (edit + new)**: a `p-multiselect` of roles. Option value = `RoleId` (string), option label =
  `RoleName (RoleId)`, ordered by `RoleName`. Populated by a **new** lookup
  `GET /api/lookups/app-roles`.
- **Request field**: `AppUserRequest.RoleIds` — `List<string>`.
- **Sync pattern on save** (create & update), inside a transaction on the same connection:
  1. `DELETE FROM AppUserRole WHERE UserId = @UserId`
  2. Bulk `INSERT INTO AppUserRole (UserId, RoleId) VALUES (@UserId, @RoleId)` for each distinct id.
- **GET-by-id** populates `AppUser.RoleIds` via a separate `SELECT RoleId FROM AppUserRole WHERE
  UserId = @userId ORDER BY RoleId`.

---

## Query Filters

- **keyword**: string — LIKE on `UserId`, `UserName`. (Short identifier columns only; `PasswordHash`
  is never searched.)
- **IsActive**: `bool?` — tri-state exact match. `null` = 全部, `true` = 啟用, `false` = 停用.

No FK filters (no FK columns). No date-range filter on `PasswordUpdatedTime` (low value; can be added
later if needed).

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/app-roles` | **New** | AppRole options: value = `RoleId`, label = `RoleName (RoleId)`, ordered by `RoleName`. |
| `GET /api/lookups/app-users` | Exists | Already provided (AppUser options for AppRole's multiselect); unchanged. |

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/app-users` | List all (with role count) |
| `POST` | `/api/app-users/query` | Filtered query (body: `AppUserQuery`) |
| `GET` | `/api/app-users/{id}` | Get by UserId (route id is a string, no `:int`); populates `RoleIds` |
| `POST` | `/api/app-users` | Create. 409 if `UserId` already exists. Sets `PasswordHash` from SysConfig default. |
| `PUT` | `/api/app-users` | Update (`UserId` from body). Does **not** touch `PasswordHash`. 404 if not found. |
| `DELETE` | `/api/app-users/{id}` | Delete (removes `AppUserRole` rows first, then the user) |
| `POST` | `/api/app-users/{id}/reset-password` | **Special.** Re-reads the SysConfig default password, SHA-256 hashes it, writes `PasswordHash` + stamps `PasswordUpdatedTime`. Returns 204, or 404 if the user does not exist. |

- String PK: controller route `{id}` (no `:int` constraint); the Angular service wraps the id in
  `encodeURIComponent`.
- No auth attributes in this repo (consistent with the other features); noted as a future concern.

---

## Backend Notes

### Models

**`AppUser.cs`** (response model — no `PasswordHash`):

```csharp
public class AppUser
{
    public int Pkid { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? PasswordUpdatedTime { get; set; }

    /// <summary>Number of roles assigned (COUNT over AppUserRole). List column 角色數.</summary>
    public int RoleCount { get; set; }

    /// <summary>Assigned role ids (populated on GET by id, from AppUserRole).</summary>
    public List<string> RoleIds { get; set; } = [];
}
```

**`AppUserRequest.cs`** (write DTO — **no `PasswordHash`**):

```csharp
public class AppUserRequest
{
    [Required, StringLength(200)]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string UserName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>Assigned role ids (N-N AppUserRole). Delete-then-reinsert on save.</summary>
    public List<string> RoleIds { get; set; } = [];
}
```

**`AppUserQuery.cs`**:

```csharp
public class AppUserQuery
{
    /// <summary>LIKE match on UserId, UserName.</summary>
    public string? Keyword { get; set; }

    /// <summary>Tri-state exact match on IsActive.</summary>
    public bool? IsActive { get; set; }
}
```

### SQL — SELECT (shared `SelectColumns`)

`PasswordHash` is **never selected** into the response model.

```sql
SELECT u.pkid AS Pkid,
       u.UserId,
       u.UserName,
       u.IsActive,
       u.PasswordUpdatedTime,
       (SELECT COUNT(*) FROM AppUserRole ur WHERE ur.UserId = u.UserId) AS RoleCount
FROM AppUser u
```

- `GetAllAsync`: `{SelectColumns} ORDER BY u.UserId ASC`
- `QueryAsync`: append `WHERE` for keyword (`u.UserId LIKE @kw OR u.UserName LIKE @kw`) and/or
  `u.IsActive = @IsActive`, then `ORDER BY u.UserId ASC`.
- `GetByIdAsync`: `{SelectColumns} WHERE u.UserId = @userId`, then a second query
  `SELECT RoleId FROM AppUserRole WHERE UserId = @userId ORDER BY RoleId` to fill `RoleIds`.

### SQL — INSERT

Writable columns: `UserId`, `UserName`, `IsActive`, `PasswordHash`, `PasswordUpdatedTime`.
`PasswordHash` is computed server-side (not from the request); `PasswordUpdatedTime` stamped
`GETDATE()` at creation.

```sql
INSERT INTO AppUser (UserId, UserName, IsActive, PasswordHash, PasswordUpdatedTime)
VALUES (@UserId, @UserName, @IsActive, @PasswordHash, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS int);
```

Then run the `AppUserRole` sync. `pkid` is IDENTITY — not written (standard pattern, see
`partner-feature`).

### SQL — UPDATE

`PasswordHash` and `PasswordUpdatedTime` are **excluded** — an ordinary update never touches the
password.

```sql
UPDATE AppUser
   SET UserName = @UserName,
       IsActive = @IsActive
 WHERE UserId = @UserId;
```

Then re-sync `AppUserRole`.

### SQL — Reset password (special)

```sql
UPDATE AppUser
   SET PasswordHash = @PasswordHash,
       PasswordUpdatedTime = GETDATE()
 WHERE UserId = @UserId;
```

### N-N Sync Pattern

```sql
DELETE FROM AppUserRole WHERE UserId = @UserId;
-- then for each distinct roleId:
INSERT INTO AppUserRole (UserId, RoleId) VALUES (@UserId, @RoleId);
```

### Special Column Notes — PasswordHash

- **Backend only.** Excluded from `AppUser`, `AppUserRequest`, and every Angular model. Never
  SELECTed into the response, never accepted as input.
- **Default password source.** On create (and on reset), read `SysConfig.configValue` where
  `configKey = 'appConfig'`. That value is a JSON object; extract its `defaultPassword` property.
- **Hashing.** SHA-256 the default password (UTF-8 bytes) and store the result. Store as a lowercase
  hex string (fits `nvarchar(800)`). Implemented in the repository via a small private helper
  (`System.Security.Cryptography.SHA256`), so no new DI is required.
- **On update.** `PasswordHash` is left untouched by `UpdateAsync`; only the reset-password endpoint
  rewrites it.
- **`PasswordUpdatedTime`** (`datetime`, nullable) → C# `DateTime?`. Stamped `GETDATE()` on create
  and on reset. Displayed read-only in the UI; Dapper returns it with `Kind = Unspecified`, so the
  frontend appends `'Z'` before parsing (see Frontend Notes / Date Handling).

---

## Frontend Notes

### Angular model (`core/models/app-user.model.ts`)

```typescript
export interface AppUser {
  pkid: number;
  userId: string;
  userName: string;
  isActive: boolean;
  passwordUpdatedTime: string | null;   // ISO datetime (Kind=Unspecified; append 'Z' to display)
  roleCount: number;
  roleIds: string[];
}

export interface AppUserRequest {
  userId: string;
  userName: string;
  isActive: boolean;
  roleIds: string[];
  // NOTE: no password field — the API sets it server-side.
}

export interface AppUserQuery {
  keyword?: string | null;
  isActive?: boolean | null;
}
```

Reuse the existing `LookupItem` interface from `app-role.model.ts`.

### Service (`core/services/app-user.service.ts`)

Standard six methods (string PK → `encodeURIComponent` in `getById` / `delete`), plus:

```typescript
resetPassword(userId: string): Observable<void> {
  return this.http.post<void>(`${this.baseUrl}/${encodeURIComponent(userId)}/reset-password`, {});
}
```

Add `getAppRoles()` to `lookup.service.ts` → `GET /api/lookups/app-roles`.

### List component (`features/app-users/app-user-list/`)

- Columns: 主代碼 (pkid), 使用者代碼 (userId), 使用者名稱 (userName), 啟用 (isActive as colored
  `p-tag` 是/否), 角色數 (roleCount), 操作.
- Filter drawer: keyword `pInputText`; `IsActive` tri-state `p-select` (全部/啟用/停用 →
  `null`/`true`/`false`, `appendTo="body"`).
- Session-storage keys: `app-user-list-filters`, `app-user-list-sort`, `app-user-list-page`.
- Delete confirm: `確定要刪除主代碼 <b>${pkid}</b>「${userId}」？`.

### Detail component (`features/app-users/app-user-detail/`)

- `forkJoin({ user: getById, roles: getAppRoles })`; map `roleIds` → role labels `RoleName (RoleId)`.
- Fields: 主代碼, 使用者代碼, 使用者名稱, 啟用 (`p-tag`), 密碼更新時間
  (`{{ (passwordUpdatedTime + 'Z') | date:'yyyy-MM-dd HH:mm' }}` or `—` when null).
- Roles card: chip list, or "尚未指派角色" when empty.
- Toolbar adds a **重設密碼** button → `service.resetPassword(userId)`, guarded by a
  `p-confirmdialog` (message names the user). On success show a success toast and reload so the
  updated 密碼更新時間 shows.

### Form component (`features/app-users/app-user-form/`)

- Reactive form: `userId` (required, disabled in edit via `getRawValue()`), `userName` (required),
  `isActive` (`p-checkbox [binary]`, default `true`), `roleIds` (`p-multiselect`).
- `forkJoin` loads `getAppRoles()` (and the user in edit mode).
- **No password field** — the form never shows or submits a password.
- 409 on create surfaces `使用者代碼「{userId}」已存在。`.

### Date Handling

`passwordUpdatedTime` is a `datetime` returned with `Kind = Unspecified`; append `'Z'` before
parsing/formatting (`{{ value + 'Z' | date:'…' }}`), per the repo convention. No `DateOnly` /
`date.util.ts` involvement (that helper is for `date` columns only).

### Sidebar placement

Nav group **系統管理 Admin** already contains a `使用者 AppUser` child rendered as *disabled* (no
route). Change it to a routed link → `/app-users` (`app.ts` nav data; `app.html` is data-driven).

---

## Tests

### Backend (`CMS.API.Tests`)

- `FakeAppUserRepository` implementing `IAppUserRepository` (in-memory; mirrors keyword/IsActive
  filtering, N-N role sync, exists/not-found, and a `ResetPasswordAsync` that stamps a time).
- `AppUsersControllerTests`: list, keyword filter, IsActive filter, get-by-id (found + 404),
  create (201 + persists role links + 409 duplicate), update (200 + role re-sync + 404 missing),
  delete (204 + 404), reset-password (204 + 404). Assert the create/update DTOs carry no password.

### Frontend (`CMS.NG`)

- `app-user.service.spec.ts` — each verb hits the right URL/method; `getById`/`delete`/`resetPassword`
  `encodeURIComponent` the id; `resetPassword` POSTs to `/{id}/reset-password`.
- `app-user-list.spec.ts`, `app-user-detail.spec.ts`, `app-user-form.spec.ts` — mount with a mocked
  service + lookup, assert render, required-field enforcement, and (detail) that reset-password calls
  the service.

---

## Files to Create / Modify

### Backend

| File | Action |
|------|--------|
| `CMS.API/Models/AppUser.cs` | create |
| `CMS.API/Models/AppUserRequest.cs` | create |
| `CMS.API/Models/AppUserQuery.cs` | create |
| `CMS.API/Repositories/IAppUserRepository.cs` | create |
| `CMS.API/Repositories/AppUserRepository.cs` | create |
| `CMS.API/Controllers/AppUsersController.cs` | create |
| `CMS.API/Controllers/LookupsController.cs` | modify (add `app-roles`) |
| `CMS.API/Repositories/ILookupRepository.cs` | modify (add `GetAppRolesAsync`) |
| `CMS.API/Repositories/LookupRepository.cs` | modify (add `GetAppRolesAsync`) |
| `CMS.API/Program.cs` | modify (register `IAppUserRepository`) |

### Frontend

| File | Action |
|------|--------|
| `CMS.NG/src/app/core/models/app-user.model.ts` | create |
| `CMS.NG/src/app/core/services/app-user.service.ts` | create |
| `CMS.NG/src/app/core/services/lookup.service.ts` | modify (add `getAppRoles`) |
| `CMS.NG/src/app/features/app-users/app-user-list/*` | create |
| `CMS.NG/src/app/features/app-users/app-user-detail/*` | create |
| `CMS.NG/src/app/features/app-users/app-user-form/*` | create |
| `CMS.NG/src/app/app.routes.ts` | modify (add 4 routes) |
| `CMS.NG/src/app/app.ts` | modify (route the AppUser nav child) |

### Tests

| File | Action |
|------|--------|
| `CMS.API.Tests/AppUsersControllerTests.cs` | create |
| `CMS.API.Tests/Fakes/FakeAppUserRepository.cs` | create |
| `CMS.NG/.../app-user.service.spec.ts` + 3 component specs | create |
</content>
</invoke>
