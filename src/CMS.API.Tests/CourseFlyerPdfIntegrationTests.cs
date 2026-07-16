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
/// End-to-end coverage of GET /api/courses/{id}/pdf through the REAL request pipeline: JWT auth,
/// routing, DI (the real <see cref="CMS.API.Pdf.CourseFlyerRenderer"/> — a forgotten registration
/// in Program.cs cannot ship green), and QuestPDF rendering. Only the DB-backed services are
/// swapped for in-memory fakes, mirroring <see cref="AuthorizationIntegrationTests"/>.
/// </summary>
public class CourseFlyerPdfIntegrationTests
{
    private const string SigningKey = "integration-signing-secret-key-at-least-32-bytes!!";
    private const string Password = "s3cr3t-pw";
    private const string LoginUrl = "/api/Auth/login";
    private const string FlyerUrl = "/api/courses/1/pdf";

    private static Course SeedCourse() => new()
    {
        Pkid = 1,
        Title = "ASP.NET Core 企業級 Web API 開發實戰",
        CourseId = "NET301",
        ProdCourseId = "PROD-NET301",
        FriendlyUrl = "net301",
        DisplayOrder = 1,
        PartnerPkid = 1,
        PublishStatusPkid = 1,
        ScheduleOn = new DateOnly(2026, 8, 1),
        ScheduleOff = new DateOnly(2026, 9, 30),
        Hour = 36,
        ListPrice = 45000m,
        LearningCredit = 36m,
        CanRepeat = false,
    };

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISigningKeyProvider>();
                services.AddSingleton<ISigningKeyProvider>(new FakeSigningKeyProvider(SigningKey));

                services.RemoveAll<IAuthRepository>();
                services.AddScoped<IAuthRepository>(_ => new FakeAuthRepository(new AppUserCredential
                {
                    UserId = "helen",
                    UserName = "Helen Chen",
                    IsActive = true,
                    PasswordHash = PasswordHasher.Hash(Password),
                    RoleIds = ["Admin"],
                }));

                services.RemoveAll<ICourseRepository>();
                services.AddScoped<ICourseRepository>(_ => new FakeCourseRepository(SeedCourse()));
            }));

    private static async Task<string> LoginAndGetTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(LoginUrl, new { userId = "helen", password = Password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    [Fact]
    public async Task GetFlyer_WithoutToken_Returns401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(FlyerUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFlyer_Authenticated_ReturnsPdfThroughRealPipeline()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(FlyerUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF"u8.ToArray(), bytes[..4]);

        // Both Content-Disposition filenames survive the real pipeline (T4).
        var disposition = response.Content.Headers.ContentDisposition;
        Assert.NotNull(disposition);
        Assert.Equal("attachment", disposition!.DispositionType);
        Assert.Equal("course-NET301.pdf", disposition.FileName);
        // HttpClient decodes filename* — asserting the decoded value proves the RFC 5987
        // round-trip (server percent-encodes, client restores the Chinese name).
        Assert.Equal("課程簡介-ASP.NET Core 企業級 Web API 開發實戰.pdf", disposition.FileNameStar);
    }

    [Fact]
    public async Task GetFlyer_Authenticated_UnknownCourse_Returns404()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/courses/999/pdf");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
