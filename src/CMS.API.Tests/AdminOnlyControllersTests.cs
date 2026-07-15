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
/// End-to-end checks that role management (<c>/api/app-roles</c>) and publish-status management
/// (<c>/api/publish-statuses</c>) are Admin-only controller-wide, mirroring
/// <see cref="AppUsersAdminOnlyTests"/>: 403 for an authenticated non-Admin on every action, 401
/// without a token, and an Admin still passes authorization.
/// </summary>
public class AdminOnlyControllersTests
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

                services.RemoveAll<IAppRoleRepository>();
                services.AddSingleton<IAppRoleRepository>(new FakeAppRoleRepository(
                    new AppRole { RoleId = "Admin", RoleName = "系統管理員", PermissionLevel = 1 }));

                services.RemoveAll<IPublishStatusRepository>();
                services.AddSingleton<IPublishStatusRepository>(new FakePublishStatusRepository(
                    new PublishStatus { Pkid = 1, Description = "草稿", IsDraft = true }));
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
    [InlineData("GET", "/api/app-roles")]
    [InlineData("POST", "/api/app-roles/query")]
    [InlineData("GET", "/api/app-roles/Admin")]
    [InlineData("POST", "/api/app-roles")]
    [InlineData("PUT", "/api/app-roles")]
    [InlineData("DELETE", "/api/app-roles/Admin")]
    [InlineData("GET", "/api/publish-statuses")]
    [InlineData("POST", "/api/publish-statuses/query")]
    [InlineData("GET", "/api/publish-statuses/1")]
    [InlineData("POST", "/api/publish-statuses")]
    [InlineData("PUT", "/api/publish-statuses")]
    [InlineData("DELETE", "/api/publish-statuses/1")]
    public async Task EveryAction_AuthenticatedNonAdmin_Gets403(string method, string url)
    {
        using var factory = CreateFactory();
        using var client = await ClientLoggedInAsAsync(factory, "editor");

        var response = await client.SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/app-roles")]
    [InlineData("PUT", "/api/app-roles")]
    [InlineData("GET", "/api/publish-statuses")]
    [InlineData("DELETE", "/api/publish-statuses/1")]
    public async Task Actions_WithoutToken_Still401(string method, string url)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/app-roles")]
    [InlineData("/api/publish-statuses")]
    public async Task Admin_CanStillList(string url)
    {
        using var factory = CreateFactory();
        using var client = await ClientLoggedInAsAsync(factory, "admin");

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
