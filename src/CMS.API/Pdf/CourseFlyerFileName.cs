using System.Text;
using CMS.API.Models;

namespace CMS.API.Pdf;

/// <summary>
/// Download filenames for the flyer PDF (outside voice findings 7 + 9).
///
/// The endpoint sends BOTH Content-Disposition parameters explicitly:
///   filename  = <see cref="Ascii"/>  — plain-ASCII fallback for legacy agents,
///   filename* = <see cref="Utf8"/>   — RFC 5987 UTF-8 name shown by modern browsers.
///
/// Sanitization mirrors <c>sanitizeFilename</c> in the frontend's file-download util (the
/// client-side fallback name) — keep the two rule sets in sync.
/// </summary>
public static class CourseFlyerFileName
{
    private const int MaxComponentLength = 72;
    private static readonly char[] IllegalCharacters = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

    /// <summary>
    /// "course-{CourseId}.pdf" with CourseId reduced to ASCII-safe characters. CourseId is
    /// varchar(50) under a Chinese collation, so "always ASCII" is a convention, not a
    /// constraint — when nothing survives, falls back to "course-{pkid}.pdf".
    /// </summary>
    public static string Ascii(Course course)
    {
        var safe = new StringBuilder(course.CourseId.Length);
        foreach (var c in course.CourseId)
        {
            if (c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '.' or '_' or '-')
                safe.Append(c);
        }

        var component = TrimTrailingDotsAndSpaces(safe.ToString());
        return component.Length == 0 ? $"course-{course.Pkid}.pdf" : $"course-{component}.pdf";
    }

    /// <summary>
    /// "課程簡介-{Title}.pdf" with the shared sanitization rules (illegal + control chars → '-',
    /// trailing dots/spaces stripped, component capped). Empty result → "course-{pkid}.pdf".
    /// </summary>
    public static string Utf8(Course course)
    {
        var component = SanitizeComponent(course.Title);
        return component.Length == 0 ? $"course-{course.Pkid}.pdf" : $"課程簡介-{component}.pdf";
    }

    /// <summary>Shared rules: illegal filename chars and control chars → '-'; trailing dots and
    /// spaces stripped (Windows rejects them); capped at 72 chars (leaves headroom for the
    /// prefix + extension under common 255-byte filesystem limits).</summary>
    public static string SanitizeComponent(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var safe = new StringBuilder(name.Length);
        foreach (var c in name.Trim())
            safe.Append(char.IsControl(c) || IllegalCharacters.Contains(c) ? '-' : c);

        var component = safe.ToString();
        if (component.Length > MaxComponentLength)
        {
            // Same rule as CourseFlyerText.TruncateObjective: never split a surrogate pair.
            var cut = MaxComponentLength;
            if (char.IsLowSurrogate(component[cut]))
                cut--;
            component = component[..cut];
        }

        return TrimTrailingDotsAndSpaces(component);
    }

    private static string TrimTrailingDotsAndSpaces(string value) => value.TrimEnd('.', ' ');
}
