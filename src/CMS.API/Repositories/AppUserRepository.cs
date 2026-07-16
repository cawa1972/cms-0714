using System.Data;
using System.Text.Json;
using CMS.API.Audit;
using CMS.API.Data;
using CMS.API.Models;
using CMS.API.Security;
using Dapper;

namespace CMS.API.Repositories;

public sealed class AppUserRepository : IAppUserRepository
{
    private const string TableName = "AppUser";

    private readonly ISqlConnectionFactory _factory;
    private readonly IRowAuditWriter _audit;

    public AppUserRepository(ISqlConnectionFactory factory, IRowAuditWriter audit)
    {
        _factory = factory;
        _audit = audit;
    }

    // PasswordHash is intentionally never selected — it is backend-only.
    private const string SelectColumns = """
        SELECT u.pkid AS Pkid,
               u.UserId,
               u.UserName,
               u.IsActive,
               u.PasswordUpdatedTime,
               (SELECT COUNT(*) FROM AppUserRole ur WHERE ur.UserId = u.UserId) AS RoleCount
        FROM AppUser u
        """;

    public async Task<IEnumerable<AppUser>> GetAllAsync()
    {
        using var db = _factory.Create();
        return await db.QueryAsync<AppUser>($"{SelectColumns} ORDER BY u.UserId ASC");
    }

