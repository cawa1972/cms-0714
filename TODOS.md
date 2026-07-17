# TODOS

## SECURITY: no TLS on any deployed IIS binding

- **Priority:** P1
- **What:** `setup-iis.ps1` creates both sites with `New-Website -Port`, which defaults to the
  **http** protocol — no `-Ssl`, no `New-WebBinding -Protocol https`, no certificate step. Add an
  HTTPS binding + cert, redirect HTTP→HTTPS, and wire `UseHsts()` into `Program.cs`.
- **Why:** Passwords and 24-hour JWTs cross the LAN in cleartext. `LoginRequest.cs:6` and
  `ChangePasswordRequest.cs:7` both document that passwords travel "in plain text (over TLS)" — a
  contract the deployment does not satisfy. Anyone on the same L2 segment with a packet sniffer
  reads an Admin password straight out of the TCP payload, then replays the token from any host
  (`Program.cs:88-89` sets `ValidateIssuer=false`/`ValidateAudience=false`, so nothing binds it to
  a channel).
- **Pros:** The only finding needing no prior access at all. Cert + binding is a contained change
  to `setup-iis.ps1`; `UseHsts()` is one line.
- **Cons:** Needs a certificate (internal CA, or self-signed for an intranet box). `New-Website`
  with no `-HostHeader` binds `*:80`/`*:5001`, so the "Localhost" default is LAN-reachable too —
  don't assume the default config is safe.
- **DO NOT fix by changing `ASPNETCORE_ENVIRONMENT`.** `DEPLOY-IIS.md:138` and
  `deploy/CMS.API/web.config.template:24-28` justify pinning `Development` by claiming Program.cs
  calls `UseHsts()`/`UseHttpsRedirection()` only when `IsProduction()`. **That code does not
  exist** — grep for `UseHttpsRedirection|UseHsts|RequireHttps` across `src` returns nothing.
  Both docs are stale and must be corrected as part of this fix.
- **Context:** /cso full audit (2026-07-17), Finding 1, HIGH, confidence 8/10, independently
  verified. Full detail: `.gstack/security-reports/2026-07-17-112429.json`.
- **Depends on / blocked by:** A certificate decision (internal CA vs. self-signed).

## SECURITY: shared default password with no forced rotation

- **Priority:** P1
- **What:** Add a `MustChangePassword` flag (or read `PasswordUpdatedTime` against a policy), set
  it in `AppUserRepository.CreateAsync` and `ResetPasswordAsync`, and check it in
  `AuthController.Login` before issuing a token — force the change-password flow.
- **Why:** `CreateAsync` (:96) and `ResetPasswordAsync` (:181) both resolve one global
  `SysConfig 'appConfig'.defaultPassword`. Salting makes the stored hashes differ, but the
  **plaintext is identical for every user ever created or reset**. Login (`AuthController.cs:55-60`)
  checks only null/`IsActive`/`Verify` — nothing consults password age. No `MustChangePassword`
  column exists in `database/auth.sql:25-36`; `PasswordUpdatedTime` is stamped on reset (:186) but
  never read by any authorization path. Exploit chain: any ex-onboarded user knows the default →
  pulls the roster from `GET /api/lookups/app-users` (no role attribute, returns every row to any
  authenticated caller) → sprays it, with no lockout to slow them. An admin who was reset and
  hasn't signed back in yields an Admin token.
- **Pros:** Breaks the chain at its source. Cheap secondary win: restrict
  `/api/lookups/app-users` to Admin — it has zero non-admin consumers today (`lookup.service.ts:13`
  is called only from `app-role-form.ts:61` and `app-role-detail.ts:33`, both behind `adminGuard`).
- **Cons:** Touches schema, repository, controller, and the Angular login flow. Restricting the
  lookup contradicts the `spec/cross-cutting.md` rule that `/api/lookups/*` is the non-admin picker
  surface — decide deliberately.
- **Check before fixing:** `ResetPasswordAuthorizationTests.cs:145` hard-codes `CMS4fun#` as the
  default. Suppressed as a test fixture (it appears nowhere in non-test code), but it does not read
  like a placeholder. **If it matches the live SysConfig value, the key to this exploit chain is in
  a committed file** — verify against the database and rotate the value if so.
- **Context:** /cso full audit (2026-07-17), Finding 2, HIGH, confidence 8/10, independently
  verified. Full detail: `.gstack/security-reports/2026-07-17-112429.json`.
- **Depends on / blocked by:** Nothing.

## SECURITY: Swagger served unauthenticated in every environment

- **Priority:** P2
- **What:** Gate `Program.cs:113-117` on `app.Environment.IsDevelopment()`, or add
  `.RequireAuthorization()` and move the registration below line 122. Delete the false comment at
  `deploy/CMS.API/web.config.template:28`.
- **Why:** `UseSwagger()`/`UseSwaggerUI()` are unconditional — no `IsDevelopment()` gate exists
  anywhere in `src`. The global `AuthorizeFilter` cannot cover them: it is an **MVC filter**, so it
  runs only inside the action pipeline reached via `MapControllers()` (:124). Independently fatal —
  Swagger is registered at :113, *before* `UseAuthentication()` at :121. `setup-iis.ps1:228` binds
  the API to `*:5001` with no host header, so any unauthenticated party on the network can
  `GET /swagger/v1/swagger.json` and receive the full API map including every Admin-only route.
  The loopback CORS policy (`Program.cs:40`) does not help — it constrains browser JS, not curl.
