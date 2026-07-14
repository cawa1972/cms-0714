namespace CMS.API.Models;

/// <summary>
/// Search DTO for <c>POST /api/publish-statuses/query</c>.
/// </summary>
public class PublishStatusQuery
{
    /// <summary>LIKE match on Description.</summary>
    public string? Keyword { get; set; }

    /// <summary>Tri-state exact match on IsDraft (null = no filter).</summary>
    public bool? IsDraft { get; set; }

    /// <summary>Tri-state exact match on IsPublished (null = no filter).</summary>
    public bool? IsPublished { get; set; }

    /// <summary>Tri-state exact match on IsDiscontinued (null = no filter).</summary>
    public bool? IsDiscontinued { get; set; }
}
