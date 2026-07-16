using CMS.API.Models;
using CMS.API.Pdf;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Xunit;

namespace CMS.API.Tests;

/// <summary>
/// Real QuestPDF rendering through <see cref="CourseFlyerRenderer"/> — the one test class that
/// touches Skia and the Noto Sans TC font assets (which must be copied to the test output via
/// the CMS.API project's font item). Controller tests use <see cref="Fakes.FakeCourseFlyerRenderer"/>.
/// </summary>
public class CourseFlyerRendererTests
{
    private static readonly DateOnly GeneratedOn = new(2026, 7, 16);

    private static Course SampleCourse() => new()
    {
        Pkid = 1,
        Title = "ASP.NET Core 企業級 Web API 開發實戰",
        OfficialTitle = "Building Enterprise Web APIs with ASP.NET Core",
        CourseId = "NET301",
        ProdCourseId = "PROD-NET301",
        FriendlyUrl = "net301",
        DisplayOrder = 1,
        PartnerPkid = 1,
        CourseGroupPkid = 2,
        PublishStatusPkid = 1,
        ScheduleOn = new DateOnly(2026, 8, 1),
        ScheduleOff = new DateOnly(2026, 9, 30),
        Hour = 36,
        ListPrice = 45000m,
        LearningCredit = 36m,
        Objective = "本課程帶領學員從零開始建置符合企業標準的 Web API（涵蓋 RESTful 設計原則、Dapper 資料存取）。",
        CanRepeat = false,
        PartnerName = "恆逸教育訓練中心",
        CourseGroupDescription = "微軟開發系列",
        PublishStatusIsPublished = true,
    };

    /// <summary>Rasterizes page-by-page via the real renderer's bootstrap; one PNG per page.</summary>
    private static IReadOnlyList<byte[]> RenderPages(Course course)
    {
        _ = new CourseFlyerRenderer().Render(course); // ensure lazy bootstrap (license + fonts)
        return new CourseFlyerDocument(course, GeneratedOn)
            .GenerateImages(new ImageGenerationSettings { RasterDpi = 72 })
            .ToList();
    }

    [Fact]
    public void Render_ReturnsPdfMagicBytes()
    {
        var bytes = new CourseFlyerRenderer().Render(SampleCourse());

        Assert.True(bytes.Length > 4);
        Assert.Equal("%PDF"u8.ToArray(), bytes[..4]);
    }

    [Fact]
    public void Render_TwiceInARow_BothSucceed()
    {
        var renderer = new CourseFlyerRenderer();

        var first = renderer.Render(SampleCourse());
        var second = renderer.Render(SampleCourse());

        Assert.Equal("%PDF"u8.ToArray(), first[..4]);
        Assert.Equal("%PDF"u8.ToArray(), second[..4]);
    }

    [Fact]
    public void Render_NullOptionalFields_Succeeds()
    {
        var course = SampleCourse();
        course.OfficialTitle = null;
        course.Objective = null;
        course.CourseGroupPkid = null;
        course.CourseGroupDescription = null;
        course.PartnerName = null;

        var bytes = new CourseFlyerRenderer().Render(course);

        Assert.Equal("%PDF"u8.ToArray(), bytes[..4]);
    }

    [Fact]
    public void Render_PathologicalCourse_StaysOnOnePage()
    {
        // Newline-heavy, budget-length objective + overlong title: the one-page invariant must
        // hold by construction (line clamps + footer slot), not by luck of the seed data.
        var course = SampleCourse();
        course.Title = string.Concat(Enumerable.Repeat("超長課程名稱", 33)) + "末";     // ~200 chars
        course.Objective = string.Concat(Enumerable.Repeat("目標\n", 800));            // 4000 chars, all newlines

        var pages = RenderPages(course);

        Assert.Single(pages);
    }

    [Fact]
    public void Render_DraftWatermark_PresentForDraft_AbsentForPublished()
    {
        var published = SampleCourse();
        var draft = SampleCourse();
        draft.PublishStatusIsPublished = false;

        var publishedPage = RenderPages(published).Single();
        var draftPage = RenderPages(draft).Single();
        var publishedAgain = RenderPages(published).Single();

        // Rendering is deterministic (injected date), so the ONLY difference between the two
        // variants is the watermark layer.
        Assert.Equal(publishedPage, publishedAgain);
        Assert.NotEqual(publishedPage, draftPage);
    }

    [Fact]
    public void GlyphPolicy_StrictInTests_TolerantAtRuntime()
    {
        var renderer = new CourseFlyerRenderer();
        var covered = SampleCourse();
        var emoji = SampleCourse();
        emoji.Title = "課程 🚀 火箭班";

        // Strict check (the test-fixture posture): the expected charset renders cleanly.
        // Note: we deliberately do NOT assert that an emoji throws under the strict check —
        // QuestPDF falls back to environment fonts (Segoe UI Emoji on Windows), so that
        // behavior is host-dependent, which is exactly why the runtime policy stays tolerant.
        try
        {
            _ = renderer.Render(covered); // bootstrap first (sets the runtime default)
            Settings.CheckIfAllTextGlyphsAreAvailable = true;

            _ = renderer.Render(covered);
        }
        finally
        {
            Settings.CheckIfAllTextGlyphsAreAvailable = false; // restore the runtime policy
        }

        // Runtime policy: an out-of-primary-font course renders (fallback or placeholder
        // glyph — never a 500).
        var bytes = renderer.Render(emoji);
        Assert.Equal("%PDF"u8.ToArray(), bytes[..4]);
    }
}
