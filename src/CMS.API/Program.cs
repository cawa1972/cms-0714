using System.Text;
using CMS.API.Audit;
using CMS.API.Data;
using CMS.API.Middleware;
using CMS.API.Repositories;
using CMS.API.Security;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Register Dapper type handlers (baseline for date/time-backed features).
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());

const string CorsPolicy = "LocalhostCors";

// Require an authenticated user for every endpoint by default; AuthController opts out with
// [AllowAnonymous].
builder.Services.AddControllers(options =>
{
    var requireAuth = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(requireAuth));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "CMS.API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.SetIsOriginAllowed(origin => new Uri(origin).IsLoopback)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Data access
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IAppRoleRepository, AppRoleRepository>();
builder.Services.AddScoped<IAppUserRepository, AppUserRepository>();
builder.Services.AddScoped<IPublishStatusRepository, PublishStatusRepository>();
builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
builder.Services.AddScoped<ICourseGroupRepository, CourseGroupRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IFeaturedPromoItemRepository, FeaturedPromoItemRepository>();
builder.Services.AddScoped<ILookupRepository, LookupRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

// Cross-cutting row audit: repositories log one RowAudit row per Insert/Update/Delete. The writer
// reads the acting user's UserName claim from the current request, hence IHttpContextAccessor.
// The repository is the read side (per-record history for the RowAudit badge).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRowAuditWriter, RowAuditWriter>();
builder.Services.AddScoped<IRowAuditRepository, RowAuditRepository>();

// Token issuance + the shared signing key (SysConfig 'appConfig'.symmetricSecurityKey) that both
// issues and validates JWTs.
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<ISigningKeyProvider, SysConfigSigningKeyProvider>();

// JWT bearer authentication. The signing key is resolved at validation time from ISigningKeyProvider
// so it stays in lock-step with the key AuthController signs tokens with.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<ISigningKeyProvider>((options, keyProvider) =>
    {
        // Keep the raw claim names ("role", "userId") — without this the handler rewrites "role" to
        // the legacy ClaimTypes.Role URI and [Authorize(Roles = …)] / RoleClaimType never match.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Resolved per-validation (lazily, once the DB is reachable) rather than at startup.
            IssuerSigningKeyResolver = (_, _, _, _) =>
            {
                var key = keyProvider.GetSigningKey();
                return string.IsNullOrWhiteSpace(key)
                    ? []
                    : [new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))];
            },
            NameClaimType = JwtClaims.UserId,
            RoleClaimType = JwtClaims.Role,
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Outermost middleware: any unhandled exception becomes a logged, generic 500 JSON response.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CMS.API v1");
});

app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed so the test project (WebApplicationFactory) can reference the entry point.
public partial class Program { }
