import {
  filenameFromContentDisposition,
  sanitizeFilename,
  saveBlob,
} from './file-download.util';

describe('sanitizeFilename', () => {
  it('returns empty string for null/undefined/whitespace', () => {
    expect(sanitizeFilename(null)).toBe('');
    expect(sanitizeFilename(undefined)).toBe('');
    expect(sanitizeFilename('   ')).toBe('');
  });

  it('keeps ordinary Chinese and ASCII text unchanged', () => {
    expect(sanitizeFilename('ASP.NET Core 開發實戰')).toBe('ASP.NET Core 開發實戰');
  });

  it('replaces Windows-illegal characters with dashes', () => {
    expect(sanitizeFilename('CI/CD 實戰')).toBe('CI-CD 實戰');
    expect(sanitizeFilename('a\\b:c*d?e"f<g>h|i')).toBe('a-b-c-d-e-f-g-h-i');
  });

  it('replaces control characters with dashes', () => {
    expect(sanitizeFilename('A\tB')).toBe('A-B');
    expect(sanitizeFilename('A\u0007B')).toBe('A-B');
  });

  it('strips trailing dots and spaces (Windows rejects them)', () => {
    expect(sanitizeFilename('進階課程 . . ')).toBe('進階課程');
  });

  it('caps at 72 characters', () => {
    expect(sanitizeFilename('A'.repeat(200))).toBe('A'.repeat(72));
  });

  it('never splits a surrogate pair at the cap', () => {
    // 71 BMP chars then U+20000 (two code units) straddling the 72-char cap.
    const result = sanitizeFilename('課'.repeat(71) + '𠀀' + '課'.repeat(30));
    expect(result).toBe('課'.repeat(71));
  });
});

describe('filenameFromContentDisposition', () => {
  it('returns null for a missing header', () => {
    expect(filenameFromContentDisposition(null)).toBeNull();
  });

  it('prefers the RFC 5987 filename* parameter and decodes it', () => {
    const header =
      "attachment; filename=course-NET301.pdf; filename*=UTF-8''%E8%AA%B2%E7%A8%8B%E7%B0%A1%E4%BB%8B-Azure.pdf";
    expect(filenameFromContentDisposition(header)).toBe('課程簡介-Azure.pdf');
  });

  it('falls back to plain filename when filename* is absent', () => {
    expect(filenameFromContentDisposition('attachment; filename="report.pdf"')).toBe('report.pdf');
    expect(filenameFromContentDisposition('attachment; filename=report.pdf')).toBe('report.pdf');
  });

  it('falls back to plain filename when filename* is malformed', () => {
    const header = "attachment; filename=ok.pdf; filename*=UTF-8''%E8%ZZ";
    expect(filenameFromContentDisposition(header)).toBe('ok.pdf');
  });

  it('returns null when no filename parameter exists', () => {
    expect(filenameFromContentDisposition('attachment')).toBeNull();
  });
});

describe('saveBlob', () => {
  it('creates an object URL, clicks a download anchor, then revokes the URL', () => {
    const createSpy = spyOn(URL, 'createObjectURL').and.returnValue('blob:fake-url');
    const revokeSpy = spyOn(URL, 'revokeObjectURL');
    const clickSpy = spyOn(HTMLAnchorElement.prototype, 'click');

    saveBlob(new Blob(['%PDF-fake']), '課程簡介-test.pdf');

    expect(createSpy).toHaveBeenCalledTimes(1);
    expect(clickSpy).toHaveBeenCalledTimes(1);
    expect(revokeSpy).toHaveBeenCalledWith('blob:fake-url');
  });
});
