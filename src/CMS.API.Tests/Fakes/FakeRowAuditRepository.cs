using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>Returns a canned history and records the filter it was asked for.</summary>
public sealed class FakeRowAuditRepository : IRowAuditRepository
{
    public List<RowAuditEntry> Entries { get; } = [];

    public string? LastTableName { get; private set; }
    public string? LastPkid { get; private set; }

    /// <summary>The limit the controller passed on the most recent call.</summary>
    public int? LastLimit { get; private set; }

    public Task<IEnumerable<RowAuditEntry>> GetHistoryAsync(
        string tableName, string pkid, int limit = IRowAuditRepository.DefaultHistoryLimit)
    {
        LastTableName = tableName;
        LastPkid = pkid;
        LastLimit = limit;

        // Honour the cap like the real TOP (@limit) does, so a test that seeds more than `limit`
        // entries sees the same truncation the SQL would produce.
        return Task.FromResult<IEnumerable<RowAuditEntry>>(Entries.Take(limit).ToList());
    }
}
