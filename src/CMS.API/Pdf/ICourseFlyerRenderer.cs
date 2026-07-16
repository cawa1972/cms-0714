using CMS.API.Models;

namespace CMS.API.Pdf;

/// <summary>
/// Renders a course as the one-page A4 flyer PDF. Behind an interface so controller tests can
/// fake rendering (same pattern as the fake repositories); real rendering is covered by its own
/// test class.
/// </summary>
public interface ICourseFlyerRenderer
{
    byte[] Render(Course course);
}
