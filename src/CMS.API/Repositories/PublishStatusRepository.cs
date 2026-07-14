using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class PublishStatusRepository : IPublishStatusRepository
{
    private readonly ISqlConnectionFactory _factory;

    public PublishStatusRepository(ISqlConnectionFactory factory) => _factory = factory;

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
        // pkid is user-assigned (tinyint, not IDENTITY): write it explicitly, no SCOPE_IDENTITY().
        await db.ExecuteAsync("""
            INSERT INTO PublishStatus (pkid, Description, IsDraft, IsPublished, IsDiscontinued)
            VALUES (@Pkid, @Description, @IsDraft, @IsPublished, @IsDiscontinued);
            """, request);
    }

    public async Task<bool> UpdateAsync(PublishStatusRequest request)
    {
        using var db = _factory.Create();
        var affected = await db.ExecuteAsync("""
            UPDATE PublishStatus
               SET Description = @Description,
                   IsDraft = @IsDraft,
                   IsPublished = @IsPublished,
                   IsDiscontinued = @IsDiscontinued
             WHERE pkid = @Pkid;
            """, request);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(byte pkid)
    {
        using var db = _factory.Create();
        var affected = await db.ExecuteAsync(
            "DELETE FROM PublishStatus WHERE pkid = @pkid", new { pkid });
        return affected > 0;
    }
}
