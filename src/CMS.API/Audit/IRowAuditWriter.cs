using System.Data;

namespace CMS.API.Audit;

/// <summary>
/// Cross-cutting change log: writes one <c>RowAudit</c> row describing an Insert / Update / Delete
/// on any business table. Repositories call this after the data change succeeds, passing their own
/// connection and transaction so the audit row commits — or rolls back — atomically with the change
/// it describes. Generic — entities are inspected via reflection, so any model with a <c>Pkid</c>
/// property works.
/// </summary>
public interface IRowAuditWriter
{
    /// <summary>Logs an insert; ActionDesc is the entity's first string-property value.</summary>
    Task LogInsertAsync(string tableName, object entity, IDbConnection db, IDbTransaction? tx = null);

    /// <summary>
    /// Logs an update; ActionDesc is the comma-separated names of the properties whose values differ
    /// between <paramref name="before"/> and <paramref name="after"/>. If nothing changed, no row is
    /// written.
    /// </summary>
    Task LogUpdateAsync(string tableName, object before, object after, IDbConnection db, IDbTransaction? tx = null);

    /// <summary>Logs a delete; ActionDesc is the deleted entity's first string-property value.</summary>
    Task LogDeleteAsync(string tableName, object entity, IDbConnection db, IDbTransaction? tx = null);
}
