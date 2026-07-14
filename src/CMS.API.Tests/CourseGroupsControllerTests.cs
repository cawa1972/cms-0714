using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CMS.API.Tests;

public class CourseGroupsControllerTests
{
    private static FakeCourseGroupRepository SeededRepo() => new(
        new CourseGroup { Pkid = 1, Description = "程式設計" },
        new CourseGroup { Pkid = 2, Description = "雲端運算" },
        new CourseGroup { Pkid = 3, Description = "資訊安全" });

    private static CourseGroupsController Controller(FakeCourseGroupRepository repo) => new(repo);

    // ---- List ----------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsAllGroups()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var groups = Assert.IsAssignableFrom<IEnumerable<CourseGroup>>(ok.Value);
        Assert.Equal(3, groups.Count());
    }

    // ---- Filter --------------------------------------------------------

    [Fact]
    public async Task Query_ByKeyword_FiltersOnDescription()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new CourseGroupQuery { Keyword = "雲端" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var groups = Assert.IsAssignableFrom<IEnumerable<CourseGroup>>(ok.Value).ToList();
        Assert.Single(groups);
        Assert.Equal((short)2, groups[0].Pkid);
    }

    [Fact]
    public async Task Query_EmptyQuery_ReturnsAll()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new CourseGroupQuery());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(3, Assert.IsAssignableFrom<IEnumerable<CourseGroup>>(ok.Value).Count());
    }

    // ---- View ----------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingGroup_ReturnsGroup()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById(2);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var group = Assert.IsType<CourseGroup>(ok.Value);
        Assert.Equal("雲端運算", group.Description);
    }

    [Fact]
    public async Task GetById_MissingGroup_ReturnsNotFound()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById(99);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Add -----------------------------------------------------------

    [Fact]
    public async Task Create_NewGroup_ReturnsCreatedWithDatabaseAssignedPkid()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);
        var request = new CourseGroupRequest { Description = "數據分析" };

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var group = Assert.IsType<CourseGroup>(created.Value);
        Assert.Equal((short)4, group.Pkid);
        Assert.Equal("數據分析", group.Description);
        Assert.NotNull(await repo.GetByIdAsync(4));
    }

    // ---- Edit ----------------------------------------------------------

    [Fact]
    public async Task Update_ExistingGroup_ReturnsUpdatedFields()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);
        var request = new CourseGroupRequest { Pkid = 3, Description = "資安與合規" };

        var result = await controller.Update(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var group = Assert.IsType<CourseGroup>(ok.Value);
        Assert.Equal("資安與合規", group.Description);
    }

    [Fact]
    public async Task Update_MissingGroup_ReturnsNotFound()
    {
        var controller = Controller(SeededRepo());
        var request = new CourseGroupRequest { Pkid = 99, Description = "Ghost" };

        var result = await controller.Update(request);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Delete --------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingGroup_ReturnsNoContent()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete(2);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_MissingGroup_ReturnsNotFound()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }
}
