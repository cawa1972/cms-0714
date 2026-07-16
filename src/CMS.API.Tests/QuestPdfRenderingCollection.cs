using Xunit;

namespace CMS.API.Tests;

/// <summary>
/// Serializes every test class that renders through the real QuestPDF pipeline. The glyph-policy
/// test flips the process-global <c>Settings.CheckIfAllTextGlyphsAreAvailable</c>; without a
/// shared collection, xUnit runs test classes in parallel and the strict window could bleed into
/// a concurrent render in another class (host-font-dependent, intermittent).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class QuestPdfRenderingCollection
{
    public const string Name = "QuestPDF rendering";
}
