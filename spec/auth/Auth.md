# Build Spec for Auth (JWT Login & Authorization)

- database schema: `.\database\auth.sql` (reuses `AppUser`, `AppUserRole`, `SysConfig`)

This is **not** a CRUD feature — there is no `Auth` table. It is the cross-cutting authentication +
authorization layer that sits in front of every other feature: a login endpoint that issues a signed
JWT, global bearer-token authorization on the API, and the matching Angular login page, HTTP
interceptor, route guard, and app shell.

It builds directly on the [AppUser](./AppUser.md) feature: credentials are checked against `AppUser`
(+ `AppUserRole` for roles), and the JWT signing secret is read from `SysConfig`.

---

## Summary

| Item | Detail |
|------|--------|
| Auth scheme | JWT bearer (HS256), 24-hour lifetime |
| Credential source | `AppUser` (UserId + IsActive + PasswordHash), roles from `AppUserRole` |
| Password check | `PasswordHash == SHA256(supplied password)` (lowercase hex) — same hash as AppUser |
| Signing key | `SysConfig.configValue` where `configKey='appConfig'` → JSON `symmetricSecurityKey` |
| Key ownership | One `ISigningKeyProvider` feeds **both** issuance and validation |
| Authorization | Global — every controller requires an authenticated user **except** `AuthController` |
| Token claims | `userId`, `userName`, `sub`, one `role` claim per assigned RoleId |
| Frontend session | Profile in **session storage** (`cms.auth`) — never local storage |
| Frontend Admin gating | `adminGuard` on the 系統管理 routes + `adminOnly` sidebar filter (UX only — the API is the boundary) |
| Never returned | `PasswordHash` (absent from every response DTO and model) |

---

## Login Contract

### `POST /api/Auth/login`

Anonymous (see Authorization). Request body:

```json
{ "userId": "kenny", "password": "……" }
```

**Credential check** (all failures collapse to one response so the reason is never leaked):

1. `AppUser` row exists with the supplied `UserId`.
2. `IsActive = 1`.
3. `PasswordHash = SHA256(password)` (UTF-8 bytes, lowercase hex).

If any check fails → **401** `{ "message": "invalid credentials" }`.

On success → **200** with the profile (no `PasswordHash`):

```json
{ "userId": "kenny", "userName": "Kenny Lin", "accessToken": "<signed JWT>" }
```

### `PUT /api/Auth/profile`

Self-service: the signed-in user edits **their own** display name. **Protected** — reached only with a
valid bearer token (the `login` action carries `[AllowAnonymous]`; `profile` does not, so the global
filter guards it). Request body:

```json
{ "userName": "Helen Wu" }
```

- The target **UserId is taken from the token's `userId` claim, never the request body** — a user can
  rename only their own account and can never change their UserId or roles. Any `userId` sent in the
  body is ignored (the DTO does not bind it).
- `UserName` is **required** and stored **trimmed**; empty/whitespace-only → **400**.
- Unknown user row → **404**; missing/invalid token → **401** (global filter).
- On success → **200** `{ "userId": "helen", "userName": "Helen Wu" }`. The repository writes **only**
  `AppUser.UserName` (`IAuthRepository.UpdateUserNameAsync`) — roles, IsActive, PasswordHash untouched.

### `POST /api/Auth/change-password`

Self-service: the signed-in user changes **their own** password. **Protected** (global filter); the
target UserId comes from the token's `userId` claim, never the body. Request body:

```json
{ "currentPassword": "……", "newPassword": "……", "confirmNewPassword": "……" }
```

Checks run in order; each failure → **400** with a bilingual `message` and **nothing is written**:

1. `SHA256(currentPassword)` must equal the stored `PasswordHash`
   (`目前密碼錯誤。(Current password is incorrect.)`).
2. New-password complexity (`Security/PasswordPolicy`): length ≥ 8 **and** ≥ 3 of the 4 classes —
   uppercase / lowercase / digit / symbol
   (`密碼長度至少需 8 碼，且內容須至少包含四種字元的其中三種：大寫英文／小寫英文／數字／符號 …`).
