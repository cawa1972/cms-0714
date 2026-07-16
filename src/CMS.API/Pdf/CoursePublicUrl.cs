using CMS.API.Models;

namespace CMS.API.Pdf;

/// <summary>
/// Canonical public course-page URL, used for the QR code on the flyer PDF.
/// The same template is duplicated client-side in
/// <c>src/CMS.NG/src/app/features/courses/course-detail/course-detail.ts</c> (on-screen QR) —
/// keep both in sync when the public site changes.
/// </summary>
public static class CoursePublicUrl
{
    public static string For(Course course)
        => $"https://www.uuu.com.tw/Course/Show/{course.Pkid}/{course.CourseId}";
}
