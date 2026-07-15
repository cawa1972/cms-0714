using System.Data;
using CMS.API.Audit;
using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class AppRoleRepository : IAppRoleRepository
{
    private const string TableName = "AppRole";

    private readonly ISqlConnectionFactory _factory;
    private readonly IRowAuditWriter _audit;

    public AppRoleRepository(ISqlConnectionFactory factory, IRowAuditWriter audit)
    {
        _factory = factory;
        _audit = audit;
    }

    private const string SelectColumns = """
        SELECT r.pkid AS Pkid,
               r.RoleId,
               r.RoleName,
               r.PermissionLevel,
               r.Description,
               (SELECT COUNT(*) FROM AppUserRole ur WHERE ur.RoleId = r.RoleId) AS UserCount
        FROM AppRole r
        """;

    public async Task<IEnumerable<AppRole>> GetAllAsync()
    {
        using var db = _factory.Create();
        return await db.QueryAsync<AppRole>($"{SelectColumns} ORDER BY r.RoleId ASC");
    }

    public async Task<IEnumerable<AppRole>> QueryAsync(AppRoleQuery query)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add("(r.RoleId LIKE @kw OR r.RoleName LIKE @kw OR r.Description LIKE @kw)");
            p.Add("kw", $"%{query.Keyword.Trim()}%");
        }

        if (query.PermissionLevel is not null)
        {
            where.Add("r.PermissionLevel = @PermissionLevel");
            p.Add("PermissionLevel", query.PermissionLevel);
        }

        var sql = SelectColumns;
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += " ORDER BY r.RoleId ASC";

        using var db = _factory.Create();
        return await db.QueryAsync<AppRole>(sql, p);
    }

    public async Task<AppRole?> GetByIdAsync(string roleId)
    {
        using var db = _factory.Create();
        var role = await db.QuerySingleOrDefaultAsync<AppRole>(
            $"{SelectColumns} WHERE r.RoleId = @roleId", new { roleId });

        if (role is null)
            return null;

        var userIds = await db.QueryAsync<string>(
            "SELECT UserId FROM AppUserRole WHERE RoleId = @roleId ORDER BY UserId", new { roleId });
        role.UserIds = userIds.ToList();
        return role;
    }

    public async Task<bool> ExistsAsync(string roleId)
    {
        using var db = _factory.Create();
        var count = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM AppRole WHERE RoleId = @roleId", new { roleId });
        return count > 0;
    }

    public async Task<int> CreateAsync(AppRoleRequest request)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        var pkid = await db.ExecuteScalarAsync<int>("""
            INSERT INTO AppRole (RoleId, RoleName, PermissionLevel, Description)
            VALUES (@RoleId, @RoleName, @PermissionLevel, @Description);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """, request, tx);

        await SyncUsersAsync(db, tx, request.RoleId, request.UserIds);

        var created = await GetSnapshotAsync(db, tx, request.RoleId);
        if (created is not null)
            await _audit.LogInsertAsync(TableName, created, db, tx);

        tx.Commit();
        return pkid;
    }

    public async Task<bool> UpdateAsync(AppRoleRequest request)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        // Load "before" (including assigned users) first so the audit row can list exactly the
        // changed columns; a rewritten-but-identical UserIds list does not count as a change.
        var before = await GetSnapshotAsync(db, tx, request.RoleId);
        if (before is null)
            return false;

        var affected = await db.ExecuteAsync("""
            UPDATE AppRole
               SET RoleName = @RoleName,
                   PermissionLevel = @PermissionLevel,
                   Description = @Description
             WHERE RoleId = @RoleId;
            """, request, tx);

        if (affected == 0)
        {
            tx.Rollback();
            return false;
        }

        await SyncUsersAsync(db, tx, request.RoleId, request.UserIds);

        var after = await GetSnapshotAsync(db, tx, request.RoleId);
        await _audit.LogUpdateAsync(TableName, before, after!, db, tx);

        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(string roleId)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        // Load the row first so its RoleId/name is still available for the audit description.
        var row = await GetSnapshotAsync(db, tx, roleId);
        if (row is null)
            return false;

        await db.ExecuteAsync("DELETE FROM AppUserRole WHERE RoleId = @roleId", new { roleId }, tx);
        await db.ExecuteAsync("DELETE FROM AppRole WHERE RoleId = @roleId", new { roleId }, tx);

        await _audit.LogDeleteAsync(TableName, row, db, tx);

        tx.Commit();
        return true;
    }

    /// <summary>
    /// Audit snapshot, read inside the caller's transaction: raw AppRole columns (no derived
    /// UserCount) plus the assigned user ids, so membership changes show up as "UserIds".
    /// </summary>
    private static async Task<AppRole?> GetSnapshotAsync(IDbConnection db, IDbTransaction tx, string roleId)
    {
        var role = await db.QuerySingleOrDefaultAsync<AppRole>("""
            SELECT r.pkid AS Pkid,
                   r.RoleId,
                   r.RoleName,
                   r.PermissionLevel,
                   r.Description
            FROM AppRole r
            WHERE r.RoleId = @roleId
            """, new { roleId }, tx);

        if (role is null)
            return null;

        var userIds = await db.QueryAsync<string>(
            "SELECT UserId FROM AppUserRole WHERE RoleId = @roleId ORDER BY UserId",
            new { roleId }, tx);
        role.UserIds = userIds.ToList();
        return role;
    }

    /// <summary>Delete-then-reinsert the AppUserRole links for a role.</summary>
    private static async Task SyncUsersAsync(IDbConnection db, IDbTransaction tx, string roleId, List<string> userIds)
    {
        await db.ExecuteAsync("DELETE FROM AppUserRole WHERE RoleId = @roleId", new { roleId }, tx);

        var distinct = userIds.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList();
        if (distinct.Count == 0)
            return;

        await db.ExecuteAsync(
            "INSERT INTO AppUserRole (UserId, RoleId) VALUES (@UserId, @RoleId)",
            distinct.Select(u => new { UserId = u, RoleId = roleId }),
            tx);
    }
}
