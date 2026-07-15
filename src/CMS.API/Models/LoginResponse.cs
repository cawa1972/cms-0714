namespace CMS.API.Models;

/// <summary>
/// User profile returned on a successful login. Deliberately carries no PasswordHash — only the
/// identity fields and the signed JWT the client uses on subsequent requests.
/// </summary>
public class LoginResponse
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    /// <summary>The signed JWT access token (HS256), valid for 24 hours from issue.</summary>
    public string AccessToken { get; set; } = string.Empty;
}
