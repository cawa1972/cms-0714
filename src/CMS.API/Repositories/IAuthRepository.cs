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
}
