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
/// Unit tests for <c>POST /api/Auth/change-password</c>: a wrong current password changes nothing,
/// the complexity policy is enforced (length &gt;= 8 and at least 3 of the 4 character classes),
/// new/confirm mismatch is rejected, and a valid change stores SHA256(new) + stamps
/// PasswordUpdatedTime — always for the UserId taken from the JWT.
/// </summary>
public class AuthChangePasswordControllerTests
{
    private const string SigningKey = "test-signing-secret-key-must-be-at-least-32-bytes-long!!";
    private const string CurrentPassword = "Old-pw-123";
    /// <summary>Satisfies the policy: 10 chars, upper + lower + digit + symbol.</summary>
    private const string ValidNewPassword = "New-pw-456";

    private static AppUserCredential Helen() => new()
    {
        UserId = "helen",
        UserName = "Helen Chen",
        IsActive = true,
        PasswordHash = PasswordHasher.Hash(CurrentPassword),
        RoleIds = ["Admin"],
    };

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

    private static ChangePasswordRequest Request(
        string current = CurrentPassword, string newPw = ValidNewPassword, string? confirm = null) => new()
    {
        CurrentPassword = current,
        NewPassword = newPw,
        ConfirmNewPassword = confirm ?? newPw,
    };

    private static string BadRequestMessage(IActionResult result)
    {
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        return (string)bad.Value!.GetType().GetProperty("message")!.GetValue(bad.Value)!;
    }

    // ---- Wrong current password: rejected, nothing changes -------------------

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns400_AndChangesNothing()
    {
        var repo = new FakeAuthRepository(Helen());
        var controller = Controller(repo, "helen");

        var result = await controller.ChangePassword(Request(current: "not-the-password"));

        Assert.Contains("目前密碼錯誤", BadRequestMessage(result));

        // No write happened; the stored hash is untouched.
        Assert.Null(repo.PasswordUpdatedUserId);
        var stored = await repo.GetCredentialAsync("helen");
        Assert.Equal(PasswordHasher.Hash(CurrentPassword), stored!.PasswordHash);
    }

    // ---- Complexity policy ----------------------------------------------------

    [Theory]
    [InlineData("Ab1!")]        // < 8 chars, even with all 4 classes
    [InlineData("Abc123!")]     // 7 chars — one short
    [InlineData("abcdefgh")]    // 8 chars but only 1 class (lower)
    [InlineData("abcd1234")]    // 8 chars but only 2 classes (lower + digit)
    [InlineData("ABCD1234")]    // 8 chars but only 2 classes (upper + digit)
    [InlineData("12345678")]    // 8 chars but only 1 class (digit)
    public async Task ChangePassword_WeakNewPassword_Returns400WithPolicyMessage_AndChangesNothing(string weak)
    {
        var repo = new FakeAuthRepository(Helen());
        var controller = Controller(repo, "helen");

        var result = await controller.ChangePassword(Request(newPw: weak));

        Assert.Equal(PasswordPolicy.RequirementMessage, BadRequestMessage(result));
        Assert.Null(repo.PasswordUpdatedUserId);
    }

    [Theory]
    [InlineData("Abcdefg1")]    // upper + lower + digit
    [InlineData("abcdef1!")]    // lower + digit + symbol
    [InlineData("ABCDEF1!")]    // upper + digit + symbol
    [InlineData("Abcdefg!")]    // upper + lower + symbol
    [InlineData("New-pw-456")]  // all 4 classes
    public async Task ChangePassword_CompliantNewPassword_Succeeds(string strong)
    {
        var repo = new FakeAuthRepository(Helen());
        var controller = Controller(repo, "helen");

        var result = await controller.ChangePassword(Request(newPw: strong));

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(PasswordHasher.Hash(strong), repo.UpdatedPasswordHash);
    }

    // ---- New / confirm mismatch ------------------------------------------------

    [Fact]
    public async Task ChangePassword_NewAndConfirmMismatch_Returns400_AndChangesNothing()
    {
        var repo = new FakeAuthRepository(Helen());
        var controller = Controller(repo, "helen");

        var result = await controller.ChangePassword(
            Request(newPw: ValidNewPassword, confirm: "Different-1"));

        Assert.Contains("不一致", BadRequestMessage(result));
        Assert.Null(repo.PasswordUpdatedUserId);
    }

    // ---- Valid change ----------------------------------------------------------

    [Fact]
    public async Task ChangePassword_Valid_StoresSha256OfNew_AndStampsPasswordUpdatedTime()
    {
        var repo = new FakeAuthRepository(Helen());
        var controller = Controller(repo, "helen");

        var before = DateTime.UtcNow;
        var result = await controller.ChangePassword(Request());
        var after = DateTime.UtcNow;

        Assert.IsType<NoContentResult>(result);

        // The JWT user's row was targeted, with exactly SHA256(new password).
        Assert.Equal("helen", repo.PasswordUpdatedUserId);
        Assert.Equal(PasswordHasher.Hash(ValidNewPassword), repo.UpdatedPasswordHash);

        // The stored credential now carries the new hash, and the timestamp was stamped.
        var stored = await repo.GetCredentialAsync("helen");
        Assert.Equal(PasswordHasher.Hash(ValidNewPassword), stored!.PasswordHash);
        Assert.NotNull(repo.PasswordUpdatedTime);
        Assert.InRange(repo.PasswordUpdatedTime.Value, before, after);
    }

    [Fact]
    public async Task ChangePassword_AfterValidChange_LoginWorksWithNewPasswordOnly()
    {
        var repo = new FakeAuthRepository(Helen());
        await Controller(repo, "helen").ChangePassword(Request());

        // The same credential store now authenticates with the new password, not the old one.
        var login = Controller(repo, "helen");
        var withNew = await login.Login(new LoginRequest { UserId = "helen", Password = ValidNewPassword });
        Assert.IsType<OkObjectResult>(withNew.Result);

        var withOld = await login.Login(new LoginRequest { UserId = "helen", Password = CurrentPassword });
        Assert.IsType<UnauthorizedObjectResult>(withOld.Result);
    }

    // ---- Identity handling -----------------------------------------------------

    [Fact]
    public async Task ChangePassword_WithoutJwtUserId_ReturnsUnauthorized()
    {
        var repo = new FakeAuthRepository(Helen());
        var controller = Controller(repo, jwtUserId: null);

        var result = await controller.ChangePassword(Request());

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Null(repo.PasswordUpdatedUserId);
    }

    [Fact]
    public async Task ChangePassword_UnknownJwtUser_ReturnsNotFound()
    {
        var repo = new FakeAuthRepository(Helen());
        var controller = Controller(repo, jwtUserId: "ghost");

        var result = await controller.ChangePassword(Request());

        Assert.IsType<NotFoundResult>(result);
        Assert.Null(repo.PasswordUpdatedUserId);
    }
}
