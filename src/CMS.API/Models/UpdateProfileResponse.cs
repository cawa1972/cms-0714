namespace CMS.API.Models;

/// <summary>
/// Returned by <c>PUT /api/Auth/profile</c> after a successful self-service update. Echoes the
/// authenticated identity and the newly stored (trimmed) UserName so the client can refresh its
/// session copy without re-reading the token.
/// </summary>
public class UpdateProfileResponse
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
