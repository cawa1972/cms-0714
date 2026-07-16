using CMS.API.Models;
using CMS.API.Pdf;
using Xunit;

namespace CMS.API.Tests;

/// <summary>
/// The flyer's text-shaping rules (pure functions — eng review 3A). The truncation tests aim at
/// both axes: the plain char-count boundary AND the surrogate-pair boundary that a naive
/// Substring would corrupt (outside voice finding 2).
/// </summary>
public class CourseFlyerTextTests
{
    // ---- TruncateObjective: null/empty handling --------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TruncateObjective_NullOrWhitespace_ReturnsNull(string? input)
        => Assert.Null(CourseFlyerText.TruncateObjective(input));

    [Fact]
    public void TruncateObjective_TrimsSurroundingWhitespace()
        => Assert.Equal("課程簡介", CourseFlyerText.TruncateObjective("  課程簡介  "));

    // ---- TruncateObjective: plain boundary (279/280/281) -----------------

    [Fact]
    public void TruncateObjective_AtBudgetMinusOne_Unchanged()
    {
        var text = new string('課', 279);
        Assert.Equal(text, CourseFlyerText.TruncateObjective(text));
    }

    [Fact]
    public void TruncateObjective_ExactlyAtBudget_Unchanged()
    {
        var text = new string('課', 280);
        Assert.Equal(text, CourseFlyerText.TruncateObjective(text));
    }

    [Fact]
    public void TruncateObjective_OneOverBudget_TruncatedWithEllipsis()
    {
        var text = new string('課', 281);

        var result = CourseFlyerText.TruncateObjective(text);

        Assert.Equal(new string('課', 280) + CourseFlyerText.Ellipsis, result);
    }

    // ---- TruncateObjective: surrogate-pair boundary ----------------------

    [Fact]
    public void TruncateObjective_SurrogatePairStraddlingBudget_DroppedWhole()
    {
        // '𠀀' (U+20000, CJK Ext B) is one text element of TWO UTF-16 code units. Placed at
        // positions 280-281, a naive Substring(0, 280) would slice it in half.
        var text = new string('課', 279) + "𠀀" + new string('課', 10);

        var result = CourseFlyerText.TruncateObjective(text);

        Assert.Equal(new string('課', 279) + CourseFlyerText.Ellipsis, result);
    }

    [Fact]
    public void TruncateObjective_TruncatedResult_ContainsNoLoneSurrogate()
    {
        var text = string.Concat(Enumerable.Repeat("𠀀", 200)); // 400 code units of astral chars

        var result = CourseFlyerText.TruncateObjective(text)!;

        for (var i = 0; i < result.Length; i++)
        {
            if (char.IsHighSurrogate(result[i]))
            {
                Assert.True(i + 1 < result.Length && char.IsLowSurrogate(result[i + 1]),
                    $"lone high surrogate at index {i}");
                i++;
            }
            else
            {
                Assert.False(char.IsLowSurrogate(result[i]), $"lone low surrogate at index {i}");
            }
        }
    }

    [Fact]
    public void TruncateObjective_PreservesNewlines_WhenUnderBudget()
    {
        var text = "第一行\n第二行";
        Assert.Equal(text, CourseFlyerText.TruncateObjective(text));
    }

    // ---- FormatPrice ------------------------------------------------------

    [Theory]
    [InlineData(45000, "NT$ 45,000")]
    [InlineData(500, "NT$ 500")]
    [InlineData(1234500, "NT$ 1,234,500")]
    [InlineData(0, "NT$ 0")]
    public void FormatPrice_ThousandsSeparated_NoDecimals(decimal price, string expected)
        => Assert.Equal(expected, CourseFlyerText.FormatPrice(price));

    // ---- FormatCredit -----------------------------------------------------

    [Theory]
    [InlineData("36", "36")]
    [InlineData("36.0", "36")]
    [InlineData("4.5", "4.5")]
    [InlineData("4.50", "4.5")]
    [InlineData("0.25", "0.25")]
    [InlineData("0", "0")]
    public void FormatCredit_TrimsTrailingZeros(string credit, string expected)
        => Assert.Equal(expected, CourseFlyerText.FormatCredit(decimal.Parse(credit)));

    // ---- Dates --------------------------------------------------------------

    [Fact]
    public void FormatDate_UsesSlashFormat_MatchingDetailPage()
        => Assert.Equal("2026/08/01", CourseFlyerText.FormatDate(new DateOnly(2026, 8, 1)));

    [Fact]
    public void FormatDateRange_JoinsWithEnDash()
        => Assert.Equal("2026/08/01 – 2026/09/30",
            CourseFlyerText.FormatDateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 30)));
}

/// <summary>Pins the public course URL shape shared with course-detail.ts (on-screen QR).</summary>
public class CoursePublicUrlTests
{
    [Fact]
    public void For_BuildsPkidAndCourseIdUrl()
    {
        var course = new Course { Pkid = 123, CourseId = "NET301" };

        Assert.Equal("https://www.uuu.com.tw/Course/Show/123/NET301", CoursePublicUrl.For(course));
    }
}