- **Pros:** Two-line change. Optionally bind the API site to `127.0.0.1`, since the ARR proxy is
  its only intended caller.
- **Cons:** Loses Swagger as a post-deploy smoke test on the server (`DEPLOY-IIS.md:149` leans on
  it for troubleshooting). Disclosure only — the endpoints themselves still enforce auth.
- **Trap:** `web.config.template:28` claims "Swagger is registered ONLY when IsDevelopment()".
  It is stale and false. Anyone who trusts it will believe flipping the environment name closed
  this. It did not.
- **Context:** /cso full audit (2026-07-17), Finding 3, MEDIUM, confidence 9/10, independently
  verified. Full detail: `.gstack/security-reports/2026-07-17-112429.json`.
- **Depends on / blocked by:** Nothing.

## Auth interceptor: parse Blob error bodies on 5xx

- **Priority:** P3
- **What:** The flyer download introduced the app's first `responseType: 'blob'` request. On a
  5xx, Angular delivers `error.error` as a Blob, so the interceptor's `body?.message` probe
  misses and users always see the generic fallback toast instead of the exception middleware's
  message.
- **Why:** Slightly better error messages for blob endpoints (flyer today, catalog export later).
- **Pros:** One `error.error instanceof Blob` branch + async read; a spec pins the behavior.
- **Cons:** Touches the shared auth interceptor; current behavior (generic toast) is degraded
  but correct, so priority is low.
- **Context:** Flagged by the /ship red-team review (2026-07-16) as an integration-boundary gap
  in `src/CMS.NG/src/app/core/interceptors/auth.interceptor.ts:36`.
- **Depends on / blocked by:** Nothing.

## Multi-course catalog PDF export

- **Priority:** P2
- **What:** Select multiple courses in the course list → download one paginated PDF catalog
  (cover page + one flyer-style page per course).
- **Why:** The "10x version" of the course-flyer feature — same brand-quality output at scale
  for open-house events and sales packets.
- **Pros:** Reuses `CourseFlyerDocument`, `CourseFlyerText`, `CoursePublicUrl`, and
  `file-download.util.ts` nearly unchanged; high demo value.
- **Cons:** Multi-select UX on the list page; pagination/cover design; larger payloads
  (streaming may replace `byte[]`).
- **Context:** Designed for in the flyer feature — wireframe and mockup in
  `spec/custom/CourseFlyer/` (repo). Key decisions carried forward: one flyer-style page per
  course reusing `CourseFlyerDocument`; catalog is either one multi-page `IDocument` over N
  courses or a document merge. Start at `src/CMS.API/Pdf/`.
- **Depends on / blocked by:** Course flyer PDF feature shipped.

## Promotion-aware flyer + anonymous share link

- **Priority:** P3
- **What:** (a) Stamp a live promotion ribbon/callout on the flyer when `Promotion2` has an
  active `ScheduleOn`–`ScheduleOff` window matching the course's partner or course group.
  (b) "Copy share link" button minting a short-lived signed token so the flyer PDF can be
  downloaded without logging in (shareable via LINE/email).
- **Why:** Turns a static flyer into a time-aware sales artifact and lets staff send the PDF
  itself to customers, closing the loop the QR code starts.
- **Pros:** `Promotion2` schema already has the needed columns; the signed-link exercise is the
  most instructive auth work available in this codebase.
- **Cons / SECURITY CONSTRAINT:** The share link deliberately pierces the hard guardrail that
  `AuthController.Login` is the ONLY anonymous endpoint. It must be a deliberate, token-scoped
  exception: short TTL, single-purpose claim (course pkid), signed via `ISigningKeyProvider`,
  validated without granting any session. Do not implement casually.
- **Context:** Proposed by the cross-model second opinion during the flyer design session
  (2026-07-16); explicitly rejected for v1 (Approach C at decision D8) as scope-doubling.
  `Promotion2` lives in `database/promotion.sql`; no repository/feature exists for it yet.
- **Depends on / blocked by:** Course flyer PDF feature shipped; a Promotion feature
  (repository + at least read access) built.

## Data cleanup: Course NINS-1 has 下架日期 before 上架日期

- **Priority:** P3
- **What:** Course pkid 1980 (`NINS-1`, 網路基礎架構與網路服務(測試)) has 上架日期
  2023/02/17 and 下架日期 2013/12/17 — ten years backwards. Correct the seed data directly
  (SQL update) once the intended real end date is known.
- **Why:** `/qa` on 2026-07-17 found and fixed the code-level bug that let this happen (ISSUE-001 —
  see `.gstack/qa-reports/qa-report-cms-2026-07-17.md`), but the fix only blocks *new* invalid
  saves; it does not retroactively correct rows already in the database. This record now can't be
  re-saved unchanged through the UI (the new validation rejects it), so it will stay stuck until
  someone corrects the date and saves.
- **Pros:** One-row `UPDATE`; trivial once someone confirms the intended date range.
- **Cons:** Needs a human decision on what the correct 下架日期 should be — not guessable from
  the data alone.
- **Context:** `/qa` full-project sweep (2026-07-17). Full detail:
  `.gstack/qa-reports/qa-report-cms-2026-07-17.md`.
- **Depends on / blocked by:** Nothing — just needs the correct date.

## Completed

- **Course form accepted end date before start date** — fixed by `/qa` on 2026-07-17,
  commit `24edf1e`. See `.gstack/qa-reports/qa-report-cms-2026-07-17.md` (ISSUE-001).
