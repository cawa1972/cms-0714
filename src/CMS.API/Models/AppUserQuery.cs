namespace CMS.API.Models;

/// <summary>
/// Search DTO for <c>POST /api/app-users/query</c>.
/// </summary>
public class AppUserQuery
{
    /// <summary>LIKE match on UserId, UserName.</summary>
    public string? Keyword { get; set; }

    /// <summary>Tri-state exact match on IsActive (null = no filter).</summary>
    public bool? IsActive { get; set; }
}
