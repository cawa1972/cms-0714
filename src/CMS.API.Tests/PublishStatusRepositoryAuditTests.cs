using System.Data;
using CMS.API.Audit;
using CMS.API.Data;
using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Tests.Fakes;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CMS.API.Tests;

/// <summary>
/// Proves the RowAudit wiring of one retrofitted repository (<see cref="PublishStatusRepository"/>)
/// end-to-end against a real database — an in-memory SQLite instance running the repository's
/// actual SQL (PublishStatus is the one entity whose CUD statements are portable: user-assigned
/// pkid, so no SCOPE_IDENTITY()). Each test gets its own private database.
/// </summary>
public sealed class PublishStatusRepositoryAuditTests : IDisposable
{
    private readonly SqliteConnection _keeper;
    private readonly PublishStatusRepository _repo;

    private sealed class SqliteConnectionFactory(string connectionString) : ISqlConnectionFactory
    {
        public IDbConnection Create() => new SqliteConnection(connectionString);
    }

    public PublishStatusRepositoryAuditTests()
    {
        // Shared-cache named in-memory DB: lives as long as _keeper stays open, visible to every
        // connection the repository opens through the factory.
        var connectionString = $"Data Source=file:audit-{Guid.NewGuid():N}?mode=memory&cache=shared";

        _keeper = new SqliteConnection(connectionString);
        _keeper.Open();
        _keeper.Execute("""
            CREATE TABLE PublishStatus (
                pkid           INTEGER PRIMARY KEY,
                Description    TEXT    NOT NULL,
                IsDraft        INTEGER NOT NULL,
                IsPublished    INTEGER NOT NULL,
                IsDiscontinued INTEGER NOT NULL);

            CREATE TABLE RowAudit (
                pkid             INTEGER PRIMARY KEY AUTOINCREMENT,
                TableName        TEXT NOT NULL,
                UserName         TEXT NOT NULL,
                PrimaryKeyValues TEXT NOT NULL,
                ActionType       TEXT NOT NULL,
                ActionDesc       TEXT NULL,
                [DateTime]       TEXT NOT NULL);
            """);

        _repo = new PublishStatusRepository(
            new SqliteConnectionFactory(connectionString),
            new RowAuditWriter(new FakeHttpContextAccessor()));
    }

    public void Dispose() => _keeper.Dispose();

    private Task SeedAsync(byte pkid, string description, bool isDraft = false, bool isPublished = false) =>
        _keeper.ExecuteAsync("""
            INSERT INTO PublishStatus (pkid, Description, IsDraft, IsPublished, IsDiscontinued)
            VALUES (@pkid, @description, @isDraft, @isPublished, 0)
            """, new { pkid, description, isDraft, isPublished });

    private async Task<List<RowAudit>> AuditRowsAsync() =>
        (await _keeper.QueryAsync<RowAudit>("""
            SELECT TableName, UserName, PrimaryKeyValues, ActionType, ActionDesc
            FROM RowAudit ORDER BY pkid
            """)).ToList();

    // ---- Insert -----------------------------------------------------------

    [Fact]
    public async Task Create_WritesInsertAuditRow_WithFirstStringColumn()
    {
        await _repo.CreateAsync(new PublishStatusRequest { Pkid = 5, Description = "草稿", IsDraft = true });

        var row = Assert.Single(await AuditRowsAsync());
        Assert.Equal("PublishStatus", row.TableName);
        Assert.Equal("Insert", row.ActionType);
        Assert.Equal("5", row.PrimaryKeyValues);
        Assert.Equal("草稿", row.ActionDesc);
        Assert.Equal("system", row.UserName); // no authenticated request in unit tests
    }

    [Fact]
    public async Task Create_Failed_LeavesNoAuditRow()
    {
        await SeedAsync(5, "草稿");

        // Duplicate primary key: the INSERT throws and the transaction rolls back.
        await Assert.ThrowsAsync<SqliteException>(() =>
            _repo.CreateAsync(new PublishStatusRequest { Pkid = 5, Description = "重複" }));

        Assert.Empty(await AuditRowsAsync());
    }

    // ---- Update -----------------------------------------------------------

    [Fact]
    public async Task Update_WritesUpdateAuditRow_ListingExactlyTheChangedColumns()
    {
        await SeedAsync(5, "草稿", isDraft: true);

        var ok = await _repo.UpdateAsync(new PublishStatusRequest
        {
            Pkid = 5,
            Description = "已發布",
            IsDraft = false,
            IsPublished = true,
        });

        Assert.True(ok);
        var row = Assert.Single(await AuditRowsAsync());
        Assert.Equal("Update", row.ActionType);
        Assert.Equal("5", row.PrimaryKeyValues);
        Assert.Equal("Description, IsDraft, IsPublished", row.ActionDesc); // IsDiscontinued unchanged
    }

    [Fact]
    public async Task Update_NothingChanged_WritesNoAuditRow()
    {
        await SeedAsync(5, "草稿", isDraft: true);

        var ok = await _repo.UpdateAsync(new PublishStatusRequest
        {
            Pkid = 5,
            Description = "草稿",
            IsDraft = true,
        });

        Assert.True(ok);
        Assert.Empty(await AuditRowsAsync());
    }

    [Fact]
    public async Task Update_MissingRow_ReturnsFalse_AndWritesNoAuditRow()
    {
        var ok = await _repo.UpdateAsync(new PublishStatusRequest { Pkid = 99, Description = "無" });

        Assert.False(ok);
        Assert.Empty(await AuditRowsAsync());
    }

    // ---- Delete -----------------------------------------------------------

    [Fact]
    public async Task Delete_WritesDeleteAuditRow_WithFirstStringColumn()
    {
        await SeedAsync(5, "草稿");

        var ok = await _repo.DeleteAsync(5);

        Assert.True(ok);
        var row = Assert.Single(await AuditRowsAsync());
        Assert.Equal("Delete", row.ActionType);
        Assert.Equal("5", row.PrimaryKeyValues);
        Assert.Equal("草稿", row.ActionDesc);
    }

    [Fact]
    public async Task Delete_MissingRow_ReturnsFalse_AndWritesNoAuditRow()
    {
        var ok = await _repo.DeleteAsync(99);

        Assert.False(ok);
        Assert.Empty(await AuditRowsAsync());
    }
}
