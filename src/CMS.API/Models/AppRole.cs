namespace CMS.API.Models;

/// <summary>
/// Response model for the <c>AppRole</c> table (角色).
/// The clustered primary key is <see cref="RoleId"/> (nvarchar); <see cref="Pkid"/> is an
/// IDENTITY surrogate shown as 主代碼. Users are linked N-N via <c>AppUserRole</c>.
/// </summary>
public class AppRole
{
    public int Pkid { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int PermissionLevel { get; set; }
    public string? Description { get; set; }

    /// <summary>Number of users assigned to this role (COUNT over AppUserRole). List column 使用者數.</summary>
    public int UserCount { get; set; }

    /// <summary>Assigned user ids (populated on GET by id, from AppUserRole).</summary>
    public List<string> UserIds { get; set; } = [];
}
