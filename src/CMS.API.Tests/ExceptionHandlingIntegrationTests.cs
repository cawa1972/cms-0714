using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CMS.API.Middleware;
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
/// End-to-end checks for <see cref="ExceptionHandlingMiddleware"/> over the real pipeline: an
/// endpoint whose repository throws returns one generic 500 JSON body that leaks nothing, while the
/// already-meaningful responses (401 unauthenticated, 403 forbidden, 400 validation) are untouched.
/// </summary>
public class ExceptionHandlingIntegrationTests
{
    private const string SigningKey = "integration-signing-secret-key-at-least-32-bytes!!";
    private const string Password = "s3cr3t-pw";
    private const string ThrowingUrl = "/api/app-roles";
    private const string LoginUrl = "/api/Auth/login";

    /// <summary>Deliberately leaky exception text — none of it may ever reach the client.</summary>
    private const string SecretExceptionMessage =
        "boom: SELECT PasswordHash FROM AppUser WHERE UserId = 'helen' -- Server=db01;Password=hunter2";

    /// <summary>Every member throws, standing in for a repository hitting a broken database.</summary>
    private sealed class ThrowingAppRoleRepository : IAppRoleRepository
    {
        private static Exception Boom() => new InvalidOperationException(SecretExceptionMessage);

        public Task<IEnumerable<AppRole>> GetAllAsync() => throw Boom();
        public Task<IEnumerable<AppRole>> QueryAsync(AppRoleQuery query) => throw Boom();
        public Task<AppRole?> GetByIdAsync(string roleId) => throw Boom();
        public Task<bool> ExistsAsync(string roleId) => throw Boom();
        public Task<int> CreateAsync(AppRoleRequest request) => throw Boom();
        public Task<bool> UpdateAsync(AppRoleRequest request) => throw Boom();
        public Task<bool> DeleteAsync(string roleId) => throw Boom();
    }

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

                // An Admin and an authenticated non-Admin, for the 500 and 403 paths respectively.
                services.RemoveAll<IAuthRepository>();
                services.AddSingleton<IAuthRepository>(new FakeAuthRepository(
                    Credential("admin", "Admin"),
                    Credential("editor", "Editor")));

                // The protected endpoint's repository always throws.
                services.RemoveAll<IAppRoleRepository>();
                services.AddSingleton<IAppRoleRepository>(new ThrowingAppRoleRepository());

                // Healthy user repo so the 403 reset-password check reaches authorization, not a throw.
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

    [Fact]
    public async Task ThrowingEndpoint_Returns500_WithOnlyTheGenericMessage()
    {
        using var factory = CreateFactory();
        using var client = await ClientLoggedInAsAsync(factory, "admin");

        var response = await client.GetAsync(ThrowingUrl);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal(ExceptionHandlingMiddleware.GenericErrorMessage, body!["message"]);
    }

    [Fact]
    public async Task ThrowingEndpoint_LeaksNoExceptionDetails()
    {
        using var factory = CreateFactory();
        using var client = await ClientLoggedInAsAsync(factory, "admin");

        var response = await client.GetAsync(ThrowingUrl);
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("boom", raw);                       // exception message
        Assert.DoesNotContain("SELECT", raw);                     // SQL text
        Assert.DoesNotContain("Password", raw);                   // connection/credential details
        Assert.DoesNotContain("InvalidOperationException", raw);  // exception type
        Assert.DoesNotContain("   at ", raw);                     // stack-trace frames
    }

    [Fact]
    public async Task Unauthenticated_Still401_NotRewrittenTo500()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ThrowingUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ForbiddenAction_Still403_NotRewrittenTo500()
    {
        using var factory = CreateFactory();
        using var client = await ClientLoggedInAsAsync(factory, "editor");

        // Admin-only endpoint, called by an authenticated non-Admin.
        var response = await client.PostAsync("/api/app-users/helen/reset-password", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InvalidLoginModel_Still400ValidationProblem()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        // Missing required UserId/Password: [ApiController] model validation answers with 400.
        var response = await client.PostAsJsonAsync(LoginUrl, new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.Contains("errors", raw); // the standard validation-problem shape, not the generic 500 body
    }
}
