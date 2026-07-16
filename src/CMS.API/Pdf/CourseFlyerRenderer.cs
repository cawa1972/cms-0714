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

    // PublicationOnly (not ExecutionAndPublication): that mode would CACHE a factory exception,
    // so one transient failure (font file locked by an AV scan, IO hiccup) would poison every
    // subsequent request until process restart. PublicationOnly retries on the next request;
    // concurrent first requests may each run the factory, which is harmless here (the license
    // assignment and font registration tolerate repetition).
    private static readonly Lazy<bool> Bootstrap = new(() =>
    {
        Settings.License = LicenseType.Community;

        // Runtime glyph policy: a character outside Noto Sans TC (emoji, CJK Ext B) renders as a
        // placeholder box instead of throwing. The rendering-test fixture flips this to true to
        // enforce font coverage of the expected charset.
        Settings.CheckIfAllTextGlyphsAreAvailable = false;

        var fontsDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");
        var registered = 0;
        foreach (var fontPath in Directory.EnumerateFiles(fontsDirectory, "NotoSansTC-*.otf"))
        {
            using var stream = File.OpenRead(fontPath);
            FontManager.RegisterFont(stream);
            registered++;
        }

        // Zero matches would otherwise "succeed" and every flyer would render CJK as tofu with
        // an HTTP 200 — fail loudly instead (missing directory already throws above).
        if (registered == 0)
            throw new InvalidOperationException(
                $"No Noto Sans TC font assets found in '{fontsDirectory}' — publish output is missing Assets/Fonts.");

        return true;
    }, LazyThreadSafetyMode.PublicationOnly);

    public byte[] Render(Course course)
    {
        _ = Bootstrap.Value;
        return new CourseFlyerDocument(course).GeneratePdf();
    }
}
