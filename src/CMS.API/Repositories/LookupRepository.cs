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
}
