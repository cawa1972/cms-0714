# Changelog

All notable changes to this project are documented in this file.
Versions use the 4-digit MAJOR.MINOR.PATCH.MICRO format.

## [0.1.0.0] - 2026-07-16

### Added
- 課程簡介 PDF 下載：Course detail now has a 下載課程簡介 PDF button that downloads a
  branded one-page A4 flyer — course title, official English title, course code, enrollment
  period, hours, NT$ price, learning credits, a truncated 課程目標 blurb, and a QR code to
  the public course page. Traditional Chinese renders in embedded Noto Sans TC.
- Unpublished courses render their flyer with a diagonal 「草稿・未發佈」 watermark, so a
  printed draft can never pass as final.
- Flyer downloads are named by the server (RFC 5987 `課程簡介-{課程名稱}.pdf`) with a
  locally-built fallback when the header is unavailable.

### Changed
- The public course QR URL now percent-encodes the course code on both the detail page and
  the flyer, so unusual course codes can't malform the printed QR target.
- Download errors are no longer silent: the page reports a clear message when the course is
  missing or the network is down (server errors keep their existing global toast).

### Fixed
- Flyer generation date prints in Taiwan time regardless of server timezone.
- A transient font-loading failure no longer disables PDF generation until restart, and
  missing font assets fail loudly instead of rendering blank glyphs.

### Infrastructure
- gstack skill-routing rules added to CLAUDE.md; project versioning (VERSION + this
  changelog) starts with this release.
