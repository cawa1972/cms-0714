using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class PartnerRepository : IPartnerRepository
{
    private readonly ISqlConnectionFactory _factory;

    public PartnerRepository(ISqlConnectionFactory factory) => _factory = factory;

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
        // pkid is smallint IDENTITY: excluded from the column list, returned via SCOPE_IDENTITY().
        return await db.ExecuteScalarAsync<short>("""
            INSERT INTO Partner (Name, AppKey, NameOnPartnerMenu, NameOnCourseDetailPage, DisplayOrder, ImageFilename)
            VALUES (@Name, @AppKey, @NameOnPartnerMenu, @NameOnCourseDetailPage, @DisplayOrder, @ImageFilename);
            SELECT CAST(SCOPE_IDENTITY() AS smallint);
            """, request);
    }

    public async Task<bool> UpdateAsync(PartnerRequest request)
    {
        using var db = _factory.Create();
        var affected = await db.ExecuteAsync("""
            UPDATE Partner
               SET Name = @Name,
                   AppKey = @AppKey,
                   NameOnPartnerMenu = @NameOnPartnerMenu,
                   NameOnCourseDetailPage = @NameOnCourseDetailPage,
                   DisplayOrder = @DisplayOrder,
                   ImageFilename = @ImageFilename
             WHERE pkid = @Pkid;
            """, request);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(short pkid)
    {
        using var db = _factory.Create();
        var affected = await db.ExecuteAsync(
            "DELETE FROM Partner WHERE pkid = @pkid", new { pkid });
        return affected > 0;
    }
}
