using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class CourseGroupRepository : ICourseGroupRepository
{
    private readonly ISqlConnectionFactory _factory;

    public CourseGroupRepository(ISqlConnectionFactory factory) => _factory = factory;

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
        // pkid is IDENTITY: the database assigns it, SCOPE_IDENTITY() returns the new value.
        return await db.ExecuteScalarAsync<short>("""
            INSERT INTO CourseGroup (Description)
            VALUES (@Description);
            SELECT CAST(SCOPE_IDENTITY() AS smallint);
            """, request);
    }

    public async Task<bool> UpdateAsync(CourseGroupRequest request)
    {
        using var db = _factory.Create();
        var affected = await db.ExecuteAsync("""
            UPDATE CourseGroup
               SET Description = @Description
             WHERE pkid = @Pkid;
            """, request);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(short pkid)
    {
        using var db = _factory.Create();
        var affected = await db.ExecuteAsync(
            "DELETE FROM CourseGroup WHERE pkid = @pkid", new { pkid });
        return affected > 0;
    }
}
