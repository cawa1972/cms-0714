using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>
/// Payload for <c>PUT /api/Auth/profile</c> — the signed-in user editing their own profile.
/// Deliberately carries <b>only</b> UserName: the UserId is taken from the JWT, never the body, so a
/// user can update their own display name but cannot target another account or change their roles.
/// </summary>
public class UpdateProfileRequest
{
    [Required]
    [StringLength(200)]
    public string UserName { get; set; } = string.Empty;
}
