namespace CMS.API.Models;

/// <summary>
/// Search DTO for <c>POST /api/courses/query</c>. All fields are optional; a null field imposes
/// no constraint. <see cref="Keyword"/> matches (LIKE) the short identifying string columns.
/// </summary>
public class CourseQuery
{
    public string? Keyword { get; set; }
    public short? PartnerPkid { get; set; }
    public short? CourseGroupPkid { get; set; }
    public byte? PublishStatusPkid { get; set; }
    public DateOnly? ScheduleOnFrom { get; set; }
    public DateOnly? ScheduleOnTo { get; set; }
    public DateOnly? ScheduleOffFrom { get; set; }
    public DateOnly? ScheduleOffTo { get; set; }
    public bool? CanRepeat { get; set; }
}
