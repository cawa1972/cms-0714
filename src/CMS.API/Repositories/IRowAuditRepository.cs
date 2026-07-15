using CMS.API.Models;

namespace CMS.API.Repositories;

/// <summary>Read side of the RowAudit change log (writes go through IRowAuditWriter).</summary>
public interface IRowAuditRepository
{
    /// <summary>
    /// All audit rows for one record — matched by table name and the record's pkid (stored as a
    /// string in PrimaryKeyValues) — newest first.
    /// </summary>
    Task<IEnumerable<RowAuditEntry>> GetHistoryAsync(string tableName, string pkid);
}