3. `newPassword` must equal `confirmNewPassword`
   (`新密碼與確認密碼不一致。(New password and confirmation do not match.)`).

On success → **204**: `PasswordHash = SHA256(newPassword)` and `PasswordUpdatedTime = GETDATE()`
(`IAuthRepository.UpdatePasswordAsync`). Passwords travel plain (over TLS) and are never trimmed;
**no hash ever crosses the API boundary** in either direction. The Angular Profile page mirrors the
complexity rule client-side (same bilingual message) and shows the server `message` on 400.

### JWT structure

- **Algorithm**: HS256, signed with the SysConfig `symmetricSecurityKey` (must be ≥ 32 bytes).
- **Lifetime**: 24 hours from issue (`JwtTokenService.TokenLifetime`); `nbf`/`exp` set accordingly.
- **Claims**: `sub` = UserId, `userId` = UserId, `userName` = UserName, and one `role` claim per
  RoleId in `AppUserRole` for the user. Claim names are constants in `Security/JwtClaims`
  (`userId`, `userName`, `role`).

---

## Signing Key (shared by issue + validate)

`ISigningKeyProvider` is the single source of the signing secret so a token can never be issued with
one key and validated with another.

- Impl `SysConfigSigningKeyProvider` (singleton, cached): reads `SysConfig` where
  `configKey='appConfig'`, parses the JSON `configValue`, returns its `symmetricSecurityKey`. The key
  is fixed for the process lifetime, so it is fetched once and reused.
- `AuthController` calls `GetSigningKey()` to sign; JWT-bearer validation resolves the same key via
  `IssuerSigningKeyResolver` (see Program.cs wiring).

---

## Authorization (global, opt-out)

Applied **globally, then relaxed on Auth** — the reverse of decorating every controller.

- In `AddControllers`, an `AuthorizeFilter` built from
  `new AuthorizationPolicyBuilder().RequireAuthenticatedUser()` is added to `options.Filters`, so
  every endpoint requires a valid bearer token by default.
- `AuthController.Login` carries `[AllowAnonymous]` — the only endpoint reachable without a token.
  (Its sibling `AuthController.UpdateProfile` has no such opt-out, so the global filter protects it.)
- Any request to a protected endpoint without a valid `Authorization: Bearer <token>` header returns
  **401** (missing, malformed, wrong-signature, or expired token all 401).

### Role-based authorization

Endpoints that need more than "any authenticated user" add `[Authorize(Roles = AppRoles.Admin)]`
(role-name constants live in `Security/AppRoles`). The JWT's `role` claims drive this because
`TokenValidationParameters.RoleClaimType = JwtClaims.Role` — **and** `options.MapInboundClaims =
false` on the bearer options. Without the latter, the handler silently rewrites incoming `role`
claims to the legacy `ClaimTypes.Role` URI and `[Authorize(Roles = …)]` never matches (every caller
gets 403). An authenticated caller lacking the role gets **403**; the example is the Admin-only
`POST /api/app-users/{id}/reset-password` (see `AppUser.md`).

### Program.cs wiring (order matters)

```csharp
// Global authorization filter
builder.Services.AddControllers(options =>
{
    var requireAuth = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.Filters.Add(new AuthorizeFilter(requireAuth));
});

builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<ISigningKeyProvider, SysConfigSigningKeyProvider>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<ISigningKeyProvider>((options, keyProvider) =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (_, _, _, _) =>
            {
                var key = keyProvider.GetSigningKey();
                return string.IsNullOrWhiteSpace(key)
                    ? [] : [new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))];
            },
            NameClaimType = JwtClaims.UserId,
            RoleClaimType = JwtClaims.Role,
        };
    });
builder.Services.AddAuthorization();

// …after CORS, before MapControllers:
app.UseAuthentication();
app.UseAuthorization();
```

---

## Backend Notes

### Models

