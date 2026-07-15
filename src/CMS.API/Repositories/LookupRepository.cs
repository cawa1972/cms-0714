using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class LookupRepository : ILookupRepository
{
    private readonly ISqlConnectionFactory _factory;

    public LookupRepository(ISqlConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<LookupItem>> GetAppUsersAsync()
    {
        using var db = _factory.Create();
        return await db.QueryAsync<LookupItem>("""
            SELECT UserId AS Value,
                   UserName + ' (' + UserId + ')' AS Label
            FROM AppUser
            ORDER BY UserName ASC
            """);
    }

    public async Task<IEnumerable<LookupItem>> GetAppRolesAsync()
    {
        using var db = _factory.Create();
        return await db.QueryAsync<LookupItem>("""
            SELECT RoleId AS Value,
                   RoleName + ' (' + RoleId + ')' AS Label
            FROM AppRole
            ORDER BY RoleName ASC
            """);
    }

    public async Task<IEnumerable<LookupItem>> GetPublishStatusesAsync()
    {
        using var db = _factory.Create();
        return await db.QueryAsync<LookupItem>("""
            SELECT CAST(pkid AS varchar(3)) AS Value,
                   Description AS Label
            FROM PublishStatus
            ORDER BY pkid ASC
            """);
    }

    public async Task<IEnumerable<LookupItem>> GetPartnersAsync()
    {
        using var db = _factory.Create();
        return await db.QueryAsync<LookupItem>("""
            SELECT CAST(pkid AS varchar(6)) AS Value,
                   Name AS Label
            FROM Partner
            ORDER BY DisplayOrder ASC
            """);
    }

    public async Task<IEnumerable<LookupItem>> GetCourseGroupsAsync()
    {
        using var db = _factory.Create();
        return await db.QueryAsync<LookupItem>("""
            SELECT CAST(pkid AS varchar(6)) AS Value,
                   Description AS Label
            FROM CourseGroup
            ORDER BY pkid ASC
            """);
    }
}
