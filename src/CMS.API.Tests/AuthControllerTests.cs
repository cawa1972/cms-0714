using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using CMS.API.Controllers;
using CMS.API.Models;
using CMS.API.Security;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CMS.API.Tests;

public class AuthControllerTests
{
    // HS256 needs a key of at least 32 bytes (256 bits); this stand-in mirrors the SysConfig secret.
    private const string SigningKey = "test-signing-secret-key-must-be-at-least-32-bytes-long!!";
    private const string Password = "s3cr3t-pw";

    /// <summary>An active user "helen" (roles Admin/Editor) whose stored hash matches <see cref="Password"/>.</summary>
    private static AppUserCredential ActiveUser() => new()
    {
        UserId = "helen",
        UserName = "Helen Chen",
        IsActive = true,
        PasswordHash = PasswordHasher.Hash(Password),
        RoleIds = ["Admin", "Editor"],
    };

    private static AuthController Controller(params AppUserCredential[] seed)
        => new(new FakeAuthRepository(seed), new JwtTokenService(), new FakeSigningKeyProvider(SigningKey));

    private static LoginResponse OkBody(ActionResult<LoginResponse> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<LoginResponse>(ok.Value);
    }

    private static void AssertUnauthorized(ActionResult<LoginResponse> result)
    {
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        // Generic message; must not disclose which check failed.
        var message = unauthorized.Value!.GetType().GetProperty("message")!.GetValue(unauthorized.Value) as string;
        Assert.Equal("invalid credentials", message);
    }

    // ---- Success -------------------------------------------------------

    [Fact]
    public async Task Login_ValidActiveUser_ReturnsProfileWithToken()
    {
        var controller = Controller(ActiveUser());

        var result = await controller.Login(new LoginRequest { UserId = "helen", Password = Password });

        var body = OkBody(result);
        Assert.Equal("helen", body.UserId);
        Assert.Equal("Helen Chen", body.UserName);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        // The token is a real, parseable JWT.
        Assert.True(new JwtSecurityTokenHandler().CanReadToken(body.AccessToken));
    }

    // ---- Failures (all 401, generic message) ---------------------------

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var controller = Controller(ActiveUser());

        var result = await controller.Login(new LoginRequest { UserId = "helen", Password = "wrong" });

