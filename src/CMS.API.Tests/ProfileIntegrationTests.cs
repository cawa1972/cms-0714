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
/// End-to-end checks over the real pipeline for <c>PUT /api/Auth/profile</c>: it requires a bearer
/// token, and it updates the UserName of the user named by the <em>token</em> — ignoring any UserId
/// smuggled into the request body. A shared in-memory <see cref="FakeAuthRepository"/> (registered as
/// a singleton so the test can read back what was written) stands in for SQL Server.
/// </summary>
public class ProfileIntegrationTests
{
    private const string SigningKey = "integration-signing-secret-key-at-least-32-bytes!!";
    private const string Password = "s3cr3t-pw";
    private const string ProfileUrl = "/api/Auth/profile";
    private const string LoginUrl = "/api/Auth/login";

    private static FakeAuthRepository SeedHelen() => new(new AppUserCredential
    {
        UserId = "helen",
        UserName = "Helen Chen",
        IsActive = true,
        PasswordHash = PasswordHasher.Hash(Password),
        RoleIds = ["Admin"],
    });

    private static WebApplicationFactory<Program> CreateFactory(FakeAuthRepository authRepo) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISigningKeyProvider>();
                services.AddSingleton<ISigningKeyProvider>(new FakeSigningKeyProvider(SigningKey));

                // Singleton so the assertions can inspect what the endpoint wrote.
                services.RemoveAll<IAuthRepository>();
                services.AddSingleton<IAuthRepository>(authRepo);
            }));

    private static async Task<string> LoginAsHelenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(LoginUrl, new { userId = "helen", password = Password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    [Fact]
    public async Task Profile_WithoutToken_Returns401()
    {
        var authRepo = SeedHelen();
        using var factory = CreateFactory(authRepo);
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(ProfileUrl, new { userName = "Nobody" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(authRepo.UpdatedUserId);
    }

    [Fact]
    public async Task Profile_UpdatesUserNameForTokenUser_IgnoringBodyUserId()
    {
        var authRepo = SeedHelen();
        using var factory = CreateFactory(authRepo);
        using var client = factory.CreateClient();

        var token = await LoginAsHelenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // The body carries a bogus userId; only the token's identity ("helen") must be honoured.
        var response = await client.PutAsJsonAsync(ProfileUrl, new
        {
            userId = "attacker",
            userName = "Helen Wu",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<UpdateProfileResponse>();
        Assert.Equal("helen", body!.UserId);
        Assert.Equal("Helen Wu", body.UserName);

        // The repository was asked to update "helen" (from the token), never "attacker" (from the body).
        Assert.Equal("helen", authRepo.UpdatedUserId);
        Assert.Equal("Helen Wu", authRepo.UpdatedUserName);
    }

    [Fact]
    public async Task Profile_WithWhitespaceUserName_Returns400_AndDoesNotUpdate()
    {
        var authRepo = SeedHelen();
        using var factory = CreateFactory(authRepo);
        using var client = factory.CreateClient();

        var token = await LoginAsHelenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync(ProfileUrl, new { userName = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(authRepo.UpdatedUserId);
    }
}
