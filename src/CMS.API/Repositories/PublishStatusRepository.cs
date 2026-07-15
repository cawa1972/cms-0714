using System.Data;
using CMS.API.Audit;
using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class PublishStatusRepository : IPublishStatusRepository
{
    private const string TableName = "PublishStatus";

    private readonly ISqlConnectionFactory _factory;
    private readonly IRowAuditWriter _audit;

    public PublishStatusRepository(ISqlConnectionFactory factory, IRowAuditWriter audit)
    {
        _factory = factory;
        _audit = audit;
    }

    private const string SelectColumns = """
        SELECT ps.pkid AS Pkid,
               ps.Description,
               ps.IsDraft,
               ps.IsPublished,
               ps.IsDiscontinued
        FROM PublishStatus ps
        """;

    public async Task<IEnumerable<PublishStatus>> GetAllAsync()
    {
        using var db = _factory.Create();
        return await db.QueryAsync<PublishStatus>($"{SelectColumns} ORDER BY ps.pkid ASC");
    }

    public async Task<IEnumerable<PublishStatus>> QueryAsync(PublishStatusQuery query)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add("ps.Description LIKE @kw");
            p.Add("kw", $"%{query.Keyword.Trim()}%");
        }

        if (query.IsDraft is not null)
        {
            where.Add("ps.IsDraft = @IsDraft");
            p.Add("IsDraft", query.IsDraft);
        }

        if (query.IsPublished is not null)
        {
            where.Add("ps.IsPublished = @IsPublished");
            p.Add("IsPublished", query.IsPublished);
        }

        if (query.IsDiscontinued is not null)
        {
            where.Add("ps.IsDiscontinued = @IsDiscontinued");
            p.Add("IsDiscontinued", query.IsDiscontinued);
        }

        var sql = SelectColumns;
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += " ORDER BY ps.pkid ASC";

        using var db = _factory.Create();
        return await db.QueryAsync<PublishStatus>(sql, p);
    }

    public async Task<PublishStatus?> GetByIdAsync(byte pkid)
    {
        using var db = _factory.Create();
        return await db.QuerySingleOrDefaultAsync<PublishStatus>(
            $"{SelectColumns} WHERE ps.pkid = @pkid", new { pkid });
    }

    public async Task<bool> ExistsAsync(byte pkid)
    {
        using var db = _factory.Create();
        var count = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM PublishStatus WHERE pkid = @pkid", new { pkid });
        return count > 0;
    }

    public async Task CreateAsync(PublishStatusRequest request)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        // pkid is user-assigned (tinyint, not IDENTITY): write it explicitly, no SCOPE_IDENTITY().
        await db.ExecuteAsync("""
            INSERT INTO PublishStatus (pkid, Description, IsDraft, IsPublished, IsDiscontinued)
            VALUES (@Pkid, @Description, @IsDraft, @IsPublished, @IsDiscontinued);
            """, request, tx);

        var created = await GetSnapshotAsync(db, tx, request.Pkid);
        if (created is not null)
            await _audit.LogInsertAsync(TableName, created, db, tx);

        tx.Commit();
    }

    public async Task<bool> UpdateAsync(PublishStatusRequest request)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        // Load "before" first so the audit row can list exactly the changed columns.
        var before = await GetSnapshotAsync(db, tx, request.Pkid);
        if (before is null)
            return false;

        var affected = await db.ExecuteAsync("""
            UPDATE PublishStatus
               SET Description = @Description,
                   IsDraft = @IsDraft,
                   IsPublished = @IsPublished,
                   IsDiscontinued = @IsDiscontinued
             WHERE pkid = @Pkid;
            """, request, tx);

        if (affected == 0)
            return false;

        var after = await GetSnapshotAsync(db, tx, request.Pkid);
        await _audit.LogUpdateAsync(TableName, before, after!, db, tx);

        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(byte pkid)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        // Load the row first so its Description is still available for the audit description.
        var row = await GetSnapshotAsync(db, tx, pkid);
        if (row is null)
            return false;

        await db.ExecuteAsync("DELETE FROM PublishStatus WHERE pkid = @pkid", new { pkid }, tx);
        await _audit.LogDeleteAsync(TableName, row, db, tx);

        tx.Commit();
        return true;
    }

    /// <summary>Audit snapshot, read inside the caller's transaction.</summary>
    private static Task<PublishStatus?> GetSnapshotAsync(IDbConnection db, IDbTransaction tx, byte pkid) =>
        db.QuerySingleOrDefaultAsync<PublishStatus?>(
            $"{SelectColumns} WHERE ps.pkid = @pkid", new { pkid }, tx);
}
