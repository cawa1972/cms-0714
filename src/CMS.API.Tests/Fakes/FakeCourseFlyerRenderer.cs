using CMS.API.Models;
using CMS.API.Pdf;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory stand-in for the QuestPDF renderer so controller tests never touch Skia or font
/// assets (same pattern as the fake repositories). Returns bytes starting with the %PDF magic
/// so content-shape assertions stay meaningful, and records the last rendered course.
/// </summary>
public class FakeCourseFlyerRenderer : ICourseFlyerRenderer
{
    public Course? LastRendered { get; private set; }

    public byte[] Render(Course course)
    {
        LastRendered = course;
        return "%PDF-fake"u8.ToArray();
    }
}
