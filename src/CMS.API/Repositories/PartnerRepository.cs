using System.Data;
using CMS.API.Audit;
using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class PartnerRepository : IPartnerRepository
{
    private const string TableName = "Partner";

    private readonly ISqlConnectionFactory _factory;
    private readonly IRowAuditWriter _audit;

    public PartnerRepository(ISqlConnectionFactory factory, IRowAuditWriter audit)
    {
        _factory = factory;
        _audit = audit;
    }

    private const string SelectColumns = """
        SELECT p.pkid AS Pkid,
               p.Name,
               p.AppKey,
               p.NameOnPartnerMenu,
               p.NameOnCourseDetailPage,
               p.DisplayOrder,
               p.ImageFilename
        FROM Partner p
        """;

    public async Task<IEnumerable<Partner>> GetAllAsync()
    {
        using var db = _factory.Create();
        return await db.QueryAsync<Partner>($"{SelectColumns} ORDER BY p.DisplayOrder ASC");
    }

    public async Task<IEnumerable<Partner>> QueryAsync(PartnerQuery query)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add("(p.Name LIKE @kw OR p.AppKey LIKE @kw OR p.NameOnPartnerMenu LIKE @kw OR p.NameOnCourseDetailPage LIKE @kw)");
            p.Add("kw", $"%{query.Keyword.Trim()}%");
        }

        var sql = SelectColumns;
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += " ORDER BY p.DisplayOrder ASC";

        using var db = _factory.Create();
        return await db.QueryAsync<Partner>(sql, p);
    }

    public async Task<Partner?> GetByIdAsync(short pkid)
    {
        using var db = _factory.Create();
        return await db.QuerySingleOrDefaultAsync<Partner>(
            $"{SelectColumns} WHERE p.pkid = @pkid", new { pkid });
    }

    public async Task<short> CreateAsync(PartnerRequest request)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        // pkid is smallint IDENTITY: excluded from the column list, returned via SCOPE_IDENTITY().
        var pkid = await db.ExecuteScalarAsync<short>("""
            INSERT INTO Partner (Name, AppKey, NameOnPartnerMenu, NameOnCourseDetailPage, DisplayOrder, ImageFilename)
            VALUES (@Name, @AppKey, @NameOnPartnerMenu, @NameOnCourseDetailPage, @DisplayOrder, @ImageFilename);
            SELECT CAST(SCOPE_IDENTITY() AS smallint);
            """, request, tx);

        var created = await GetSnapshotAsync(db, tx, pkid);
        if (created is not null)
            await _audit.LogInsertAsync(TableName, created, db, tx);

        tx.Commit();
        return pkid;
    }

    public async Task<bool> UpdateAsync(PartnerRequest request)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        // Load "before" first so the audit row can list exactly the changed columns.
        var before = await GetSnapshotAsync(db, tx, request.Pkid);
        if (before is null)
            return false;

        var affected = await db.ExecuteAsync("""
            UPDATE Partner
               SET Name = @Name,
                   AppKey = @AppKey,
                   NameOnPartnerMenu = @NameOnPartnerMenu,
                   NameOnCourseDetailPage = @NameOnCourseDetailPage,
                   DisplayOrder = @DisplayOrder,
                   ImageFilename = @ImageFilename
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

        // Load the row first so its Name is still available for the audit description.
        var row = await GetSnapshotAsync(db, tx, pkid);
        if (row is null)
            return false;

        await db.ExecuteAsync("DELETE FROM Partner WHERE pkid = @pkid", new { pkid }, tx);
        await _audit.LogDeleteAsync(TableName, row, db, tx);

        tx.Commit();
        return true;
    }

    /// <summary>Audit snapshot, read inside the caller's transaction.</summary>
    private static Task<Partner?> GetSnapshotAsync(IDbConnection db, IDbTransaction tx, short pkid) =>
        db.QuerySingleOrDefaultAsync<Partner?>(
            $"{SelectColumns} WHERE p.pkid = @pkid", new { pkid }, tx);
}