```csharp
// LoginRequest.cs  — both required
public class LoginRequest { public string UserId; public string Password; }

// LoginResponse.cs — profile returned on success (never carries PasswordHash)
public class LoginResponse { public string UserId; public string UserName; public string AccessToken; }

// AppUserCredential.cs — backend-only projection that DOES carry PasswordHash + RoleIds;
// used solely by the login flow, never serialized to a client.
public sealed class AppUserCredential
{
    public string UserId; public string UserName; public bool IsActive;
    public string PasswordHash; public List<string> RoleIds = [];
}
```

### Repository — `IAuthRepository` / `AuthRepository`

Single method, `GetCredentialAsync(userId)`:

```sql
SELECT u.UserId, u.UserName, u.IsActive, u.PasswordHash
FROM AppUser u
WHERE u.UserId = @userId;
-- then, if found:
SELECT RoleId FROM AppUserRole WHERE UserId = @userId ORDER BY RoleId;
```

The `IsActive` check is left to the controller so the failure reason is never leaked at the SQL
layer. Signing-key reading lives in `ISigningKeyProvider`, **not** the repository.

### Services — `Security/`

| Type | Role |
|------|------|
| `PasswordHasher` (static) | `Hash(plain)` = SHA-256 → lowercase hex. **Single source of truth** — also used by `AppUserRepository` for the default-password hash. |
| `IJwtTokenService` / `JwtTokenService` | `CreateToken(userId, userName, roleIds, signingKey)` → HS256 JWT with the claims + 24h lifetime. |
| `JwtClaims` (static) | Claim-name constants: `userId`, `userName`, `role`. |
| `ISigningKeyProvider` / `SysConfigSigningKeyProvider` | Cached read of the SysConfig signing secret. |

### AuthController flow

1. `GetCredentialAsync(request.UserId)`.
2. If `null` **or** `!IsActive` **or** `PasswordHash != PasswordHasher.Hash(request.Password)` →
   `Unauthorized(new { message = "invalid credentials" })`.
3. `signingKey = _signingKeyProvider.GetSigningKey()` (throws `InvalidOperationException` if missing —
   a server misconfiguration, not a 401).
4. `CreateToken(...)` and return the `LoginResponse`.

---

## Frontend Notes

### Model (`core/models/auth.model.ts`)

```typescript
export interface LoginRequest { userId: string; password: string; }
export interface AuthProfile  { userId: string; userName: string; accessToken: string; }
```

### AuthService (`core/services/auth.service.ts`)

- `login(request)` → `POST {apiBaseUrl}/Auth/login`; on success stores the profile in
  **`sessionStorage`** under key `cms.auth` (**never** `localStorage`).
- Signals: `profile`, `userName`, `isAuthenticated`, `roles`. **Roles are decoded from the JWT**
  `role` claim (base64url-decode the payload; normalize a single string to an array) — **not** a
  separate API call.
- `token` getter, `hasRole(role)`, and `clearSession()` (removes `cms.auth`, resets the signal).

### HTTP interceptor (`core/interceptors/auth.interceptor.ts`)

Functional interceptor registered via `withInterceptors([authInterceptor])`:

- Attaches `Authorization: Bearer <token>` to every outgoing request when a token is present.
- On any **401** response → `clearSession()` + `router.navigate(['/login'])`, then rethrows.

### Route guards

Two `CanActivateFn`s stack: `authGuard` answers "signed in at all?", `adminGuard` answers "may enter
系統管理?". Both are UX affordances — a client-side guard can be bypassed by anyone willing to edit
their own session storage, so **the API's `[Authorize(Roles = …)]` remains the only real boundary**.
The guards exist so a legitimate user never lands on a page that would only 403 at them.

#### `core/guards/auth.guard.ts`

Returns `true` when a token is in session storage, otherwise `router.createUrlTree(['/login'])`. The
`/login` route is **not** guarded, so it stays public.

#### `core/guards/admin.guard.ts`

Returns `true` when `auth.hasRole(AppRoles.Admin)`. Otherwise it toasts
`沒有權限存取此頁面。(You do not have permission to access this page.)` (severity `warn`, summary
`無權限`) and redirects to `homeRouteFor(auth.roles())`. It runs **after** `authGuard` (which owns the
signed-out case), so it only ever judges an authenticated user.

