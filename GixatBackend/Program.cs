using System.Text;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Data;
using GixatBackend.Modules.Users.GraphQL;
using GixatBackend.Modules.Organizations.GraphQL;
using GixatBackend.Modules.Common.GraphQL;
using GixatBackend.Modules.Lookup.GraphQL;
using GixatBackend.Modules.Sessions.GraphQL;
using GixatBackend.Modules.JobCards.GraphQL;
using GixatBackend.Modules.Customers.GraphQL;
using GixatBackend.Modules.Common.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;

var builder = WebApplication.CreateBuilder(args);

// Load .env file
DotNetEnv.Env.Load();

// Add services to the container.
var connectionString = $"Host={Environment.GetEnvironmentVariable("DB_SERVER")};Port=5432;Database={Environment.GetEnvironmentVariable("DB_DATABASE")};Username={Environment.GetEnvironmentVariable("DB_USER")};Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantService, TenantService>();

// AWS Configuration
var awsOptions = builder.Configuration.GetAWSOptions();
awsOptions.Credentials = new BasicAWSCredentials(
    Environment.GetEnvironmentVariable("AWS_ACCESS_KEY"),
    Environment.GetEnvironmentVariable("AWS_SECRET_KEY"));
awsOptions.Region = RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION") ?? "me-central-1");
builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddScoped<IS3Service, S3Service>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyWithAtLeast32Chars!"))
    };
});

builder.Services.AddAuthorization();

builder.Services
    .AddGraphQLServer()
    .AddQueryType()
        .AddTypeExtension<AuthQueries>()
        .AddTypeExtension<OrganizationQueries>()
        .AddTypeExtension<LookupQueries>()
        .AddTypeExtension<SessionQueries>()
        .AddTypeExtension<JobCardQueries>()
        .AddTypeExtension<CustomerQueries>()
    .AddMutationType()
        .AddTypeExtension<AuthMutations>()
        .AddTypeExtension<OrganizationMutations>()
        .AddTypeExtension<MediaMutations>()
        .AddTypeExtension<LookupMutations>()
        .AddTypeExtension<SessionMutations>()
        .AddTypeExtension<JobCardMutations>()
        .AddTypeExtension<CustomerMutations>()
    .AddUploadType()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .AddAuthorization();

builder.Services.AddOpenApi();

var app = builder.Build();

// Seed Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await DbInitializer.SeedLookupDataAsync(context).ConfigureAwait(false);
        await DbInitializer.SeedExampleDataAsync(context).ConfigureAwait(false);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

await app.RunAsync().ConfigureAwait(false);