    public async Task<IEnumerable<AppUser>> QueryAsync(AppUserQuery query)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add("(u.UserId LIKE @kw OR u.UserName LIKE @kw)");
            p.Add("kw", $"%{query.Keyword.Trim()}%");
        }

        if (query.IsActive is not null)
        {
            where.Add("u.IsActive = @IsActive");
            p.Add("IsActive", query.IsActive);
        }

        var sql = SelectColumns;
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += " ORDER BY u.UserId ASC";

        using var db = _factory.Create();
        return await db.QueryAsync<AppUser>(sql, p);
    }

    public async Task<AppUser?> GetByIdAsync(string userId)
    {
        using var db = _factory.Create();
        var user = await db.QuerySingleOrDefaultAsync<AppUser>(
            $"{SelectColumns} WHERE u.UserId = @userId", new { userId });

        if (user is null)
            return null;

        var roleIds = await db.QueryAsync<string>(
            "SELECT RoleId FROM AppUserRole WHERE UserId = @userId ORDER BY RoleId", new { userId });
        user.RoleIds = roleIds.ToList();
        return user;
    }

    public async Task<bool> ExistsAsync(string userId)
    {
        using var db = _factory.Create();
        var count = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM AppUser WHERE UserId = @userId", new { userId });
        return count > 0;
    }

    public async Task<int> CreateAsync(AppUserRequest request)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        var passwordHash = await GetDefaultPasswordHashAsync(db, tx);

        var pkid = await db.ExecuteScalarAsync<int>("""
            INSERT INTO AppUser (UserId, UserName, IsActive, PasswordHash, PasswordUpdatedTime)
            VALUES (@UserId, @UserName, @IsActive, @PasswordHash, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """,
            new { request.UserId, request.UserName, request.IsActive, PasswordHash = passwordHash },
            tx);

        await SyncRolesAsync(db, tx, request.UserId, request.RoleIds);

        var created = await GetSnapshotAsync(db, tx, request.UserId);
        if (created is not null)
            await _audit.LogInsertAsync(TableName, created, db, tx);

        tx.Commit();
        return pkid;
    }

    public async Task<bool> UpdateAsync(AppUserRequest request)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        // Load "before" (including assigned roles) first so the audit row can list exactly the
        // changed columns; a rewritten-but-identical RoleIds list does not count as a change.
        var before = await GetSnapshotAsync(db, tx, request.UserId);
        if (before is null)
            return false;

        // PasswordHash / PasswordUpdatedTime are deliberately untouched here.
        var affected = await db.ExecuteAsync("""
            UPDATE AppUser
               SET UserName = @UserName,
                   IsActive = @IsActive
             WHERE UserId = @UserId;
            """, request, tx);

        if (affected == 0)
        {
            tx.Rollback();
            return false;
        }

        await SyncRolesAsync(db, tx, request.UserId, request.RoleIds);

        var after = await GetSnapshotAsync(db, tx, request.UserId);
        await _audit.LogUpdateAsync(TableName, before, after!, db, tx);

        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(string userId)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        // Load the row first so its UserId/name is still available for the audit description.
        var row = await GetSnapshotAsync(db, tx, userId);
        if (row is null)
            return false;

        await db.ExecuteAsync("DELETE FROM AppUserRole WHERE UserId = @userId", new { userId }, tx);
        await db.ExecuteAsync("DELETE FROM AppUser WHERE UserId = @userId", new { userId }, tx);

        await _audit.LogDeleteAsync(TableName, row, db, tx);

        tx.Commit();
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string userId)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        var before = await GetSnapshotAsync(db, tx, userId);
        if (before is null)
            return false;

        var passwordHash = await GetDefaultPasswordHashAsync(db, tx);

        var affected = await db.ExecuteAsync("""
            UPDATE AppUser
               SET PasswordHash = @PasswordHash,
                   PasswordUpdatedTime = GETUTCDATE()
             WHERE UserId = @UserId;
            """, new { UserId = userId, PasswordHash = passwordHash }, tx);

        if (affected == 0)
            return false;

        // PasswordHash is backend-only and absent from the model, so the audit row surfaces the
        // reset as a "PasswordUpdatedTime" change — the hash value itself is never logged.
        var after = await GetSnapshotAsync(db, tx, userId);
        await _audit.LogUpdateAsync(TableName, before, after!, db, tx);

        tx.Commit();
        return true;
    }

    /// <summary>
    /// Audit snapshot, read inside the caller's transaction: raw AppUser columns (no derived
    /// RoleCount, no PasswordHash) plus the assigned role ids, so membership changes show up as
    /// "RoleIds".
    /// </summary>
    private static async Task<AppUser?> GetSnapshotAsync(IDbConnection db, IDbTransaction tx, string userId)
    {
        var user = await db.QuerySingleOrDefaultAsync<AppUser>("""
            SELECT u.pkid AS Pkid,
                   u.UserId,
                   u.UserName,
                   u.IsActive,
                   u.PasswordUpdatedTime
            FROM AppUser u
            WHERE u.UserId = @userId
            """, new { userId }, tx);

        if (user is null)
            return null;

        var roleIds = await db.QueryAsync<string>(
            "SELECT RoleId FROM AppUserRole WHERE UserId = @userId ORDER BY RoleId",
            new { userId }, tx);
        user.RoleIds = roleIds.ToList();
        return user;
    }

    /// <summary>Delete-then-reinsert the AppUserRole links for a user.</summary>
    private static async Task SyncRolesAsync(IDbConnection db, IDbTransaction tx, string userId, List<string> roleIds)
    {
        await db.ExecuteAsync("DELETE FROM AppUserRole WHERE UserId = @userId", new { userId }, tx);

        var distinct = roleIds.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList();
        if (distinct.Count == 0)
            return;

        await db.ExecuteAsync(
            "INSERT INTO AppUserRole (UserId, RoleId) VALUES (@UserId, @RoleId)",
            distinct.Select(r => new { UserId = userId, RoleId = r }),
            tx);
    }

    /// <summary>
    /// Reads the default password from <c>SysConfig</c> (configKey = 'appConfig', a JSON blob whose
    /// <c>defaultPassword</c> property holds the value) and returns its SHA-256 hash (lowercase hex).
    /// The value is read at runtime on every reset/create — never cached, never hard-coded.
    /// </summary>
    private static async Task<string> GetDefaultPasswordHashAsync(IDbConnection db, IDbTransaction tx)
    {
        var configValue = await db.ExecuteScalarAsync<string?>(
            "SELECT configValue FROM SysConfig WHERE configKey = 'appConfig'", transaction: tx);

        return ResolveDefaultPasswordHash(configValue);
    }

    /// <summary>
    /// Pure part of the default-password resolution, split from the DB read so it is unit-testable:
    /// parses the SysConfig 'appConfig' JSON, extracts <c>defaultPassword</c>, and returns a salted
    /// <see cref="PasswordHasher"/> hash of it. Throws on missing config or property.
    /// <para>
    /// The returned hash is <b>not</b> stable across calls (the salt is random), so assert on it with
    /// <see cref="PasswordHasher.Verify"/> rather than string equality.
    /// </para>
    /// </summary>
    public static string ResolveDefaultPasswordHash(string? configValue)
    {
        if (string.IsNullOrWhiteSpace(configValue))
            throw new InvalidOperationException("SysConfig 'appConfig' is missing; cannot resolve the default password.");

        using var doc = JsonDocument.Parse(configValue);
        if (!doc.RootElement.TryGetProperty("defaultPassword", out var prop) ||
            prop.GetString() is not { Length: > 0 } defaultPassword)
        {
            throw new InvalidOperationException("SysConfig 'appConfig' has no 'defaultPassword' property.");
        }

        return PasswordHasher.Hash(defaultPassword);
    }
}