#### `core/auth/app-roles.ts`

Mirrors the backend's `Security/AppRoles.cs` so the role string has one source per side, and owns
`homeRouteFor(roles)` — the single answer to "where does this user live?":

| Roles include `Admin` | Home |
|-----------------------|------|
| yes | `/app-roles` |
| no  | `/featured-promo-items` (上稿作業, the first item in their menu) |

Both the `''` redirect and `adminGuard`'s rejection target call it, so the route table and the guard
can never disagree about where home is.

### Routing & shell

- `App` is a thin root: `<router-outlet>` + `<p-toast>` + `<p-confirmdialog>` only.
- Public route `login` → the `Login` page.
- Everything else is children of a guarded parent route rendering the **`Shell`** layout
  (`layout/shell/`): `{ path: '', loadComponent: Shell, canActivate: [authGuard], children: [...] }`.
- **The 系統管理 routes sit under one path-less `canActivate: [adminGuard]` parent** inside those
  children — `app-roles`, `app-users`, `publish-statuses` and all their `/new`, `/:id`, `/:id/edit`
  descendants. This mirrors the backend's controller-wide `[Authorize(Roles = AppRoles.Admin)]`:
  **any new system/management feature goes inside that parent**, and is gated by construction rather
  than by remembering to add a guard.
- `''` and `**` both `redirectTo` a **function** (not a static path) that resolves
  `homeRouteFor(inject(AuthService).roles())`. A static redirect to `/app-roles` would bounce every
  non-admin off their own landing page the moment `adminGuard` exists.
- `Shell` = sidebar + header + content outlet. The header shows the signed-in `userName` and a
  **Logout** action (`clearSession()` → `/login`).

### Login page (`features/auth/login/`)

Reactive form (`userId`, `password`, both required) using `p-inputtext` + `p-password`. On success
navigate to `/`; on 401 show `帳號或密碼錯誤。`, otherwise a generic error toast.

### Admin-menu gating

The sidebar item **系統管理 Admin** (children 角色/發布狀態/使用者) carries an `adminOnly` flag and is
shown only when `auth.hasRole(AppRoles.Admin)`; sections left empty are dropped. Roles come from the
stored token, not an API call.

Hiding the menu is **not** enough on its own: it stops the click, not the typed URL, the bookmark, or
the back button. `adminGuard` covers those, and the two read the same `AppRoles.Admin` constant so the
menu and the routes agree.

---

## Tests

### Backend (`CMS.API.Tests`)

- `Fakes/FakeAuthRepository` (seeded `AppUserCredential`s) and `Fakes/FakeSigningKeyProvider`.
- `AuthControllerTests` (unit): valid active user → token; wrong password / unknown UserId /
  `IsActive=0` each → 401; issued JWT carries the role claims and a ~24h expiry; `LoginResponse`
  never exposes `PasswordHash`.
- `AuthorizationIntegrationTests` (`WebApplicationFactory<Program>` + `ConfigureTestServices` swapping
  `IAuthRepository` / `IAppRoleRepository` / `ISigningKeyProvider` for fakes): a protected endpoint
  returns **401 without** a token / **401 with garbage** / **200 with** a valid bearer token; and
  `POST /api/Auth/login` is reachable **without** a token (stays anonymous).

### Frontend (`CMS.NG`)

- `auth.service.spec.ts` — login stores in **session** (not local) storage; roles decode from the
  JWT; single-string role normalizes to an array; `clearSession` empties storage.
- `auth.interceptor.spec.ts` — attaches the Bearer header; no header when no token; a **401** clears
  session storage and redirects to `/login`.
- `auth.guard.spec.ts` — redirects to `/login` (UrlTree) when no token; allows activation with a token.
- `admin.guard.spec.ts` — allows an `Admin` (alone or among several roles); redirects a non-admin, a
  role-less token, and a near-miss role name (`NotAdminReally`) to `/featured-promo-items`; toasts
  once on rejection and never on success.
- `app-roles.spec.ts` — `homeRouteFor` maps Admin → `/app-roles`, everyone else → `/featured-promo-items`,
  and never returns an Admin route for a non-admin.
