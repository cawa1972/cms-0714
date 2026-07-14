namespace CMS.API.Models;

/// <summary>
/// Search DTO for <c>POST /api/app-roles/query</c>.
/// </summary>
public class AppRoleQuery
{
    /// <summary>LIKE match on RoleId, RoleName, Description.</summary>
    public string? Keyword { get; set; }

    /// <summary>Exact match on PermissionLevel.</summary>
    public int? PermissionLevel { get; set; }
}
