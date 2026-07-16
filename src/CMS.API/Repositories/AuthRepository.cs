using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class AuthRepository : IAuthRepository
{
    private readonly ISqlConnectionFactory _factory;

    public AuthRepository(ISqlConnectionFactory factory) => _factory = factory;

    public async Task<AppUserCredential?> GetCredentialAsync(string userId)
    {
        using var db = _factory.Create();

        var credential = await db.QuerySingleOrDefaultAsync<AppUserCredential>("""
            SELECT u.UserId, u.UserName, u.IsActive, u.PasswordHash
            FROM AppUser u
            WHERE u.UserId = @userId
            """, new { userId });

        if (credential is null)
            return null;

        var roleIds = await db.QueryAsync<string>(
            "SELECT RoleId FROM AppUserRole WHERE UserId = @userId ORDER BY RoleId", new { userId });
        credential.RoleIds = roleIds.ToList();
        return credential;
    }

    public async Task<bool> UpdateUserNameAsync(string userId, string userName)
    {
        using var db = _factory.Create();

        // Only UserName is written; UserId is the WHERE key, never a SET target.
        var affected = await db.ExecuteAsync(
            "UPDATE AppUser SET UserName = @userName WHERE UserId = @userId",
            new { userId, userName });

        return affected > 0;
    }

    public async Task<bool> UpdatePasswordAsync(string userId, string passwordHash)
    {
        using var db = _factory.Create();

        var affected = await db.ExecuteAsync("""
            UPDATE AppUser
               SET PasswordHash = @passwordHash,
                   PasswordUpdatedTime = GETUTCDATE()
             WHERE UserId = @userId
            """, new { userId, passwordHash });

        return affected > 0;
    }

    public async Task<bool> UpgradePasswordHashAsync(string userId, string passwordHash)
    {
        using var db = _factory.Create();

        // PasswordUpdatedTime is intentionally absent from the SET list — see IAuthRepository.
        var affected = await db.ExecuteAsync(
            "UPDATE AppUser SET PasswordHash = @passwordHash WHERE UserId = @userId",
            new { userId, passwordHash });

        return affected > 0;
    }
}
