using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IAppUserRepository"/> that mirrors the real repository's contract
/// (keyword/IsActive filtering, N-N role sync, exists/not-found semantics, and password reset)
/// so controller endpoints can be exercised without a live SQL Server.
/// <para>
/// PasswordHash is never modelled here — it is backend-only and absent from every DTO. The only
/// observable trace of the password lifecycle is <see cref="AppUser.PasswordUpdatedTime"/>.
/// </para>
/// </summary>
public sealed class FakeAppUserRepository : IAppUserRepository
{
    private readonly List<AppUser> _users = [];
    private int _nextPkid = 1;

    public FakeAppUserRepository(params AppUser[] seed)
    {
        foreach (var user in seed)
        {
            user.Pkid = user.Pkid == 0 ? _nextPkid++ : user.Pkid;
            _nextPkid = Math.Max(_nextPkid, user.Pkid + 1);
            _users.Add(user);
        }
    }

    public Task<IEnumerable<AppUser>> GetAllAsync()
        => Task.FromResult(_users.OrderBy(u => u.UserId, StringComparer.Ordinal).AsEnumerable());

    public Task<IEnumerable<AppUser>> QueryAsync(AppUserQuery query)
    {
        IEnumerable<AppUser> result = _users;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(u =>
                u.UserId.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                u.UserName.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        if (query.IsActive is not null)
            result = result.Where(u => u.IsActive == query.IsActive);

        return Task.FromResult(result.OrderBy(u => u.UserId, StringComparer.Ordinal).AsEnumerable());
    }

    public Task<AppUser?> GetByIdAsync(string userId)
        => Task.FromResult(_users.FirstOrDefault(u => u.UserId == userId));

    public Task<bool> ExistsAsync(string userId)
        => Task.FromResult(_users.Any(u => u.UserId == userId));

    public Task<int> CreateAsync(AppUserRequest request)
    {
        var user = new AppUser
        {
            Pkid = _nextPkid++,
            UserId = request.UserId,
            UserName = request.UserName,
            IsActive = request.IsActive,
            PasswordUpdatedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            RoleIds = request.RoleIds.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList(),
        };
        user.RoleCount = user.RoleIds.Count;
        _users.Add(user);
        return Task.FromResult(user.Pkid);
    }

    public Task<bool> UpdateAsync(AppUserRequest request)
    {
        var user = _users.FirstOrDefault(u => u.UserId == request.UserId);
        if (user is null)
            return Task.FromResult(false);

        user.UserName = request.UserName;
        user.IsActive = request.IsActive;
        user.RoleIds = request.RoleIds.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList();
        user.RoleCount = user.RoleIds.Count;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string userId)
    {
        var removed = _users.RemoveAll(u => u.UserId == userId);
        return Task.FromResult(removed > 0);
    }

    public Task<bool> ResetPasswordAsync(string userId)
    {
        var user = _users.FirstOrDefault(u => u.UserId == userId);
        if (user is null)
            return Task.FromResult(false);

        // Simulate the server-side re-stamp of the password timestamp.
        user.PasswordUpdatedTime = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Unspecified);
        return Task.FromResult(true);
    }
}
