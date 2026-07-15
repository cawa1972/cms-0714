using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>
/// Payload for <c>POST /api/Auth/change-password</c> — the signed-in user changing their own
/// password. The target UserId comes from the JWT, never the body. Passwords travel in plain text
/// (over TLS) and only their SHA-256 hashes are ever stored; no hash ever crosses the API boundary.
/// </summary>
public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
