using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IAuthRepository
{
    /// <summary>
    /// Loads the credential projection (including PasswordHash and role ids) for the given UserId,
    /// or null if no such user exists. The IsActive check is left to the caller so the failure reason
    /// is never leaked.
    /// </summary>
    Task<AppUserCredential?> GetCredentialAsync(string userId);

    /// <summary>
    /// Updates <b>only</b> the <c>UserName</c> for the given UserId (self-service profile edit).
    /// Touches no other column — roles, IsActive, and PasswordHash are never affected. Returns false
    /// if no such user exists.
    /// </summary>
    Task<bool> UpdateUserNameAsync(string userId, string userName);

    /// <summary>
    /// Stores a new <c>PasswordHash</c> for the given UserId and stamps <c>PasswordUpdatedTime</c>
    /// with the current time (self-service password change). Verification of the current password and
    /// the complexity policy are the caller's job. Returns false if no such user exists.
    /// </summary>
    Task<bool> UpdatePasswordAsync(string userId, string passwordHash);

    /// <summary>
    /// Re-writes <b>only</b> the <c>PasswordHash</c> for the given UserId, leaving
    /// <c>PasswordUpdatedTime</c> untouched. This is the re-hash path for a password whose stored hash
    /// used weaker parameters: the secret is unchanged, so stamping the column would misreport when
    /// the user last changed their password. Returns false if no such user exists.
    /// </summary>
    Task<bool> UpgradePasswordHashAsync(string userId, string passwordHash);
}
