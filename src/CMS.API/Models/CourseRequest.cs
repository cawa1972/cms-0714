using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>
/// Write DTO for creating/updating a <c>Course</c>.
/// <see cref="Pkid"/> is ignored on create (the database assigns it via IDENTITY); on update it
/// identifies the row to modify. <see cref="CourseGroupPkid"/> is nullable (optional group).
/// </summary>
public class CourseRequest
{
    public int Pkid { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(300)]
    public string? OfficialTitle { get; set; }

    [Required]
    [StringLength(50)]
    public string CourseId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ProdCourseId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
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

    [StringLength(500)]
    public string? Material { get; set; }

    [StringLength(4000)]
    public string? Objective { get; set; }

    [StringLength(500)]
    public string? Target { get; set; }

    [StringLength(4000)]
    public string? Prerequisites { get; set; }

    public string? Outline { get; set; }

    public string? TowardCertOrExam { get; set; }

    [StringLength(4000)]
    public string? Note { get; set; }

    [StringLength(4000)]
    public string? OtherInfo { get; set; }

    public bool CanRepeat { get; set; }
}
