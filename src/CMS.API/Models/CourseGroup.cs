namespace CMS.API.Models;

/// <summary>
/// Response model for the <c>CourseGroup</c> table (課程分類).
/// The primary key <see cref="Pkid"/> is a <c>smallint</c> IDENTITY column assigned by the database.
/// </summary>
public class CourseGroup
{
    public short Pkid { get; set; }
    public string Description { get; set; } = string.Empty;
}
