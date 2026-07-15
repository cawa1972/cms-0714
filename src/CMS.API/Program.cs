using CMS.API.Data;
using CMS.API.Repositories;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

// Register Dapper type handlers (baseline for date/time-backed features).
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());

const string CorsPolicy = "LocalhostCors";

builder.Services.AddControllers();
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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CMS.API v1");
});

app.UseCors(CorsPolicy);
app.MapControllers();

app.Run();

// Exposed so the test project (WebApplicationFactory) can reference the entry point.
public partial class Program { }
