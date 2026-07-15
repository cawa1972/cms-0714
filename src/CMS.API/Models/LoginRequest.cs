using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>
/// Credentials posted to <c>POST /api/Auth/login</c>. Both fields are required; the password is
/// sent in plain text (over TLS) and compared against <c>AppUser.PasswordHash</c> after hashing.
/// </summary>
public class LoginRequest
{
    [Required]
    [StringLength(200)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
