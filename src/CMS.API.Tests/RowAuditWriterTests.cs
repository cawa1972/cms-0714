using System.Data;
using System.Security.Claims;
using CMS.API.Audit;
using CMS.API.Security;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CMS.API.Tests;

/// <summary>
/// Unit tests for <see cref="RowAuditWriter"/>'s reflection logic via the pure Create* methods.
/// The connection stub throws on use, which doubles as proof that building an audit row (and
/// skipping a no-change update) never touches the database.
/// </summary>
public class RowAuditWriterTests
{
    // Deliberately starts with non-string properties so "first string property" must skip them,
    // and carries a list property so collection comparison is exercised.
    private sealed class Widget
    {
        public int Pkid { get; set; }
        public int DisplayOrder { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }
        public List<string> Tags { get; set; } = [];
    }

    private sealed class ThrowingDbConnection : IDbConnection
    {
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public string ConnectionString { get; set; } = string.Empty;
        public int ConnectionTimeout => 0;
        public string Database => string.Empty;
        public ConnectionState State => ConnectionState.Closed;
        public IDbTransaction BeginTransaction() => throw Boom();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw Boom();
        public void ChangeDatabase(string databaseName) => throw Boom();
        public void Close() { }
        public IDbCommand CreateCommand() => throw Boom();
        public void Open() => throw Boom();
        public void Dispose() { }

        private static Exception Boom() =>
            new InvalidOperationException("The database must not be touched by this test.");
    }

    private static RowAuditWriter Writer(HttpContext? context = null) =>
        new(new FakeHttpContextAccessor { HttpContext = context });

    private static HttpContext AuthenticatedContext(string userName) => new DefaultHttpContext
    {
        User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(JwtClaims.UserName, userName)], authenticationType: "Test")),
    };

    private static Widget SampleWidget() => new()
    {
        Pkid = 7,
        DisplayOrder = 3,
        Name = "資料庫課程",
        Code = "DB-101",
        IsActive = true,
        Tags = ["sql", "beginner"],
    };

    // ---- Insert / Delete: first string property --------------------------

    [Fact]
    public void CreateInsertAudit_UsesFirstStringPropertyAsActionDesc()
    {
        var row = Writer().CreateInsertAudit("Widget", SampleWidget());

        Assert.Equal("Widget", row.TableName);
        Assert.Equal("Insert", row.ActionType);
        Assert.Equal("資料庫課程", row.ActionDesc); // Name, not the ints declared before it
    }

    [Fact]
    public void CreateDeleteAudit_UsesFirstStringPropertyAsActionDesc()
    {
        var row = Writer().CreateDeleteAudit("Widget", SampleWidget());

        Assert.Equal("Delete", row.ActionType);
        Assert.Equal("資料庫課程", row.ActionDesc);
    }

    [Fact]
    public void CreateInsertAudit_NoStringProperty_WritesEmptyActionDesc()
    {
        var row = Writer().CreateInsertAudit("Bare", new { Pkid = 1, Count = 2 });

        Assert.Equal(string.Empty, row.ActionDesc);
    }

    // ---- PrimaryKeyValues -------------------------------------------------

    [Fact]
    public void CreateInsertAudit_ReadsPkidAsPrimaryKeyValues()
    {
        var row = Writer().CreateInsertAudit("Widget", SampleWidget());

        Assert.Equal("7", row.PrimaryKeyValues);
    }

    // ---- Update: changed property names ------------------------------------

    [Fact]
    public void CreateUpdateAudit_ListsExactlyTheChangedPropertyNames()
    {
        var before = SampleWidget();
        var after = SampleWidget();
        after.Name = "雲端課程";
        after.IsActive = false;

        var row = Writer().CreateUpdateAudit("Widget", before, after);

        Assert.Equal("Update", row.ActionType);
        Assert.Equal("Name, IsActive", row.ActionDesc); // declaration order, changed props only
        Assert.Equal("7", row.PrimaryKeyValues);
    }

    [Fact]
    public void CreateUpdateAudit_NothingChanged_WritesEmptyActionDesc()
    {
        // The two instances hold equal-content but distinct Tags lists: content equality must win.
        var row = Writer().CreateUpdateAudit("Widget", SampleWidget(), SampleWidget());

        Assert.Equal(string.Empty, row.ActionDesc);
    }

    [Fact]
    public void CreateUpdateAudit_ListContentChanged_FlagsTheListProperty()
    {
        var before = SampleWidget();
        var after = SampleWidget();
        after.Tags = ["sql", "advanced"];

        var row = Writer().CreateUpdateAudit("Widget", before, after);

        Assert.Equal("Tags", row.ActionDesc);
    }

    [Fact]
    public async Task LogUpdateAsync_NothingChanged_SkipsTheDatabaseWrite()
    {
        // The throwing connection would fail the test if the writer attempted an insert.
        await Writer().LogUpdateAsync("Widget", SampleWidget(), SampleWidget(), new ThrowingDbConnection());
    }

    // ---- UserName from the JWT ---------------------------------------------

    [Fact]
    public void CreateInsertAudit_NoHttpContext_FallsBackToSystem()
    {
        var row = Writer(context: null).CreateInsertAudit("Widget", SampleWidget());

        Assert.Equal("system", row.UserName);
    }

    [Fact]
    public void CreateInsertAudit_UnauthenticatedRequest_FallsBackToSystem()
    {
        var row = Writer(new DefaultHttpContext()).CreateInsertAudit("Widget", SampleWidget());

        Assert.Equal("system", row.UserName);
    }

    [Fact]
    public void CreateInsertAudit_AuthenticatedRequest_UsesUserNameClaim()
    {
        var row = Writer(AuthenticatedContext("王小明")).CreateInsertAudit("Widget", SampleWidget());

        Assert.Equal("王小明", row.UserName);
    }

    // ---- ActionDesc length --------------------------------------------------

    [Fact]
    public void CreateInsertAudit_TruncatesActionDescAt1000Characters()
    {
        var widget = SampleWidget();
        widget.Name = new string('x', 1500);

        var row = Writer().CreateInsertAudit("Widget", widget);

        Assert.Equal(1000, row.ActionDesc!.Length);
        Assert.Equal(new string('x', 1000), row.ActionDesc);
    }
}
