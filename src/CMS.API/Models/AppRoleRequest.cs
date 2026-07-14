using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>
/// Write DTO for creating/updating an <c>AppRole</c>.
/// <see cref="RoleId"/> is the natural key: required on create, immutable on update.
/// </summary>
public class AppRoleRequest
{
    [Required]
    [StringLength(200)]
    public string RoleId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string RoleName { get; set; } = string.Empty;

    public int PermissionLevel { get; set; } = 100;

    [StringLength(400)]
    public string? Description { get; set; }

    /// <summary>Assigned user ids (N-N AppUserRole). Delete-then-reinsert on save.</summary>
    public List<string> UserIds { get; set; } = [];
}
