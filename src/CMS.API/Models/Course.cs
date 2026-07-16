namespace CMS.API.Models;

/// <summary>
/// Response model for the <c>Course</c> table (課程) — the central training-course entity.
/// The primary key <see cref="Pkid"/> is an <c>int</c> IDENTITY (database-assigned). The three
/// <c>*_pkid</c> foreign keys are surfaced as their numeric ids plus a resolved display label
/// (<see cref="PartnerName"/>, <see cref="CourseGroupDescription"/>,
/// <see cref="PublishStatusDescription"/>) populated via JOINs in the SELECT.
/// </summary>
public class Course
{
    public int Pkid { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? OfficialTitle { get; set; }
    public string CourseId { get; set; } = string.Empty;
    public string ProdCourseId { get; set; } = string.Empty;
    public string FriendlyUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public short PartnerPkid { get; set; }
    public short? CourseGroupPkid { get; set; }
    public byte PublishStatusPkid { get; set; }
    public DateOnly ScheduleOn { get; set; }
    public DateOnly ScheduleOff { get; set; }
    public short Hour { get; set; }
    public decimal ListPrice { get; set; }
    public decimal LearningCredit { get; set; }
    public string? Material { get; set; }
    public string? Objective { get; set; }
    public string? Target { get; set; }
    public string? Prerequisites { get; set; }
    public string? Outline { get; set; }
    public string? TowardCertOrExam { get; set; }
    public string? Note { get; set; }
    public string? OtherInfo { get; set; }
    public bool CanRepeat { get; set; }

    // Resolved FK display labels (from JOINs; not persisted).
    public string? PartnerName { get; set; }
    public string? CourseGroupDescription { get; set; }
    public string? PublishStatusDescription { get; set; }

    /// <summary>Resolved from PublishStatus.IsPublished (JOIN). Drives the flyer's draft
    /// watermark — defaults to false, so an unseeded value renders the conservative
    /// 「草稿・未發佈」 overlay rather than silently looking final.</summary>
    public bool PublishStatusIsPublished { get; set; }
}
