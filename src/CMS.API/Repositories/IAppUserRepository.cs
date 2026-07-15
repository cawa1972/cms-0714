using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IAppUserRepository
{
    Task<IEnumerable<AppUser>> GetAllAsync();
    Task<IEnumerable<AppUser>> QueryAsync(AppUserQuery query);
    Task<AppUser?> GetByIdAsync(string userId);
    Task<bool> ExistsAsync(string userId);

    /// <summary>
    /// Inserts the user (with the SysConfig default password, SHA-256 hashed) and its AppUserRole
    /// links. Returns the new IDENTITY pkid.
    /// </summary>
    Task<int> CreateAsync(AppUserRequest request);

    /// <summary>
    /// Updates a user (keyed by UserId) and re-syncs its AppUserRole links. Does not touch
    /// PasswordHash. Returns false if not found.
    /// </summary>
    Task<bool> UpdateAsync(AppUserRequest request);

    Task<bool> DeleteAsync(string userId);

    /// <summary>
    /// Resets the user's password to the SysConfig default (SHA-256 hashed) and stamps
    /// PasswordUpdatedTime. Returns false if the user does not exist.
    /// </summary>
    Task<bool> ResetPasswordAsync(string userId);
}
