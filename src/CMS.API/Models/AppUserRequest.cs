using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>
/// Write DTO for creating/updating an <c>AppUser</c>.
/// <see cref="UserId"/> is the natural key: required on create, immutable on update.
/// <para>
/// There is deliberately no password field — <c>PasswordHash</c> is set server-side from the
/// SysConfig default on create and only rewritten via the reset-password endpoint.
/// </para>
/// </summary>
public class AppUserRequest
{
    [Required]
    [StringLength(200)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string UserName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>Assigned role ids (N-N AppUserRole). Delete-then-reinsert on save.</summary>
    public List<string> RoleIds { get; set; } = [];
}
