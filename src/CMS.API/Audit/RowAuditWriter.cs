using System.Collections;
using System.Data;
using System.Reflection;
using CMS.API.Models;
using CMS.API.Security;
using Dapper;

namespace CMS.API.Audit;

/// <summary>
/// Default <see cref="IRowAuditWriter"/>: builds the audit row via reflection and inserts it with
/// Dapper on the connection/transaction the repository is already using, so a rolled-back change
/// never leaves an audit row. The acting user comes from the current request's JWT
/// (<see cref="JwtClaims.UserName"/> claim via <see cref="IHttpContextAccessor"/>); outside an
/// authenticated request it records "system". The row-building methods
/// (<see cref="CreateInsertAudit"/> etc.) are public and pure so the reflection rules can be
/// unit-tested without a database.
/// </summary>
public sealed class RowAuditWriter : IRowAuditWriter
{
    /// <summary>UserName recorded when there is no authenticated user (startup jobs, seeds, tests).</summary>
    public const string SystemUserName = "system";

    /// <summary>RowAudit.ActionDesc is varchar(1000); longer values are truncated, never rejected.</summary>
    public const int MaxActionDescLength = 1000;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public RowAuditWriter(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public Task LogInsertAsync(string tableName, object entity, IDbConnection db, IDbTransaction? tx = null) =>
        WriteAsync(CreateInsertAudit(tableName, entity), db, tx);

    public async Task LogUpdateAsync(string tableName, object before, object after, IDbConnection db, IDbTransaction? tx = null)
    {
        var row = CreateUpdateAudit(tableName, before, after);

        // Nothing changed — an audit row saying "nothing" is noise, so skip the write entirely.
        if (string.IsNullOrEmpty(row.ActionDesc))
            return;

        await WriteAsync(row, db, tx);
    }

    public Task LogDeleteAsync(string tableName, object entity, IDbConnection db, IDbTransaction? tx = null) =>
        WriteAsync(CreateDeleteAudit(tableName, entity), db, tx);

    // ---- Row building (pure; unit-tested) --------------------------------

    public RowAudit CreateInsertAudit(string tableName, object entity) =>
        CreateRow(tableName, "Insert", PkidAsString(entity), FirstStringPropertyValue(entity));

    public RowAudit CreateDeleteAudit(string tableName, object entity) =>
        CreateRow(tableName, "Delete", PkidAsString(entity), FirstStringPropertyValue(entity));

    /// <summary>
    /// Builds the update row; ActionDesc is empty (and <see cref="LogUpdateAsync"/> skips the write)
    /// when no property value differs. <paramref name="before"/> and <paramref name="after"/> must be
    /// the same type — the comparison walks one type's properties over both instances.
    /// </summary>
    public RowAudit CreateUpdateAudit(string tableName, object before, object after)
    {
        if (before.GetType() != after.GetType())
            throw new ArgumentException(
                $"before ({before.GetType().Name}) and after ({after.GetType().Name}) must be the same type.",
                nameof(after));

        return CreateRow(tableName, "Update", PkidAsString(after), ChangedPropertyNames(before, after));
    }

    private RowAudit CreateRow(string tableName, string actionType, string primaryKeyValues, string actionDesc) =>
        new()
        {
            TableName = tableName,
            UserName = ResolveUserName(),
            PrimaryKeyValues = primaryKeyValues,
            ActionType = actionType,
            ActionDesc = actionDesc.Length <= MaxActionDescLength
                ? actionDesc
                : actionDesc[..MaxActionDescLength],
            DateTime = DateTime.Now,
        };

    /// <summary>
    /// UserName from the current request's JWT. The bearer options map NameClaimType to UserId, so
    /// this reads the <see cref="JwtClaims.UserName"/> claim explicitly rather than Identity.Name.
    /// </summary>
    private string ResolveUserName()
    {
        var userName = _httpContextAccessor.HttpContext?.User.FindFirst(JwtClaims.UserName)?.Value;
        return string.IsNullOrWhiteSpace(userName) ? SystemUserName : userName;
    }

    private static string PkidAsString(object entity) =>
        entity.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.CanRead && string.Equals(p.Name, "pkid", StringComparison.OrdinalIgnoreCase))
            ?.GetValue(entity)?.ToString() ?? string.Empty;

    /// <summary>First string-typed property in declaration order — typically a Name/Title/Code field.</summary>
    private static string FirstStringPropertyValue(object entity) =>
        entity.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.CanRead && p.PropertyType == typeof(string))
            ?.GetValue(entity) as string ?? string.Empty;

    private static string ChangedPropertyNames(object before, object after)
    {
        var changed = before.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !ValuesEqual(p.GetValue(before), p.GetValue(after)))
            .Select(p => p.Name);
        return string.Join(", ", changed);
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (Equals(a, b))
            return true;

        // Collection properties (e.g. the N-N id lists on AppRole/AppUser) compare by content —
        // two separately loaded snapshots never share list instances, so Equals alone would flag
        // every collection as changed on every update.
        if (a is IEnumerable ea and not string && b is IEnumerable eb and not string)
            return ea.Cast<object?>().SequenceEqual(eb.Cast<object?>());

        return false;
    }

    // ---- Persistence ------------------------------------------------------

    private static async Task WriteAsync(RowAudit row, IDbConnection db, IDbTransaction? tx)
    {
        // pkid is int IDENTITY: excluded from the column list. DateTime is bracketed (reserved word).
        await db.ExecuteAsync("""
            INSERT INTO RowAudit (TableName, UserName, PrimaryKeyValues, ActionType, ActionDesc, [DateTime])
            VALUES (@TableName, @UserName, @PrimaryKeyValues, @ActionType, @ActionDesc, @DateTime);
            """, row, tx);
    }
}
