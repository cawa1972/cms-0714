namespace CMS.API.Models;

/// <summary>
/// Response model for the <c>Partner</c> table (合作廠商) — a course-provider / partner organization.
/// The primary key <see cref="Pkid"/> is a <c>smallint IDENTITY</c> assigned by the database, so it is
/// never supplied on create. There are no foreign keys and no N-N relationships.
/// </summary>
public class Partner
{
    public short Pkid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
    public string NameOnPartnerMenu { get; set; } = string.Empty;
    public string NameOnCourseDetailPage { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? ImageFilename { get; set; }
}
