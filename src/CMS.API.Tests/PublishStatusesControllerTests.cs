using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CMS.API.Tests;

public class PublishStatusesControllerTests
{
    private static FakePublishStatusRepository SeededRepo() => new(
        new PublishStatus { Pkid = 1, Description = "草稿", IsDraft = true, IsPublished = false, IsDiscontinued = false },
        new PublishStatus { Pkid = 2, Description = "已發布", IsDraft = false, IsPublished = true, IsDiscontinued = false },
        new PublishStatus { Pkid = 3, Description = "已停用", IsDraft = false, IsPublished = false, IsDiscontinued = true });

    private static PublishStatusesController Controller(FakePublishStatusRepository repo) => new(repo);

    // ---- List ----------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsAllStatuses()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var statuses = Assert.IsAssignableFrom<IEnumerable<PublishStatus>>(ok.Value);
        Assert.Equal(3, statuses.Count());
    }

    // ---- Filter --------------------------------------------------------

    [Fact]
    public async Task Query_ByKeyword_FiltersOnDescription()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new PublishStatusQuery { Keyword = "發布" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var statuses = Assert.IsAssignableFrom<IEnumerable<PublishStatus>>(ok.Value).ToList();
        Assert.Single(statuses);
        Assert.Equal((byte)2, statuses[0].Pkid);
    }

    [Fact]
    public async Task Query_ByIsPublished_FiltersExactMatch()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new PublishStatusQuery { IsPublished = true });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var statuses = Assert.IsAssignableFrom<IEnumerable<PublishStatus>>(ok.Value).ToList();
        Assert.Single(statuses);
        Assert.Equal("已發布", statuses[0].Description);
    }

    [Fact]
    public async Task Query_ByIsDraftFalse_ExcludesDrafts()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new PublishStatusQuery { IsDraft = false });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var statuses = Assert.IsAssignableFrom<IEnumerable<PublishStatus>>(ok.Value).ToList();
        Assert.Equal(2, statuses.Count);
        Assert.DoesNotContain(statuses, s => s.IsDraft);
    }

    [Fact]
    public async Task Query_EmptyQuery_ReturnsAll()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new PublishStatusQuery());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(3, Assert.IsAssignableFrom<IEnumerable<PublishStatus>>(ok.Value).Count());
    }

    // ---- View ----------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingStatus_ReturnsStatus()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById(2);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<PublishStatus>(ok.Value);
        Assert.Equal("已發布", status.Description);
    }

    [Fact]
    public async Task GetById_MissingStatus_ReturnsNotFound()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById(99);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Add -----------------------------------------------------------

    [Fact]
    public async Task Create_NewStatus_ReturnsCreatedAndPersists()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);
        var request = new PublishStatusRequest
        {
            Pkid = 4,
            Description = "審核中",
            IsDraft = true,
            IsPublished = false,
            IsDiscontinued = false,
        };

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var status = Assert.IsType<PublishStatus>(created.Value);
        Assert.Equal((byte)4, status.Pkid);
        Assert.NotNull(await repo.GetByIdAsync(4));
    }

    [Fact]
    public async Task Create_DuplicatePkid_ReturnsConflict()
    {
        var controller = Controller(SeededRepo());
        var request = new PublishStatusRequest { Pkid = 1, Description = "重複" };

        var result = await controller.Create(request);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // ---- Edit ----------------------------------------------------------

    [Fact]
    public async Task Update_ExistingStatus_ReturnsUpdatedFields()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);
        var request = new PublishStatusRequest
        {
            Pkid = 3,
            Description = "已封存",
            IsDraft = false,
            IsPublished = false,
            IsDiscontinued = true,
        };

        var result = await controller.Update(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<PublishStatus>(ok.Value);
        Assert.Equal("已封存", status.Description);
        Assert.True(status.IsDiscontinued);
    }

    [Fact]
    public async Task Update_MissingStatus_ReturnsNotFound()
    {
        var controller = Controller(SeededRepo());
        var request = new PublishStatusRequest { Pkid = 99, Description = "Ghost" };

        var result = await controller.Update(request);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Delete --------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingStatus_ReturnsNoContent()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete(2);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_MissingStatus_ReturnsNotFound()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }
}
