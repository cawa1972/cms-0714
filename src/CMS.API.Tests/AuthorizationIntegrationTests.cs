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
/// End-to-end checks over the real request pipeline (JWT bearer auth + global authorization filter).
/// The DB-backed services are swapped for in-memory fakes so no live SQL Server is needed; the fake
/// signing key is shared by both token issuance and validation, exactly like SysConfig in production.
/// </summary>
public class AuthorizationIntegrationTests
{
    private const string SigningKey = "integration-signing-secret-key-at-least-32-bytes!!";
    private const string Password = "s3cr3t-pw";

    /// <summary>A protected endpoint (requires auth via the global filter — AppRolesController has no [AllowAnonymous]).</summary>
    private const string ProtectedUrl = "/api/app-roles";
    private const string LoginUrl = "/api/Auth/login";

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                // Shared signing key (stands in for SysConfig 'appConfig'.symmetricSecurityKey).
                services.RemoveAll<ISigningKeyProvider>();
                services.AddSingleton<ISigningKeyProvider>(new FakeSigningKeyProvider(SigningKey));

                // An active Admin user so /api/Auth/login can mint a real token.
                services.RemoveAll<IAuthRepository>();
                services.AddScoped<IAuthRepository>(_ => new FakeAuthRepository(new AppUserCredential
                {
                    UserId = "helen",
                    UserName = "Helen Chen",
                    IsActive = true,
                    PasswordHash = PasswordHasher.Hash(Password),
                    RoleIds = ["Admin"],
                }));

                // Back the protected endpoint with an in-memory repository (no DB).
                services.RemoveAll<IAppRoleRepository>();
                services.AddScoped<IAppRoleRepository>(_ => new FakeAppRoleRepository(
                    new AppRole { RoleId = "Admin", RoleName = "系統管理員", PermissionLevel = 1 }));
            }));

    private static async Task<string> LoginAndGetTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(LoginUrl, new { userId = "helen", password = Password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ProtectedUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidBearerToken_Returns200()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(ProtectedUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithGarbageToken_Returns401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-jwt");

        var response = await client.GetAsync(ProtectedUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthLogin_IsAnonymous_ReachableWithoutToken()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        // No Authorization header at all: the login endpoint must still be reachable (not 401).
        var response = await client.PostAsJsonAsync(LoginUrl, new { userId = "helen", password = Password });

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
