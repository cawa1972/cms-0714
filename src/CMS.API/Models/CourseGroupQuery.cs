namespace CMS.API.Models;

/// <summary>
/// Search DTO for <c>POST /api/course-groups/query</c>.
/// </summary>
public class CourseGroupQuery
{
    /// <summary>LIKE match on Description.</summary>
    public string? Keyword { get; set; }
}
