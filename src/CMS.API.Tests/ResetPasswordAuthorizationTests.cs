using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Security;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end checks for the Admin-only reset-password endpoint
/// (<c>POST /api/app-users/{id}/reset-password</c>) over the real pipeline: the Admin role is
/// enforced by claim on the backend (403 for an authenticated non-Admin — not merely a hidden
/// button), an Admin reset stamps PasswordUpdatedTime, and the response never carries a password
/// or hash. Also unit-tests the SysConfig default-password resolution:
/// <see cref="AppUserRepository.ResolveDefaultPasswordHash"/> = SHA256(defaultPassword) read from
/// the 'appConfig' JSON at runtime.
/// </summary>
public class ResetPasswordAuthorizationTests
{
    private const string SigningKey = "integration-signing-secret-key-at-least-32-bytes!!";
    private const string Password = "s3cr3t-pw";
    private const string ResetUrl = "/api/app-users/helen/reset-password";
    private const string LoginUrl = "/api/Auth/login";

    private static readonly DateTime SeededTime = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    private static AppUserCredential Credential(string userId, params string[] roles) => new()
    {
        UserId = userId,
        UserName = userId,
        IsActive = true,
        PasswordHash = PasswordHasher.Hash(Password),
        RoleIds = roles.ToList(),
    };

    private static WebApplicationFactory<Program> CreateFactory(FakeAppUserRepository appUsers) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISigningKeyProvider>();
                services.AddSingleton<ISigningKeyProvider>(new FakeSigningKeyProvider(SigningKey));

                // Two login identities: an Admin and an authenticated non-Admin.
                services.RemoveAll<IAuthRepository>();
                services.AddSingleton<IAuthRepository>(new FakeAuthRepository(
                    Credential("admin", "Admin"),
                    Credential("editor", "Editor")));

                // Singleton so the test can read back the reset target's state afterwards.
                services.RemoveAll<IAppUserRepository>();
                services.AddSingleton<IAppUserRepository>(appUsers);
            }));

    private static FakeAppUserRepository SeedHelen() => new(new AppUser
    {
        UserId = "helen",
        UserName = "Helen Chen",
        IsActive = true,
        PasswordUpdatedTime = SeededTime,
        RoleIds = [],
    });

    private static async Task<HttpClient> ClientLoggedInAsAsync(
        WebApplicationFactory<Program> factory, string userId)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(LoginUrl, new { userId, password = Password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return client;
    }

    // ---- Role enforcement ------------------------------------------------------

    [Fact]
    public async Task ResetPassword_WithoutToken_Returns401()
    {
        var appUsers = SeedHelen();
        using var factory = CreateFactory(appUsers);
        using var client = factory.CreateClient();

        var response = await client.PostAsync(ResetUrl, null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(SeededTime, (await appUsers.GetByIdAsync("helen"))!.PasswordUpdatedTime);
    }

    [Fact]
    public async Task ResetPassword_AuthenticatedNonAdmin_Returns403_AndChangesNothing()
    {
        var appUsers = SeedHelen();
        using var factory = CreateFactory(appUsers);
        using var client = await ClientLoggedInAsAsync(factory, "editor");

        var response = await client.PostAsync(ResetUrl, null);

        // Enforced by role claim on the backend — not just a hidden button.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(SeededTime, (await appUsers.GetByIdAsync("helen"))!.PasswordUpdatedTime);
    }

    [Fact]
    public async Task ResetPassword_Admin_Returns204_StampsPasswordUpdatedTime_AndReturnsNoSecrets()
    {
        var appUsers = SeedHelen();
        using var factory = CreateFactory(appUsers);
        using var client = await ClientLoggedInAsAsync(factory, "admin");

        var response = await client.PostAsync(ResetUrl, null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // No password or hash (or anything else) ever comes back to the client.
        Assert.Empty(await response.Content.ReadAsStringAsync());

        // The reset re-stamped the password timestamp.
        var helen = await appUsers.GetByIdAsync("helen");
        Assert.NotEqual(SeededTime, helen!.PasswordUpdatedTime);
    }

    [Fact]
    public async Task ResetPassword_Admin_UnknownUser_Returns404()
    {
        using var factory = CreateFactory(SeedHelen());
        using var client = await ClientLoggedInAsAsync(factory, "admin");

        var response = await client.PostAsync("/api/app-users/ghost/reset-password", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- SysConfig default-password resolution (the hash the reset stores) ------

    [Fact]
    public void ResolveDefaultPasswordHash_ReturnsSha256OfDefaultPasswordFromConfigJson()
    {
        const string configValue = """{ "defaultPassword": "CMS4fun#", "symmetricSecurityKey": "irrelevant" }""";

        var hash = AppUserRepository.ResolveDefaultPasswordHash(configValue);

        // Exactly SHA256(defaultPassword) — read from the JSON at runtime, not hard-coded.
        Assert.Equal(PasswordHasher.Hash("CMS4fun#"), hash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveDefaultPasswordHash_MissingConfig_Throws(string? configValue)
    {
        Assert.Throws<InvalidOperationException>(
            () => AppUserRepository.ResolveDefaultPasswordHash(configValue));
    }

    [Theory]
    [InlineData("""{ "symmetricSecurityKey": "no-default-password-here" }""")]
    [InlineData("""{ "defaultPassword": "" }""")]
    public void ResolveDefaultPasswordHash_MissingOrEmptyProperty_Throws(string configValue)
    {
        Assert.Throws<InvalidOperationException>(
            () => AppUserRepository.ResolveDefaultPasswordHash(configValue));
    }
}
