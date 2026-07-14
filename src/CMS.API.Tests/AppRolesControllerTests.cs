using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CMS.API.Tests;

public class AppRolesControllerTests
{
    private static FakeAppRoleRepository SeededRepo() => new(
        new AppRole { RoleId = "Admin", RoleName = "Administrator", PermissionLevel = 1, Description = "系統管理員", UserCount = 3 },
        new AppRole { RoleId = "User", RoleName = "User", PermissionLevel = 100, Description = "一般使用者", UserCount = 9 });

    private static AppRolesController Controller(FakeAppRoleRepository repo) => new(repo);

    // ---- List ----------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsAllRoles()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var roles = Assert.IsAssignableFrom<IEnumerable<AppRole>>(ok.Value);
        Assert.Equal(2, roles.Count());
    }

    // ---- Filter --------------------------------------------------------

    [Fact]
    public async Task Query_ByKeyword_FiltersOnRoleNameAndDescription()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new AppRoleQuery { Keyword = "管理" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var roles = Assert.IsAssignableFrom<IEnumerable<AppRole>>(ok.Value).ToList();
        Assert.Single(roles);
        Assert.Equal("Admin", roles[0].RoleId);
    }

    [Fact]
    public async Task Query_ByPermissionLevel_FiltersExactMatch()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new AppRoleQuery { PermissionLevel = 100 });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var roles = Assert.IsAssignableFrom<IEnumerable<AppRole>>(ok.Value).ToList();
        Assert.Single(roles);
        Assert.Equal("User", roles[0].RoleId);
    }

    [Fact]
    public async Task Query_EmptyQuery_ReturnsAll()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new AppRoleQuery());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(2, Assert.IsAssignableFrom<IEnumerable<AppRole>>(ok.Value).Count());
    }

    // ---- View ----------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingRole_ReturnsRole()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById("Admin");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var role = Assert.IsType<AppRole>(ok.Value);
        Assert.Equal("Administrator", role.RoleName);
    }

    [Fact]
    public async Task GetById_MissingRole_ReturnsNotFound()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById("Nope");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Add -----------------------------------------------------------

    [Fact]
    public async Task Create_NewRole_ReturnsCreatedAndPersists()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);
        var request = new AppRoleRequest
        {
            RoleId = "Editor",
            RoleName = "Editor",
            PermissionLevel = 50,
            Description = "編輯者",
            UserIds = ["helen", "miles@uuu.com.tw"],
        };

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var role = Assert.IsType<AppRole>(created.Value);
        Assert.Equal("Editor", role.RoleId);
        Assert.Equal(2, role.UserCount);
        Assert.NotNull(await repo.GetByIdAsync("Editor"));
    }

    [Fact]
    public async Task Create_DuplicateRoleId_ReturnsConflict()
    {
        var controller = Controller(SeededRepo());
        var request = new AppRoleRequest { RoleId = "Admin", RoleName = "Dup", Description = "x" };

        var result = await controller.Create(request);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // ---- Edit ----------------------------------------------------------

    [Fact]
    public async Task Update_ExistingRole_ReturnsUpdatedFields()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);
        var request = new AppRoleRequest
        {
            RoleId = "User",
            RoleName = "General User",
            PermissionLevel = 200,
            Description = "一般使用者(更新)",
            UserIds = ["helen"],
        };

        var result = await controller.Update(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var role = Assert.IsType<AppRole>(ok.Value);
        Assert.Equal("General User", role.RoleName);
        Assert.Equal(200, role.PermissionLevel);
        Assert.Equal(1, role.UserCount);
    }

    [Fact]
    public async Task Update_MissingRole_ReturnsNotFound()
    {
        var controller = Controller(SeededRepo());
        var request = new AppRoleRequest { RoleId = "Ghost", RoleName = "Ghost", Description = "x" };

        var result = await controller.Update(request);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Delete --------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingRole_ReturnsNoContent()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete("User");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_MissingRole_ReturnsNotFound()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete("Ghost");

        Assert.IsType<NotFoundResult>(result);
    }
}
