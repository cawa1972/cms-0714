using System.Security.Claims;
using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Security;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CMS.API.Tests;

/// <summary>
/// Unit tests for <c>PUT /api/Auth/profile</c>: the UserName is updated for the UserId taken from the
/// JWT, an empty/whitespace UserName is rejected, and a missing identity yields 401. That the endpoint
/// ignores any UserId in the request <em>body</em> is proven end-to-end in
/// <see cref="ProfileIntegrationTests"/> (the request DTO structurally cannot bind one).
/// </summary>
public class AuthProfileControllerTests
{
    private const string SigningKey = "test-signing-secret-key-must-be-at-least-32-bytes-long!!";

    private static AppUserCredential Helen() => new()
    {
        UserId = "helen",
        UserName = "Helen Chen",
        IsActive = true,
        PasswordHash = PasswordHasher.Hash("s3cr3t-pw"),
        RoleIds = ["Admin", "Editor"],
    };

    /// <summary>Builds the controller with the given repo and an authenticated principal (or none).</summary>
    private static AuthController Controller(FakeAuthRepository repo, string? jwtUserId)
    {
        var controller = new AuthController(repo, new JwtTokenService(), new FakeSigningKeyProvider(SigningKey));

        var claims = jwtUserId is null ? new List<Claim>() : [new Claim(JwtClaims.UserId, jwtUserId)];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, jwtUserId is null ? null : "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };
        return controller;
    }

    private static UpdateProfileResponse OkBody(ActionResult<UpdateProfileResponse> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<UpdateProfileResponse>(ok.Value);
    }

    // ---- Success: updates UserName for the JWT user --------------------------

    [Fact]
    public async Task UpdateProfile_UpdatesUserNameForJwtUser()
    {
        var repo = new FakeAuthRepository(Helen());
        var controller = Controller(repo, jwtUserId: "helen");

        var result = await controller.UpdateProfile(new UpdateProfileRequest { UserName = "Helen Wu" });

        var body = OkBody(result);
        Assert.Equal("helen", body.UserId);
        Assert.Equal("Helen Wu", body.UserName);

        // The repository was told to update the JWT user, with the new name.
        Assert.Equal("helen", repo.UpdatedUserId);
        Assert.Equal("Helen Wu", repo.UpdatedUserName);
    }

    [Fact]
    public async Task UpdateProfile_TrimsUserName()
    {
        var repo = new FakeAuthRepository(Helen());
        var controller = Controller(repo, jwtUserId: "helen");

        var result = await controller.UpdateProfile(new UpdateProfileRequest { UserName = "   Helen Wu   " });

        Assert.Equal("Helen Wu", OkBody(result).UserName);
        Assert.Equal("Helen Wu", repo.UpdatedUserName);
    }

    // ---- Rejects empty / whitespace UserName --------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n ")]
    public async Task UpdateProfile_EmptyOrWhitespaceUserName_ReturnsBadRequest(string userName)
    {
        var repo = new FakeAuthRepository(Helen());
        var controller = Controller(repo, jwtUserId: "helen");

        var result = await controller.UpdateProfile(new UpdateProfileRequest { UserName = userName });

        var obj = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, obj.StatusCode);

        // No update was attempted.
        Assert.Null(repo.UpdatedUserId);
        var stored = await repo.GetCredentialAsync("helen");
        Assert.Equal("Helen Chen", stored!.UserName);
    }

    // ---- Missing identity ---------------------------------------------------

    [Fact]
    public async Task UpdateProfile_WithoutJwtUserId_ReturnsUnauthorized()
    {
        var repo = new FakeAuthRepository(Helen());
        var controller = Controller(repo, jwtUserId: null);

        var result = await controller.UpdateProfile(new UpdateProfileRequest { UserName = "Whoever" });

        Assert.IsType<UnauthorizedResult>(result.Result);
        Assert.Null(repo.UpdatedUserId);
    }

    // ---- Unknown user -------------------------------------------------------

    [Fact]
    public async Task UpdateProfile_WhenUserRowMissing_ReturnsNotFound()
    {
        // Token identifies "ghost" but no such row exists.
        var repo = new FakeAuthRepository(Helen());
        var controller = Controller(repo, jwtUserId: "ghost");

        var result = await controller.UpdateProfile(new UpdateProfileRequest { UserName = "Ghost" });

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
