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
}
