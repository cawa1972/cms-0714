using System.Data;
using CMS.API.Audit;
using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class CourseGroupRepository : ICourseGroupRepository
{
    private const string TableName = "CourseGroup";

    private readonly ISqlConnectionFactory _factory;
    private readonly IRowAuditWriter _audit;

    public CourseGroupRepository(ISqlConnectionFactory factory, IRowAuditWriter audit)
    {
        _factory = factory;
        _audit = audit;
    }

    private const string SelectColumns = """
        SELECT cg.pkid AS Pkid,
               cg.Description
        FROM CourseGroup cg
        """;

    public async Task<IEnumerable<CourseGroup>> GetAllAsync()
    {
        using var db = _factory.Create();
        return await db.QueryAsync<CourseGroup>($"{SelectColumns} ORDER BY cg.pkid ASC");
    }

    public async Task<IEnumerable<CourseGroup>> QueryAsync(CourseGroupQuery query)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add("cg.Description LIKE @kw");
            p.Add("kw", $"%{query.Keyword.Trim()}%");
        }

        var sql = SelectColumns;
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += " ORDER BY cg.pkid ASC";

        using var db = _factory.Create();
        return await db.QueryAsync<CourseGroup>(sql, p);
    }

    public async Task<CourseGroup?> GetByIdAsync(short pkid)
    {
        using var db = _factory.Create();
        return await db.QuerySingleOrDefaultAsync<CourseGroup>(
            $"{SelectColumns} WHERE cg.pkid = @pkid", new { pkid });
    }

    public async Task<short> CreateAsync(CourseGroupRequest request)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        // pkid is IDENTITY: the database assigns it, SCOPE_IDENTITY() returns the new value.
        var pkid = await db.ExecuteScalarAsync<short>("""
            INSERT INTO CourseGroup (Description)
            VALUES (@Description);
            SELECT CAST(SCOPE_IDENTITY() AS smallint);
            """, request, tx);

        var created = await GetSnapshotAsync(db, tx, pkid);
        if (created is not null)
            await _audit.LogInsertAsync(TableName, created, db, tx);

        tx.Commit();
        return pkid;
    }

    public async Task<bool> UpdateAsync(CourseGroupRequest request)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        // Load "before" first so the audit row can list exactly the changed columns.
        var before = await GetSnapshotAsync(db, tx, request.Pkid);
        if (before is null)
            return false;

        var affected = await db.ExecuteAsync("""
            UPDATE CourseGroup
               SET Description = @Description
             WHERE pkid = @Pkid;
            """, request, tx);

        if (affected == 0)
            return false;

        var after = await GetSnapshotAsync(db, tx, request.Pkid);
        await _audit.LogUpdateAsync(TableName, before, after!, db, tx);

        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(short pkid)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        // Load the row first so its Description is still available for the audit description.
        var row = await GetSnapshotAsync(db, tx, pkid);
        if (row is null)
            return false;

        await db.ExecuteAsync("DELETE FROM CourseGroup WHERE pkid = @pkid", new { pkid }, tx);
        await _audit.LogDeleteAsync(TableName, row, db, tx);

        tx.Commit();
        return true;
    }

    /// <summary>Audit snapshot, read inside the caller's transaction.</summary>
    private static Task<CourseGroup?> GetSnapshotAsync(IDbConnection db, IDbTransaction tx, short pkid) =>
        db.QuerySingleOrDefaultAsync<CourseGroup?>(
            $"{SelectColumns} WHERE cg.pkid = @pkid", new { pkid }, tx);
}
