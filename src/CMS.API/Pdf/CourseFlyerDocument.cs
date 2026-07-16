using CMS.API.Models;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CMS.API.Pdf;

/// <summary>
/// The one-page A4 course flyer. Pure function of a <see cref="Course"/> (+ an injectable
/// generation date for testability) — no I/O.
///
/// Layout (approved wireframe: spec/custom/CourseFlyer/wireframe.png):
///
///   ┌────────────────────────────────────────────────┐
///   │ PartnerName                 課程簡介 Course Flyer│  brand band (rule below)
///   ├────────────────────────────────────────────────┤
///   │ Title (zh, ≤2 lines)                           │  title block
///   │ OfficialTitle (en, ≤1 line, omitted if null)   │
///   │ [課程代碼 CourseId]                             │
///   │ ┌──────────┬────────┬─────────┬──────────┐     │  info grid
///   │ │ 報名期間  │ 總時數  │ 定價    │ 學習點數  │     │
///   │ ├──────────┴────────┴─────────┴──────────┤     │
///   │ │ ▎課程目標 — Objective (clamped lines)   │     │  blurb (omitted if null)
///   │ └─────────────────────────────────────────┘    │
///   │  ... flexible space ...                        │
///   ├────────────────────────────────────────────────┤
///   │ 課程分類 / 產生日期 / 資料以官網為準      [QR]   │  footer (rule above)
///   └────────────────────────────────────────────────┘
///
/// One-page enforcement: Title/OfficialTitle/Objective are line-clamped, so page geometry is
/// constant regardless of data. Unpublished courses get a diagonal 「草稿・未發佈」 watermark.
/// </summary>
public sealed class CourseFlyerDocument : IDocument
{
    private readonly Course _course;
    private readonly DateOnly _generatedOn;

    // The printed 文件產生日期 is business-local (Taiwan), not server-local: on a UTC host,
    // DateTime.Now would print yesterday's date between 00:00–08:00 Taipei time.
    private static readonly TimeZoneInfo TaiwanTimeZone = ResolveTaiwanTimeZone();

