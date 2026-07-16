using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class RowAuditRepository : IRowAuditRepository
{
    private readonly ISqlConnectionFactory _factory;

    public RowAuditRepository(ISqlConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<RowAuditEntry>> GetHistoryAsync(
        string tableName, string pkid, int limit = IRowAuditRepository.DefaultHistoryLimit)
    {
        using var db = _factory.Create();

        // The ROW_NUMBER wrapper bounds the read: RowAudit is append-only and never pruned, so a
        // long-lived record accumulates rows forever and an uncapped SELECT returns all of them just
        // to render a badge. Served by IX_RowAudit_Table_Pk (see database/admin.sql), whose key order
        // matches the OVER clause, so the server seeks and streams the newest `limit` rows instead of
        // sorting the record's whole trail.
        //
        // Why not TOP (@limit) / OFFSET-FETCH: neither parses under SQLite, and RowAuditRepositoryTests
        // runs this exact SQL on in-memory SQLite -- reaching for a T-SQL-only clause would buy paging
        // by silently dropping the only real coverage this query has. ROW_NUMBER is standard and runs
        // on both (SQL Server 2005+, SQLite 3.25+).
        //
        // Newest first; ties on DateTime break by the IDENTITY pkid (insertion order).
        return await db.QueryAsync<RowAuditEntry>("""
            SELECT t.[DateTime],
                   t.UserName,
                   t.ActionType,
                   t.ActionDesc
            FROM (
                SELECT ra.[DateTime],
                       ra.UserName,
                       ra.ActionType,
                       ra.ActionDesc,
                       ROW_NUMBER() OVER (ORDER BY ra.[DateTime] DESC, ra.pkid DESC) AS rn
                FROM RowAudit ra
                WHERE ra.TableName = @tableName
                  AND ra.PrimaryKeyValues = @pkid
            ) t
            WHERE t.rn <= @limit
            ORDER BY t.rn
            """, new { tableName, pkid, limit });
    }
}