- `app.routes.spec.ts` — drives the **real route table through the real Router**, so the wiring is
  under test and not just the guard's decision: a signed-out visitor to `/app-users` lands on
  `/login`; an admin lands on `/app-roles` from `/` and reaches every Admin area including nested
  `/:id/edit`; a non-admin lands on `/featured-promo-items`, is bounced off every Admin area and off
  an unknown URL, and still reaches `/courses`, `/partners`, `/profile`.
- `shell.spec.ts` — the **系統管理 Admin** menu shows only when roles include `Admin`; the signed-in
  user name renders in the shell.

---

## Files to Create / Modify

### Backend

| File | Action |
|------|--------|
| `CMS.API/Models/LoginRequest.cs` | create |
| `CMS.API/Models/LoginResponse.cs` | create |
| `CMS.API/Models/AppUserCredential.cs` | create |
| `CMS.API/Security/PasswordHasher.cs` | create |
| `CMS.API/Security/JwtClaims.cs` | create |
| `CMS.API/Security/IJwtTokenService.cs` + `JwtTokenService.cs` | create |
| `CMS.API/Security/ISigningKeyProvider.cs` + `SysConfigSigningKeyProvider.cs` | create |
| `CMS.API/Repositories/IAuthRepository.cs` + `AuthRepository.cs` | create |
| `CMS.API/Controllers/AuthController.cs` | create (`[AllowAnonymous]`) |
| `CMS.API/Repositories/AppUserRepository.cs` | modify (use shared `PasswordHasher`) |
| `CMS.API/Program.cs` | modify (JWT auth, global `AuthorizeFilter`, DI, middleware) |
| `CMS.API/CMS.API.csproj` | modify (add `System.IdentityModel.Tokens.Jwt`, `Microsoft.AspNetCore.Authentication.JwtBearer`) |

### Frontend

| File | Action |
|------|--------|
| `CMS.NG/src/app/core/models/auth.model.ts` | create |
| `CMS.NG/src/app/core/services/auth.service.ts` | create |
| `CMS.NG/src/app/core/interceptors/auth.interceptor.ts` | create |
| `CMS.NG/src/app/core/guards/auth.guard.ts` | create |
| `CMS.NG/src/app/core/guards/admin.guard.ts` | create (role gate for 系統管理) |
| `CMS.NG/src/app/core/auth/app-roles.ts` | create (`AppRoles` constant + `homeRouteFor`) |
| `CMS.NG/src/app/layout/shell/shell.ts` | modify (use shared `AppRoles.Admin`, not a local literal) |
| `CMS.NG/src/app/features/auth/login/*` | create |
| `CMS.NG/src/app/layout/shell/*` | create (moved shell out of `App`) |
| `CMS.NG/src/app/app.ts` + `app.html` + `app.css` | modify (thin root) |
| `CMS.NG/src/app/app.routes.ts` | modify (public `/login`, guarded `Shell` parent, `adminGuard` parent for 系統管理, role-aware `''`/`**` redirect) |
| `CMS.NG/src/app/app.config.ts` | modify (register `authInterceptor`) |

### Tests

| File | Action |
|------|--------|
| `CMS.API.Tests/AuthControllerTests.cs` | create |
| `CMS.API.Tests/AuthorizationIntegrationTests.cs` | create |
| `CMS.API.Tests/Fakes/FakeAuthRepository.cs` + `FakeSigningKeyProvider.cs` | create |
| `CMS.API.Tests/CMS.API.Tests.csproj` | modify (add `Microsoft.AspNetCore.Mvc.Testing`) |
| `CMS.NG/.../auth.service.spec.ts`, `auth.interceptor.spec.ts`, `auth.guard.spec.ts`, `layout/shell/shell.spec.ts` | create |
| `CMS.NG/.../core/guards/admin.guard.spec.ts`, `core/auth/app-roles.spec.ts`, `app.routes.spec.ts` | create |
| `CMS.NG/src/app/app.spec.ts` | modify (thin-root assertions) |
