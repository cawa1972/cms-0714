using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IAppRoleRepository"/> that mirrors the real repository's contract
/// (keyword/level filtering, N-N user sync, exists/not-found semantics) so controller
/// endpoints can be exercised without a live SQL Server.
/// </summary>
public sealed class FakeAppRoleRepository : IAppRoleRepository
{
    private readonly List<AppRole> _roles = [];
    private int _nextPkid = 1;

    public FakeAppRoleRepository(params AppRole[] seed)
    {
        foreach (var role in seed)
        {
            role.Pkid = role.Pkid == 0 ? _nextPkid++ : role.Pkid;
            _nextPkid = Math.Max(_nextPkid, role.Pkid + 1);
            _roles.Add(role);
        }
    }

    public Task<IEnumerable<AppRole>> GetAllAsync()
        => Task.FromResult(_roles.OrderBy(r => r.RoleId, StringComparer.Ordinal).AsEnumerable());

    public Task<IEnumerable<AppRole>> QueryAsync(AppRoleQuery query)
    {
        IEnumerable<AppRole> result = _roles;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(r =>
                r.RoleId.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                r.RoleName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                (r.Description ?? string.Empty).Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        if (query.PermissionLevel is not null)
            result = result.Where(r => r.PermissionLevel == query.PermissionLevel);

        return Task.FromResult(result.OrderBy(r => r.RoleId, StringComparer.Ordinal).AsEnumerable());
    }

    public Task<AppRole?> GetByIdAsync(string roleId)
        => Task.FromResult(_roles.FirstOrDefault(r => r.RoleId == roleId));

    public Task<bool> ExistsAsync(string roleId)
        => Task.FromResult(_roles.Any(r => r.RoleId == roleId));

    public Task<int> CreateAsync(AppRoleRequest request)
    {
        var role = new AppRole
        {
            Pkid = _nextPkid++,
            RoleId = request.RoleId,
            RoleName = request.RoleName,
            PermissionLevel = request.PermissionLevel,
            Description = request.Description,
            UserIds = request.UserIds.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList(),
        };
        role.UserCount = role.UserIds.Count;
        _roles.Add(role);
        return Task.FromResult(role.Pkid);
    }

    public Task<bool> UpdateAsync(AppRoleRequest request)
    {
        var role = _roles.FirstOrDefault(r => r.RoleId == request.RoleId);
        if (role is null)
            return Task.FromResult(false);

        role.RoleName = request.RoleName;
        role.PermissionLevel = request.PermissionLevel;
        role.Description = request.Description;
        role.UserIds = request.UserIds.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList();
        role.UserCount = role.UserIds.Count;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string roleId)
    {
        var removed = _roles.RemoveAll(r => r.RoleId == roleId);
        return Task.FromResult(removed > 0);
    }
}
