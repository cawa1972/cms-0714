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
/// End-to-end checks that ALL of user management (<c>/api/app-users</c>) is Admin-only via the
/// controller-level role attribute: an authenticated non-Admin gets 403 on every action, an Admin
/// still passes authorization, and an unauthenticated caller still gets 401 — role enforcement on
/// the backend, not a hidden menu.
/// </summary>
public class AppUsersAdminOnlyTests
{
    private const string SigningKey = "integration-signing-secret-key-at-least-32-bytes!!";
    private const string Password = "s3cr3t-pw";
    private const string LoginUrl = "/api/Auth/login";

    private static AppUserCredential Credential(string userId, params string[] roles) => new()
    {
        UserId = userId,
        UserName = userId,
        IsActive = true,
        PasswordHash = PasswordHasher.Hash(Password),
        RoleIds = roles.ToList(),
    };

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISigningKeyProvider>();
                services.AddSingleton<ISigningKeyProvider>(new FakeSigningKeyProvider(SigningKey));

                services.RemoveAll<IAuthRepository>();
                services.AddSingleton<IAuthRepository>(new FakeAuthRepository(
                    Credential("admin", "Admin"),
                    Credential("editor", "Editor")));

                services.RemoveAll<IAppUserRepository>();
                services.AddSingleton<IAppUserRepository>(new FakeAppUserRepository(new AppUser
                {
                    UserId = "helen",
                    UserName = "Helen Chen",
                    IsActive = true,
                }));
            }));

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

    private static HttpRequestMessage Request(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        // Authorization runs before model binding, so an empty JSON body is enough for POST/PUT.
        if (method is "POST" or "PUT")
            request.Content = JsonContent.Create(new { });
        return request;
    }

    [Theory]
    [InlineData("GET", "/api/app-users")]
    [InlineData("POST", "/api/app-users/query")]
    [InlineData("GET", "/api/app-users/helen")]
    [InlineData("POST", "/api/app-users")]
    [InlineData("PUT", "/api/app-users")]
    [InlineData("DELETE", "/api/app-users/helen")]
    [InlineData("POST", "/api/app-users/helen/reset-password")]
    public async Task EveryAction_AuthenticatedNonAdmin_Gets403(string method, string url)
    {
        using var factory = CreateFactory();
        using var client = await ClientLoggedInAsAsync(factory, "editor");

        var response = await client.SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/app-users")]
    [InlineData("PUT", "/api/app-users")]
    [InlineData("DELETE", "/api/app-users/helen")]
    public async Task EveryAction_WithoutToken_Still401(string method, string url)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanStillListUsers()
    {
        using var factory = CreateFactory();
        using var client = await ClientLoggedInAsAsync(factory, "admin");

        var response = await client.GetAsync("/api/app-users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<AppUser>>();
        Assert.Contains(users!, u => u.UserId == "helen");
    }

    [Fact]
    public async Task Admin_PassesAuthorization_OnWrites()
    {
        using var factory = CreateFactory();
        using var client = await ClientLoggedInAsAsync(factory, "admin");

        // 404 (not 403): the Admin got past authorization and reached the action.
        var response = await client.DeleteAsync("/api/app-users/no-such-user");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
