/**
 * Date helpers shared across features that use `date` (DateOnly) columns.
 *
 * IMPORTANT: serialization uses LOCAL date components, never `toISOString()`. For a UTC+8
 * user, `new Date(2026, 0, 1).toISOString()` yields `2025-12-31T16:00:00Z`, so
 * `.split('T')[0]` would send the wrong day. Building the string from `getFullYear()` /
 * `getMonth()` / `getDate()` keeps the calendar day the user picked.
 */

function pad(n: number): string {
  return n < 10 ? `0${n}` : `${n}`;
}

/** Date → 'yyyy-MM-dd' using local components. Returns null for null/undefined. */
export function toIso(d: Date | null | undefined): string | null {
  if (!d) {
    return null;
  }
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

/** 'yyyy-MM-dd' → Date at local midnight. Returns null for null/undefined/empty. */
export function fromIso(iso: string | null | undefined): Date | null {
  if (!iso) {
    return null;
  }
  return new Date(iso + 'T00:00:00');
}
