using System.Data;
using CMS.API.Data;
using CMS.API.Repositories;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CMS.API.Tests;

/// <summary>
/// Runs the real <see cref="RowAuditRepository"/> SQL against an in-memory SQLite database to prove
/// the history query filters by TableName + pkid and returns rows newest first.
/// </summary>
public sealed class RowAuditRepositoryTests : IDisposable
{
    private readonly SqliteConnection _keeper;
    private readonly RowAuditRepository _repo;

    private sealed class SqliteConnectionFactory(string connectionString) : ISqlConnectionFactory
    {
        public IDbConnection Create() => new SqliteConnection(connectionString);
    }

    public RowAuditRepositoryTests()
    {
        var connectionString = $"Data Source=file:rowaudit-{Guid.NewGuid():N}?mode=memory&cache=shared";

        _keeper = new SqliteConnection(connectionString);
        _keeper.Open();
        _keeper.Execute("""
            CREATE TABLE RowAudit (
                pkid             INTEGER PRIMARY KEY AUTOINCREMENT,
                TableName        TEXT NOT NULL,
                UserName         TEXT NOT NULL,
                PrimaryKeyValues TEXT NOT NULL,
                ActionType       TEXT NOT NULL,
                ActionDesc       TEXT NULL,
                [DateTime]       TEXT NOT NULL);
            """);

        _repo = new RowAuditRepository(new SqliteConnectionFactory(connectionString));
    }

    public void Dispose() => _keeper.Dispose();

    private Task SeedAsync(string tableName, string pkid, string actionType, string userName, DateTime at) =>
        _keeper.ExecuteAsync("""
            INSERT INTO RowAudit (TableName, UserName, PrimaryKeyValues, ActionType, ActionDesc, [DateTime])
            VALUES (@tableName, @userName, @pkid, @actionType, 'desc', @at)
            """, new { tableName, userName, pkid, actionType, at });

    [Fact]
    public async Task GetHistory_FiltersByTableNameAndPkid()
    {
        await SeedAsync("Course", "123", "Insert", "alice", new DateTime(2026, 6, 1, 10, 0, 0));
        await SeedAsync("Course", "999", "Insert", "bob", new DateTime(2026, 6, 2, 10, 0, 0));   // other record
        await SeedAsync("Partner", "123", "Insert", "carol", new DateTime(2026, 6, 3, 10, 0, 0)); // other table

        var rows = (await _repo.GetHistoryAsync("Course", "123")).ToList();

        var row = Assert.Single(rows);
        Assert.Equal("alice", row.UserName);
    }

    [Fact]
    public async Task GetHistory_ReturnsRowsNewestFirst()
    {
        await SeedAsync("Course", "123", "Insert", "alice", new DateTime(2026, 6, 1, 10, 0, 0));
        await SeedAsync("Course", "123", "Update", "carol", new DateTime(2026, 6, 4, 14, 30, 0));
        await SeedAsync("Course", "123", "Update", "bob", new DateTime(2026, 6, 2, 9, 0, 0));

        var rows = (await _repo.GetHistoryAsync("Course", "123")).ToList();

        Assert.Equal(new[] { "carol", "bob", "alice" }, rows.Select(r => r.UserName));
        Assert.Equal(new DateTime(2026, 6, 4, 14, 30, 0), rows[0].DateTime);
    }

    [Fact]
    public async Task GetHistory_SameDateTime_BreaksTiesByInsertionOrderNewestFirst()
    {
        var at = new DateTime(2026, 6, 1, 10, 0, 0);
        await SeedAsync("Course", "123", "Insert", "first", at);
        await SeedAsync("Course", "123", "Update", "second", at);

        var rows = (await _repo.GetHistoryAsync("Course", "123")).ToList();

        Assert.Equal(new[] { "second", "first" }, rows.Select(r => r.UserName));
    }

    [Fact]
    public async Task GetHistory_NoRows_ReturnsEmpty()
    {
        var rows = await _repo.GetHistoryAsync("Course", "123");

        Assert.Empty(rows);
    }

    // ---- Row cap ---------------------------------------------------------

    [Fact]
    public async Task GetHistory_MoreRowsThanLimit_ReturnsOnlyTheNewest()
    {
        // RowAudit is never pruned, so the query must bound itself rather than hand back a record's
        // entire trail to render one badge.
        for (var day = 1; day <= 10; day++)
            await SeedAsync("Course", "123", "Update", $"user{day:00}", new DateTime(2026, 6, day, 10, 0, 0));

        var rows = (await _repo.GetHistoryAsync("Course", "123", limit: 3)).ToList();

        Assert.Equal(3, rows.Count);
        Assert.Equal(new[] { "user10", "user09", "user08" }, rows.Select(r => r.UserName));
    }

    [Fact]
    public async Task GetHistory_FewerRowsThanLimit_ReturnsAllOfThem()
    {
        await SeedAsync("Course", "123", "Insert", "alice", new DateTime(2026, 6, 1, 10, 0, 0));
        await SeedAsync("Course", "123", "Update", "bob", new DateTime(2026, 6, 2, 10, 0, 0));

        var rows = (await _repo.GetHistoryAsync("Course", "123", limit: 50)).ToList();

        Assert.Equal(new[] { "bob", "alice" }, rows.Select(r => r.UserName));
    }

    [Fact]
    public async Task GetHistory_LimitApplies_AfterFilteringNotBefore()
    {
        // Guards the subquery shape: the WHERE must run inside the ROW_NUMBER window, or the cap
        // would be spent on rows belonging to other records and starve the one asked for.
        for (var day = 1; day <= 5; day++)
            await SeedAsync("Partner", "999", "Update", $"other{day}", new DateTime(2026, 6, day, 12, 0, 0));

        await SeedAsync("Course", "123", "Insert", "alice", new DateTime(2026, 6, 1, 10, 0, 0));

        var rows = (await _repo.GetHistoryAsync("Course", "123", limit: 3)).ToList();

        var row = Assert.Single(rows);
        Assert.Equal("alice", row.UserName);
    }
}
