using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>Returns a canned history and records the filter it was asked for.</summary>
public sealed class FakeRowAuditRepository : IRowAuditRepository
{
    public List<RowAuditEntry> Entries { get; } = [];

    public string? LastTableName { get; private set; }
    public string? LastPkid { get; private set; }

    public Task<IEnumerable<RowAuditEntry>> GetHistoryAsync(string tableName, string pkid)
    {
        LastTableName = tableName;
        LastPkid = pkid;
        return Task.FromResult<IEnumerable<RowAuditEntry>>(Entries);
    }
}
