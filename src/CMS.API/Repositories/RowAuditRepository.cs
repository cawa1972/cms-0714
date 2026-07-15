using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class RowAuditRepository : IRowAuditRepository
{
    private readonly ISqlConnectionFactory _factory;

    public RowAuditRepository(ISqlConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<RowAuditEntry>> GetHistoryAsync(string tableName, string pkid)
    {
        using var db = _factory.Create();
        // Newest first; ties on DateTime break by the IDENTITY pkid (insertion order).
        return await db.QueryAsync<RowAuditEntry>("""
            SELECT ra.[DateTime],
                   ra.UserName,
                   ra.ActionType,
                   ra.ActionDesc
            FROM RowAudit ra
            WHERE ra.TableName = @tableName
              AND ra.PrimaryKeyValues = @pkid
            ORDER BY ra.[DateTime] DESC, ra.pkid DESC
            """, new { tableName, pkid });
    }
}
