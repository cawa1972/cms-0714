# TODOS

## Multi-course catalog PDF export

- **What:** Select multiple courses in the course list → download one paginated PDF catalog
  (cover page + one flyer-style page per course).
- **Why:** The "10x version" of the course-flyer feature — same brand-quality output at scale
  for open-house events and sales packets.
- **Pros:** Reuses `CourseFlyerDocument`, `CourseFlyerText`, `CoursePublicUrl`, and
  `file-download.util.ts` nearly unchanged; high demo value.
- **Cons:** Multi-select UX on the list page; pagination/cover design; larger payloads
  (streaming may replace `byte[]`).
- **Context:** Designed for in the flyer feature (see
  `~/.gstack/projects/cawa1972-cms-0714/Admin-worktree-feature-course-pdf-design-20260716-114943.md`
  and `spec/custom/CourseFlyer/`). The per-course page is the flyer document; the catalog is
  either one multi-page `IDocument` over N courses or a document merge. Start at
  `src/CMS.API/Pdf/`.
- **Depends on / blocked by:** Course flyer PDF feature shipped.

## Promotion-aware flyer + anonymous share link

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
