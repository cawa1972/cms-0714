using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CMS.API.Tests;

public class AppUsersControllerTests
{
    private static FakeAppUserRepository SeededRepo() => new(
        new AppUser { UserId = "helen", UserName = "Helen Chen", IsActive = true, RoleCount = 2 },
        new AppUser { UserId = "miles", UserName = "Miles Sun", IsActive = false, RoleCount = 0 });

    private static AppUsersController Controller(FakeAppUserRepository repo) => new(repo);

    // ---- List ----------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsAllUsers()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IEnumerable<AppUser>>(ok.Value);
        Assert.Equal(2, users.Count());
    }

    // ---- Filter --------------------------------------------------------

    [Fact]
    public async Task Query_ByKeyword_FiltersOnUserIdAndUserName()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new AppUserQuery { Keyword = "helen" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IEnumerable<AppUser>>(ok.Value).ToList();
        Assert.Single(users);
        Assert.Equal("helen", users[0].UserId);
    }

    [Fact]
    public async Task Query_ByIsActive_FiltersExactMatch()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new AppUserQuery { IsActive = false });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IEnumerable<AppUser>>(ok.Value).ToList();
        Assert.Single(users);
        Assert.Equal("miles", users[0].UserId);
    }

    [Fact]
    public async Task Query_EmptyQuery_ReturnsAll()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Query(new AppUserQuery());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(2, Assert.IsAssignableFrom<IEnumerable<AppUser>>(ok.Value).Count());
    }

    // ---- View ----------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingUser_ReturnsUser()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById("helen");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var user = Assert.IsType<AppUser>(ok.Value);
        Assert.Equal("Helen Chen", user.UserName);
    }

    [Fact]
    public async Task GetById_MissingUser_ReturnsNotFound()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.GetById("nope");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Add -----------------------------------------------------------

    [Fact]
    public async Task Create_NewUser_ReturnsCreatedAndPersistsRoles()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);
        var request = new AppUserRequest
        {
            UserId = "jenny",
            UserName = "Jenny Tsao",
            IsActive = true,
            RoleIds = ["Admin", "Editor"],
        };

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var user = Assert.IsType<AppUser>(created.Value);
        Assert.Equal("jenny", user.UserId);
        Assert.Equal(2, user.RoleCount);
        Assert.NotNull(await repo.GetByIdAsync("jenny"));
    }

    [Fact]
    public async Task Create_DuplicateUserId_ReturnsConflict()
    {
        var controller = Controller(SeededRepo());
        var request = new AppUserRequest { UserId = "helen", UserName = "Dup" };

        var result = await controller.Create(request);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // ---- Edit ----------------------------------------------------------

    [Fact]
    public async Task Update_ExistingUser_ReturnsUpdatedFields()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);
        var request = new AppUserRequest
        {
            UserId = "miles",
            UserName = "Miles S.",
            IsActive = true,
            RoleIds = ["Admin"],
        };

        var result = await controller.Update(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var user = Assert.IsType<AppUser>(ok.Value);
        Assert.Equal("Miles S.", user.UserName);
        Assert.True(user.IsActive);
        Assert.Equal(1, user.RoleCount);
    }

    [Fact]
    public async Task Update_MissingUser_ReturnsNotFound()
    {
        var controller = Controller(SeededRepo());
        var request = new AppUserRequest { UserId = "ghost", UserName = "Ghost" };

        var result = await controller.Update(request);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- Delete --------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingUser_ReturnsNoContent()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete("miles");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_MissingUser_ReturnsNotFound()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.Delete("ghost");

        Assert.IsType<NotFoundResult>(result);
    }

    // ---- Reset password ------------------------------------------------

    [Fact]
    public async Task ResetPassword_ExistingUser_ReturnsNoContentAndStampsTime()
    {
        var repo = SeededRepo();
        var controller = Controller(repo);

        var result = await controller.ResetPassword("helen");

        Assert.IsType<NoContentResult>(result);
        var user = await repo.GetByIdAsync("helen");
        Assert.NotNull(user!.PasswordUpdatedTime);
    }

    [Fact]
    public async Task ResetPassword_MissingUser_ReturnsNotFound()
    {
        var controller = Controller(SeededRepo());

        var result = await controller.ResetPassword("ghost");

        Assert.IsType<NotFoundResult>(result);
    }
}