        AssertUnauthorized(result);
    }

    [Fact]
    public async Task Login_UnknownUserId_ReturnsUnauthorized()
    {
        var controller = Controller(ActiveUser());

        var result = await controller.Login(new LoginRequest { UserId = "ghost", Password = Password });

        AssertUnauthorized(result);
    }

    [Fact]
    public async Task Login_InactiveUser_ReturnsUnauthorized()
    {
        var inactive = ActiveUser();
        inactive.IsActive = false;
        var controller = Controller(inactive);

        var result = await controller.Login(new LoginRequest { UserId = "helen", Password = Password });

        AssertUnauthorized(result);
    }

    // ---- Token claims / expiry -----------------------------------------

    [Fact]
    public async Task Login_IssuedToken_CarriesUserAndRoleClaims()
    {
        var controller = Controller(ActiveUser());

        var result = await controller.Login(new LoginRequest { UserId = "helen", Password = Password });

        var token = new JwtSecurityTokenHandler().ReadJwtToken(OkBody(result).AccessToken);

        Assert.Equal("helen", token.Claims.Single(c => c.Type == JwtClaims.UserId).Value);
        Assert.Equal("Helen Chen", token.Claims.Single(c => c.Type == JwtClaims.UserName).Value);

        var roles = token.Claims.Where(c => c.Type == JwtClaims.Role).Select(c => c.Value).ToList();
        Assert.Equal(2, roles.Count);
        Assert.Contains("Admin", roles);
        Assert.Contains("Editor", roles);
    }

    [Fact]
    public async Task Login_IssuedToken_ExpiresInAbout24Hours()
    {
        var controller = Controller(ActiveUser());

        var before = DateTime.UtcNow;
        var result = await controller.Login(new LoginRequest { UserId = "helen", Password = Password });
        var after = DateTime.UtcNow;

        var token = new JwtSecurityTokenHandler().ReadJwtToken(OkBody(result).AccessToken);

        // ValidTo is UTC. It should sit ~24h after issue, allowing for the wall-clock window of the call.
        Assert.InRange(
            token.ValidTo,
            before.AddHours(24).AddSeconds(-30),
            after.AddHours(24).AddSeconds(30));
    }

    // ---- PasswordHash never leaks --------------------------------------

    [Fact]
    public async Task Login_Response_NeverExposesPasswordHash()
    {
        var user = ActiveUser();
        var controller = Controller(user);

        var result = await controller.Login(new LoginRequest { UserId = "helen", Password = Password });

        var body = OkBody(result);

        // The response type has no PasswordHash member at all...
        Assert.Null(typeof(LoginResponse).GetProperty("PasswordHash",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));

        // ...and the stored hash value appears in none of the returned fields (token included).
        Assert.DoesNotContain(user.PasswordHash, body.AccessToken);
        Assert.DoesNotContain(user.PasswordHash, body.UserId);
        Assert.DoesNotContain(user.PasswordHash, body.UserName);
    }

    // ---- Legacy hash upgrade on login ----------------------------------

    /// <summary>A user whose stored hash is still the old unsalted SHA-256 hex.</summary>
    private static AppUserCredential LegacyHashUser() => new()
    {
        UserId = "helen",
        UserName = "Helen Chen",
        IsActive = true,
        PasswordHash = Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Password))),
        RoleIds = ["Admin"],
    };

    [Fact]
    public async Task Login_LegacySha256Hash_CorrectPassword_Succeeds()
    {
        // A user whose row predates the PBKDF2 migration must still be able to sign in.
        var repo = new FakeAuthRepository(LegacyHashUser());
        var controller = new AuthController(repo, new JwtTokenService(), new FakeSigningKeyProvider(SigningKey));

        var result = await controller.Login(new LoginRequest { UserId = "helen", Password = Password });

        Assert.Equal("helen", OkBody(result).UserId);
    }

    [Fact]
    public async Task Login_LegacySha256Hash_WrongPassword_StillUnauthorized_AndDoesNotUpgrade()
    {
        var repo = new FakeAuthRepository(LegacyHashUser());
        var controller = new AuthController(repo, new JwtTokenService(), new FakeSigningKeyProvider(SigningKey));

        var result = await controller.Login(new LoginRequest { UserId = "helen", Password = "wrong" });

        AssertUnauthorized(result);
        // A failed login must never rewrite the stored hash.
        Assert.Equal(0, repo.UpgradeCount);
    }

    [Fact]
    public async Task Login_LegacySha256Hash_UpgradesStoredHashToPbkdf2()
    {
        var repo = new FakeAuthRepository(LegacyHashUser());
        var controller = new AuthController(repo, new JwtTokenService(), new FakeSigningKeyProvider(SigningKey));

        await controller.Login(new LoginRequest { UserId = "helen", Password = Password });

        Assert.Equal(1, repo.UpgradeCount);

        // The row now holds a salted hash that still verifies, and is no longer flagged for upgrade.
        var stored = await repo.GetCredentialAsync("helen");
        Assert.True(PasswordHasher.Verify(Password, stored!.PasswordHash));
        Assert.False(PasswordHasher.NeedsRehash(stored.PasswordHash));
    }

    [Fact]
    public async Task Login_LegacyHashUpgrade_DoesNotStampPasswordUpdatedTime()
    {
        // The re-hash is transparent: the secret did not change, so the column that reports when the
        // user last changed their password must not move.
        var repo = new FakeAuthRepository(LegacyHashUser());
        var controller = new AuthController(repo, new JwtTokenService(), new FakeSigningKeyProvider(SigningKey));

        await controller.Login(new LoginRequest { UserId = "helen", Password = Password });

        Assert.Null(repo.PasswordUpdatedTime);
        Assert.Null(repo.PasswordUpdatedUserId);
    }

    [Fact]
    public async Task Login_AlreadyCurrentHash_DoesNotUpgrade()
    {
        // No pointless write (and no PBKDF2 re-derivation) on the overwhelmingly common path.
        var repo = new FakeAuthRepository(ActiveUser());
        var controller = new AuthController(repo, new JwtTokenService(), new FakeSigningKeyProvider(SigningKey));

        await controller.Login(new LoginRequest { UserId = "helen", Password = Password });

        Assert.Equal(0, repo.UpgradeCount);
    }
}
