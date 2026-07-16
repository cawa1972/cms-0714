using CMS.API.Models;
using QuestPDF;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace CMS.API.Pdf;

/// <summary>
/// QuestPDF implementation of the flyer renderer.
///
/// Bootstrap (license + CJK font registration) is deliberately lazy — NOT in Program.cs — so a
/// font-asset problem is contained to a logged 500 on the /pdf endpoint instead of failing app
/// startup and every WebApplicationFactory integration test.
/// </summary>
public sealed class CourseFlyerRenderer : ICourseFlyerRenderer
{
    /// <summary>Family name inside the registered Noto Sans TC font files.</summary>
    internal const string FontFamily = "Noto Sans TC";

    private static readonly Lazy<bool> Bootstrap = new(() =>
    {
        Settings.License = LicenseType.Community;

        // Runtime glyph policy: a character outside Noto Sans TC (emoji, CJK Ext B) renders as a
        // placeholder box instead of throwing. The rendering-test fixture flips this to true to
        // enforce font coverage of the expected charset.
        Settings.CheckIfAllTextGlyphsAreAvailable = false;

        var fontsDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");
        foreach (var fontPath in Directory.EnumerateFiles(fontsDirectory, "NotoSansTC-*.otf"))
        {
            using var stream = File.OpenRead(fontPath);
            FontManager.RegisterFont(stream);
        }

        return true;
    }, LazyThreadSafetyMode.ExecutionAndPublication);

    public byte[] Render(Course course)
    {
        _ = Bootstrap.Value;
        return new CourseFlyerDocument(course).GeneratePdf();
    }
}
