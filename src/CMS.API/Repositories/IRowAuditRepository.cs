using CMS.API.Models;

namespace CMS.API.Repositories;

/// <summary>Read side of the RowAudit change log (writes go through IRowAuditWriter).</summary>
public interface IRowAuditRepository
{
    /// <summary>
    /// Default cap on <see cref="GetHistoryAsync"/>. RowAudit is append-only and never pruned, so an
    /// uncapped read grows without bound for the life of the record. The badge renders one entry
    /// inline and the rest in a scrollable dialog, so the newest 200 is far more trail than the UI
    /// can use.
    /// </summary>
    const int DefaultHistoryLimit = 200;

    /// <summary>
    /// The most recent audit rows for one record — matched by table name and the record's pkid
    /// (stored as a string in PrimaryKeyValues) — newest first, capped at
    /// <paramref name="limit"/> rows.
    /// </summary>
    Task<IEnumerable<RowAuditEntry>> GetHistoryAsync(
        string tableName, string pkid, int limit = DefaultHistoryLimit);
}
