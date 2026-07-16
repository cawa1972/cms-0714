/**
 * Blob-download plumbing shared by any feature that saves a server-generated file
 * (course flyer PDF today; course catalog export later).
 *
 * Sanitization mirrors CourseFlyerFileName.SanitizeComponent on the API side — the two rule
 * sets must stay in sync (illegal + control chars → '-', trailing dots/spaces stripped,
 * 72-char cap).
 *
 * saveBlob sequence:
 *   Blob ──▶ URL.createObjectURL ──▶ hidden <a download=…> click ──▶ revokeObjectURL
 */

// Windows-illegal characters plus ASCII control characters (C0 + DEL).
// eslint-disable-next-line no-control-regex
const ILLEGAL_FILENAME_CHARS = new RegExp('[\\\\/:*?"<>|\\u0000-\\u001F\\u007F]', 'g');
const MAX_COMPONENT_LENGTH = 72;

/** Illegal + control chars → '-'; trims; strips trailing dots/spaces; caps at 72 chars. */
export function sanitizeFilename(name: string | null | undefined): string {
  if (!name?.trim()) {
    return '';
  }

  let component = name.trim().replace(ILLEGAL_FILENAME_CHARS, '-');

  if (component.length > MAX_COMPONENT_LENGTH) {
    // Never split a surrogate pair at the cap (same rule as the API side).
    let cut = MAX_COMPONENT_LENGTH;
    const boundary = component.charCodeAt(cut);
    if (boundary >= 0xdc00 && boundary <= 0xdfff) {
      cut--;
    }
    component = component.slice(0, cut);
  }

  return component.replace(/[. ]+$/, '');
}

/**
 * Client-side flyer filename — the fallback when the server's Content-Disposition is missing
 * or unparsable. Mirrors CourseFlyerFileName.Utf8 on the API side (課程簡介-{Title}.pdf,
 * empty-sanitized title → course-{pkid}.pdf) — keep the two in sync.
 */
export function buildFlyerFilename(title: string | null | undefined, pkid: number): string {
  const component = sanitizeFilename(title);
  return component ? `課程簡介-${component}.pdf` : `course-${pkid}.pdf`;
}

/**
 * Extracts the download filename from a Content-Disposition header: RFC 5987 `filename*`
 * (UTF-8, percent-encoded) preferred, plain `filename` second. Returns null when the header
 * is absent, unparsable, or contains path separators (defense-in-depth — this util is shared
 * plumbing and must not trust any endpoint) — callers fall back to a locally-built name.
 */
export function filenameFromContentDisposition(header: string | null): string | null {
  if (!header) {
    return null;
  }

  const accept = (value: string | undefined): string | null => {
    const name = value?.trim();
    return name && !name.includes('/') && !name.includes('\\') ? name : null;
  };

  const star = /filename\*=UTF-8''([^;]+)/i.exec(header);
  if (star) {
    try {
      const decoded = accept(decodeURIComponent(star[1]));
      if (decoded) {
        return decoded;
      }
    } catch {
      // Malformed percent-encoding — fall through to plain filename.
    }
  }

  const plain = /filename=(?:"([^"]*)"|([^;]+))/i.exec(header);
  return accept(plain?.[1] ?? plain?.[2]);
}

/** Triggers a browser download of the blob under the given filename. */
export function saveBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  anchor.click();
  // Deferred revoke: revoking synchronously after click() can cancel the still-starting
  // download in Safari (and historically Firefox).
  setTimeout(() => URL.revokeObjectURL(url), 0);
}
