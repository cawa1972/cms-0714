using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IAppRoleRepository
{
    Task<IEnumerable<AppRole>> GetAllAsync();
    Task<IEnumerable<AppRole>> QueryAsync(AppRoleQuery query);
    Task<AppRole?> GetByIdAsync(string roleId);
    Task<bool> ExistsAsync(string roleId);

    /// <summary>Inserts the role and its AppUserRole links. Returns the new IDENTITY pkid.</summary>
    Task<int> CreateAsync(AppRoleRequest request);

    /// <summary>Updates a role (keyed by RoleId) and re-syncs its AppUserRole links. Returns false if not found.</summary>
    Task<bool> UpdateAsync(AppRoleRequest request);

    Task<bool> DeleteAsync(string roleId);
}