    public CourseFlyerDocument(Course course, DateOnly? generatedOn = null)
    {
        _course = course;
        _generatedOn = generatedOn
            ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, TaiwanTimeZone));
    }

    private static TimeZoneInfo ResolveTaiwanTimeZone()
    {
        // IANA id works cross-platform on .NET 6+ (ICU); Windows id as fallback; local as last
        // resort. Catches Exception, not just TimeZoneNotFoundException: this initializes a
        // static field, and .NET caches a type-initializer failure — any other throw here
        // (corrupt tz data) would otherwise poison every flyer render until process restart.
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"); }
        catch (Exception)
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time"); }
            catch (Exception) { return TimeZoneInfo.Local; }
        }
    }

    public DocumentMetadata GetMetadata()
    {
        var metadata = DocumentMetadata.Default;
        metadata.Title = _course.Title;
        metadata.Author = _course.PartnerName ?? "CMS";
        return metadata;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(42);
            page.DefaultTextStyle(style => style.FontFamily(CourseFlyerRenderer.FontFamily).FontSize(11));

            if (!_course.PublishStatusIsPublished)
                page.Foreground().Element(ComposeWatermark);

            page.Content().Column(column =>
            {
                column.Item().Element(ComposeBrandBand);
                column.Item().PaddingTop(28).Element(ComposeTitleBlock);
                column.Item().PaddingTop(24).Element(ComposeInfoGrid);

                var objective = CourseFlyerText.TruncateObjective(_course.Objective);
                if (objective is not null)
                    column.Item().PaddingTop(24).Element(c => ComposeObjective(c, objective));
            });

            // The footer slot pins to the page bottom; a column Extend() would instead push
            // this onto a second page.
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeBrandBand(IContainer container)
    {
        container.BorderBottom(2.5f).BorderColor(Colors.Grey.Darken3).PaddingBottom(10)
            .Row(row =>
            {
                row.RelativeItem().AlignBottom()
                    .Text(_course.PartnerName ?? string.Empty).FontSize(18).Bold();
                row.AutoItem().AlignBottom()
                    .Text("課程簡介 Course Flyer").FontSize(10).FontColor(Colors.Grey.Darken1);
            });
    }

    private void ComposeTitleBlock(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text(_course.Title).FontSize(24).Bold().LineHeight(1.25f).ClampLines(2);

            if (!string.IsNullOrWhiteSpace(_course.OfficialTitle))
                column.Item().PaddingTop(4)
                    .Text(_course.OfficialTitle).FontSize(12).FontColor(Colors.Grey.Darken1).ClampLines(1);

            column.Item().PaddingTop(10).Row(row =>
                row.AutoItem().Border(0.75f).BorderColor(Colors.Grey.Darken1)
                    .PaddingVertical(2).PaddingHorizontal(8)
                    .Text($"課程代碼 {_course.CourseId}").FontSize(9).FontColor(Colors.Grey.Darken2));
        });
    }

    private void ComposeInfoGrid(IContainer container)
    {
        container.Border(0.75f).BorderColor(Colors.Grey.Lighten1).Row(row =>
        {
            Cell(row.RelativeItem(1.5f), "報名期間",
                CourseFlyerText.FormatDateRange(_course.ScheduleOn, _course.ScheduleOff), borderLeft: false);
            Cell(row.RelativeItem(0.8f), "總時數", $"{_course.Hour} 小時");
            Cell(row.RelativeItem(1.1f), "定價", CourseFlyerText.FormatPrice(_course.ListPrice));
            Cell(row.RelativeItem(0.8f), "學習點數", $"{CourseFlyerText.FormatCredit(_course.LearningCredit)} 點");
        });

        static void Cell(IContainer cell, string label, string value, bool borderLeft = true)
        {
            (borderLeft ? cell.BorderLeft(0.75f).BorderColor(Colors.Grey.Lighten1) : cell)
                .Padding(10).Column(column =>
                {
                    column.Item().Text(label).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                    column.Item().PaddingTop(3).Text(value).FontSize(12).SemiBold();
                });
        }
    }

    private static void ComposeObjective(IContainer container, string objective)
    {
        container.Column(column =>
        {
            column.Item().BorderLeft(3.5f).BorderColor(Colors.Grey.Darken3).PaddingLeft(8)
                .Text("課程目標").FontSize(12).SemiBold();

            column.Item().PaddingTop(8)
                .Text(objective).FontSize(10.5f).LineHeight(1.7f).ClampLines(7);

            if (objective.EndsWith(CourseFlyerText.Ellipsis, StringComparison.Ordinal))
                column.Item().PaddingTop(6)
                    .Text("▸ 完整課程內容請掃描右下角 QR Code 查看官網")
                    .FontSize(8.5f).FontColor(Colors.Grey.Darken1);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        var qrPng = CreateQrPng(CoursePublicUrl.For(_course));

        container.BorderTop(0.75f).BorderColor(Colors.Grey.Lighten1).PaddingTop(12)
            .Row(row =>
            {
                row.RelativeItem().AlignBottom().Column(column =>
                {
                    if (!string.IsNullOrWhiteSpace(_course.CourseGroupDescription))
                        column.Item().Text($"課程分類：{_course.CourseGroupDescription}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);

                    column.Item().PaddingTop(2)
                        .Text($"文件產生日期：{CourseFlyerText.FormatDate(_generatedOn)} ・ 資料以官網公告為準")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                row.AutoItem().Column(column =>
                {
                    column.Item().AlignCenter().Width(96).Image(qrPng);
                    column.Item().AlignCenter().PaddingTop(3)
                        .Text("掃描查看課程詳情").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                });
            });
    }

    private static void ComposeWatermark(IContainer container)
    {
        // Rotation pivots on the element's top-left, so: anchor at page center (Align*),
        // free the layout (Unconstrained), rotate, then pull the text back by half its
        // rendered size so its center lands on the anchor.
        container.AlignCenter().AlignMiddle().Unconstrained()
            .Rotate(-30)
            .TranslateX(-192).TranslateY(-45)
            .Text("草稿・未發佈").FontSize(64).Bold().FontColor("#28999999");
    }

    private static byte[] CreateQrPng(string url)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(pixelsPerModule: 8);
    }
}
