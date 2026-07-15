namespace CMS.API.Models;

/// <summary>
/// Backend-only projection of an <c>AppUser</c> used solely by the login flow. Unlike
/// <see cref="AppUser"/>, this <em>does</em> carry <see cref="PasswordHash"/> so the controller can
/// verify credentials — it is never serialized to a client.
/// </summary>
public sealed class AppUserCredential
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Role ids assigned to this user (from AppUserRole), used as JWT role claims.</summary>
    public List<string> RoleIds { get; set; } = [];
}
