namespace CMS.API.Models;

/// <summary>
/// Response model for the <c>AppUser</c> table (使用者).
/// The clustered primary key is <see cref="UserId"/> (nvarchar); <see cref="Pkid"/> is an
/// IDENTITY surrogate shown as 主代碼. Roles are linked N-N via <c>AppUserRole</c>.
/// <para>
/// <c>PasswordHash</c> is intentionally absent: it is backend-only and never exposed to clients.
/// </para>
/// </summary>
public class AppUser
{
    public int Pkid { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    /// <summary>When the password was last (re)set. Read-only; stamped server-side. May be null.</summary>
    public DateTime? PasswordUpdatedTime { get; set; }

    /// <summary>Number of roles assigned to this user (COUNT over AppUserRole). List column 角色數.</summary>
    public int RoleCount { get; set; }

    /// <summary>Assigned role ids (populated on GET by id, from AppUserRole).</summary>
    public List<string> RoleIds { get; set; } = [];
}
