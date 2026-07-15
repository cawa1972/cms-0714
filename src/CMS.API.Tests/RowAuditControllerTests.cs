using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CMS.API.Tests;

public class RowAuditControllerTests
{
    [Fact]
    public async Task GetHistory_ReturnsRepositoryRows_ForTrimmedTableNameAndPkid()
    {
        var repo = new FakeRowAuditRepository();
        repo.Entries.Add(new RowAuditEntry
        {
            DateTime = new DateTime(2026, 6, 4, 14, 30, 0),
            UserName = "alice",
            ActionType = "Update",
            ActionDesc = "Title",
        });
        var controller = new RowAuditController(repo);

        var result = await controller.GetHistory(" Course ", " 123 ");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IEnumerable<RowAuditEntry>>(ok.Value);
        Assert.Single(rows);
        Assert.Equal("Course", repo.LastTableName);
        Assert.Equal("123", repo.LastPkid);
    }

    [Theory]
    [InlineData(null, "123")]
    [InlineData("  ", "123")]
    [InlineData("Course", null)]
    [InlineData("Course", "  ")]
    public async Task GetHistory_MissingTableNameOrPkid_ReturnsBadRequest(string? tableName, string? pkid)
    {
        var controller = new RowAuditController(new FakeRowAuditRepository());

        var result = await controller.GetHistory(tableName, pkid);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
