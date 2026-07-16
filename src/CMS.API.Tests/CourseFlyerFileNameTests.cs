using CMS.API.Models;
using CMS.API.Pdf;
using Xunit;

namespace CMS.API.Tests;

/// <summary>
/// Download-filename rules (outside voice findings 7 + 11). The Big5-CourseId cases exist
/// because CourseId is varchar(50) under a Chinese collation — "always ASCII" is a convention,
/// not a constraint.
/// </summary>
public class CourseFlyerFileNameTests
{
    private static Course CourseWith(string courseId = "NET301", string title = "課程", int pkid = 7)
        => new() { Pkid = pkid, CourseId = courseId, Title = title };

    // ---- Ascii (filename=) ------------------------------------------------

    [Fact]
    public void Ascii_PlainCourseId_UsedDirectly()
        => Assert.Equal("course-NET301.pdf", CourseFlyerFileName.Ascii(CourseWith("NET301")));

    [Fact]
    public void Ascii_Big5CourseId_KeepsAsciiSurvivors()
        => Assert.Equal("course-301.pdf", CourseFlyerFileName.Ascii(CourseWith("課程301")));

    [Fact]
    public void Ascii_FullyNonAsciiCourseId_FallsBackToPkid()
        => Assert.Equal("course-7.pdf", CourseFlyerFileName.Ascii(CourseWith("課程編號")));

    [Fact]
    public void Ascii_StripsUnsafePunctuation()
        => Assert.Equal("course-NET_301.x.pdf", CourseFlyerFileName.Ascii(CourseWith("NET_3:01/.x")));

    // ---- Utf8 (filename*=) --------------------------------------------------

    [Fact]
    public void Utf8_ChineseTitle_PrefixedFlyerName()
        => Assert.Equal("課程簡介-ASP.NET Core 開發實戰.pdf",
            CourseFlyerFileName.Utf8(CourseWith(title: "ASP.NET Core 開發實戰")));

    [Fact]
    public void Utf8_IllegalCharacters_ReplacedWithDash()
        => Assert.Equal("課程簡介-CI-CD 實戰.pdf", CourseFlyerFileName.Utf8(CourseWith(title: "CI/CD 實戰")));

    [Fact]
    public void Utf8_ControlCharacters_ReplacedWithDash()
        => Assert.Equal("課程簡介-A-B.pdf", CourseFlyerFileName.Utf8(CourseWith(title: "A\tB")));

    [Fact]
    public void Utf8_TrailingDotsAndSpaces_Stripped()
        => Assert.Equal("課程簡介-進階課程.pdf", CourseFlyerFileName.Utf8(CourseWith(title: "進階課程 . . ")));

    [Fact]
    public void Utf8_EmptyTitle_FallsBackToPkid()
        => Assert.Equal("course-7.pdf", CourseFlyerFileName.Utf8(CourseWith(title: "   ")));

    [Fact]
    public void Utf8_OverlongTitle_CappedWithoutSplittingSurrogatePair()
    {
        // 71 BMP chars then '𠀀' (2 code units) straddling the 72-char cap — must drop whole.
        var title = new string('課', 71) + "𠀀" + new string('課', 30);

        var result = CourseFlyerFileName.Utf8(CourseWith(title: title));

        Assert.Equal($"課程簡介-{new string('課', 71)}.pdf", result);
    }

    [Fact]
    public void SanitizeComponent_CapsAt72Characters()
    {
        var result = CourseFlyerFileName.SanitizeComponent(new string('A', 200));

        Assert.Equal(new string('A', 72), result);
    }
}
