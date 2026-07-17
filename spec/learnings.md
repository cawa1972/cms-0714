# Project Learnings

Durable gotchas captured while building this app — the kind that cost real time the first
time and would cost it again. Exported from gstack's per-project learnings store
(`/learn export`); re-export to refresh rather than hand-editing entries.

## Patterns

- **interceptor-owns-5xx-toasts** (confidence: 8/10)
  `auth.interceptor.ts` already toasts every 5xx, so feature components must scope their own
  error toasts to non-5xx statuses (404 only, typically) or users get stacked double toasts.
  Component specs should assert silence on 5xx.
  Files: `src/CMS.NG/src/app/core/interceptors/auth.interceptor.ts`

- **waf-boots-program-lazy-bootstrap** (confidence: 8/10)
  Integration tests boot the real `Program.cs` via `WebApplicationFactory<Program>`, so any
  optional-feature bootstrap (font registration, native libs) placed in `Program.cs` joins the
  blast radius of app boot *and* every integration test. Keep such init lazy inside the owning
  service — `static Lazy<>` with `LazyThreadSafetyMode.ExecutionAndPublication`.
  Files: `src/CMS.API/Program.cs`, `src/CMS.API.Tests/AuthorizationIntegrationTests.cs`

## Pitfalls

- **course-model-no-instructor** (confidence: 9/10)
  The Course schema has no Instructor field and no single Description column. Flyer and report
  content must map to Title / OfficialTitle / Objective / Target / Outline. Do not spec
  documents against phantom fields.
  Files: `src/CMS.API/Models/Course.cs`, `database/course.sql`

- **ps51-utf8-roundtrip** (confidence: 9/10)
  Never round-trip this repo's files through PowerShell 5.1 text commands — its UTF-16 default
  corrupts UTF-8 sources. Use the Edit tool, or ASCII-only sed.

## Tools

- **questpdf-env-font-fallback** (confidence: 9/10)
  QuestPDF falls back to environment fonts (Segoe UI Emoji on Windows, for one) even with
  `CheckIfAllTextGlyphsAreAvailable=true`, which makes missing-glyph exceptions host-dependent.
  Never write a test asserting the glyph check throws for a specific char; assert the tolerant
  runtime path instead. Two layout notes from the same work: the `page.Footer()` slot pins a
  footer where a column `Extend()` pushes it to page 2, and `Rotate()` pivots on the top-left
  (use Unconstrained + translate-back to center a watermark).
  Files: `src/CMS.API/Pdf/CourseFlyerRenderer.cs`, `src/CMS.API/Pdf/CourseFlyerDocument.cs`

## Architecture

- **pdf-gen-questpdf-cjk** (confidence: 8/10)
  PDF generation is server-side QuestPDF (Community license) + QRCoder + embedded Noto Sans TC,
  behind an `ICourseFlyerRenderer` seam so controller tests can fake rendering. Client-side
  jsPDF/pdfmake was rejected on CJK font embedding cost. Blob downloads must be named by the
  frontend: CORS does not expose `Content-Disposition`, so the server sends an ASCII
  `course-{CourseId}.pdf`.
  Files: `src/CMS.API/Models/Course.cs`,
  `src/CMS.NG/src/app/features/courses/course-detail/course-detail.ts`
