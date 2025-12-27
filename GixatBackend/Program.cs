using System.Text;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Services;
using GixatBackend.Data;
using GixatBackend.Modules.Users.GraphQL;
using GixatBackend.Modules.Common.Constants;
using GixatBackend.Modules.Common.Middleware;
using GixatBackend.Modules.Organizations.GraphQL;
using GixatBackend.Modules.Common.GraphQL;
using GixatBackend.Modules.Common.Lookup.GraphQL;
using GixatBackend.Modules.Sessions.GraphQL;
using GixatBackend.Modules.JobCards.GraphQL;
using GixatBackend.Modules.Customers.GraphQL;
using GixatBackend.Modules.Invites.GraphQL;
using GixatBackend.Modules.Common.Services;
using GixatBackend.Modules.Common.Services.AWS;
using GixatBackend.Modules.Common.Services.Tenant;
using GixatBackend.Modules.Common.Services.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using System.Security.Cryptography;
using StackExchange.Redis;

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GixatBackend.Tests")]

var builder = WebApplication.CreateBuilder(args);

// Load .env file only if it exists and doesn't contain multi-line values
var envPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    try
    {
        DotNetEnv.Env.Load();
    }
    catch (Exception ex) when (ex is FormatException || ex is ArgumentException)
    {
        // Skip .env if it has parsing issues (e.g., multi-line values)
    }
}

// Add services to the container.
var connectionString = $"Host={Environment.GetEnvironmentVariable("DB_SERVER")};Port=5432;Database={Environment.GetEnvironmentVariable("DB_DATABASE")};Username={Environment.GetEnvironmentVariable("DB_USER")};Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};";

// Add DbContext with standard scoped registration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        npgsqlOptions.CommandTimeout(DatabaseConstants.CommandTimeoutSeconds);
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: DatabaseConstants.MaxRetryCount,
            maxRetryDelay: TimeSpan.FromSeconds(DatabaseConstants.MaxRetryDelaySeconds),
            errorCodesToAdd: null);
    }));

// Add DbContext Factory for DataLoaders - uses separate options instance
builder.Services.AddSingleton<IDbContextFactory<ApplicationDbContext>>(sp =>
{
    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
    optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        npgsqlOptions.CommandTimeout(DatabaseConstants.CommandTimeoutSeconds);
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: DatabaseConstants.MaxRetryCount,
            maxRetryDelay: TimeSpan.FromSeconds(DatabaseConstants.MaxRetryDelaySeconds),
            errorCodesToAdd: null);
    });
    
    return new PooledApplicationDbContextFactory(optionsBuilder.Options);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<GixatBackend.Modules.Customers.Services.CustomerExportService>();
builder.Services.AddScoped<GixatBackend.Modules.Customers.Services.CustomerActivityDataLoader>();
builder.Services.AddScoped<IVirusScanService, ClamAvScanService>();
builder.Services.AddScoped<IImageCompressionService, ImageCompressionService>();
builder.Services.AddScoped<GixatBackend.Modules.Users.Services.UserProfileService>();

// Phase 3: Business logic services
builder.Services.AddScoped<GixatBackend.Modules.JobCards.Services.IJobCardService, GixatBackend.Modules.JobCards.Services.JobCardService>();
builder.Services.AddScoped<GixatBackend.Modules.Sessions.Services.ISessionService, GixatBackend.Modules.Sessions.Services.SessionService>();
builder.Services.AddScoped<GixatBackend.Modules.Appointments.Services.IAppointmentService, GixatBackend.Modules.Appointments.Services.AppointmentService>();

// AWS Configuration
var awsOptions = builder.Configuration.GetAWSOptions();
awsOptions.Credentials = new BasicAWSCredentials(
    Environment.GetEnvironmentVariable("AWS_ACCESS_KEY"),
    Environment.GetEnvironmentVariable("AWS_SECRET_KEY"));
awsOptions.Region = RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION") ?? "me-central-1");
builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddScoped<IS3Service, S3Service>();

// Redis Configuration (optional for local development)
var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? "localhost:6379";
try
{
    // Parse connection string and ensure proper configuration
    var configOptions = ConfigurationOptions.Parse(redisConnectionString);
    
    // Add default options if not specified
    if (!redisConnectionString.Contains("abortConnect", StringComparison.OrdinalIgnoreCase))
    {
        configOptions.AbortOnConnectFail = false;
    }
    
    // Set connection timeout
    configOptions.ConnectTimeout = 5000; // 5 seconds
    configOptions.SyncTimeout = 5000;
    
    // Allow admin commands if needed
    configOptions.AllowAdmin = false;
    
    var redis = await ConnectionMultiplexer.ConnectAsync(configOptions).ConfigureAwait(false);
    
    // Test connection
    var db = redis.GetDatabase();
    await db.PingAsync().ConfigureAwait(false);
    
    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
    builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
    
    using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
    var logger = loggerFactory.CreateLogger("Startup");
    logger.LogInformation("Redis connected successfully to {Endpoint}", configOptions.EndPoints.FirstOrDefault());
}
catch (Exception ex) when (ex is StackExchange.Redis.RedisConnectionException || ex is TimeoutException)
{
    // Redis not available - skip cache service
    using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
    var logger = loggerFactory.CreateLogger("Startup");
    Log.LogRedisWarning(logger, ex);
}

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
    var jwtKey = builder.Configuration["Jwt:Key"] 
        ?? throw new InvalidOperationException("Jwt:Key is not configured.");
    
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = key
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // First, try to get token from Authorization header (default behavior)
            // If not present, fall back to cookie
            if (string.IsNullOrEmpty(context.Token))
            {
                var token = context.Request.Cookies["access_token"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
            }
            return Task.CompletedTask;
        }
    };
})
.AddGoogle(options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") 
        ?? throw new InvalidOperationException("GOOGLE_CLIENT_ID is not configured.");
    options.ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") 
        ?? throw new InvalidOperationException("GOOGLE_CLIENT_SECRET is not configured.");
    
    // Request profile and email scopes
    options.Scope.Add("profile");
    options.Scope.Add("email");
    
    // Save tokens for future use
    options.SaveTokens = true;
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:3002", "https://gixat.com", "https://www.gixat.com")
              .SetIsOriginAllowed(origin => 
              {
                  // Allow mobile apps (Flutter) which may send null or empty origin
                  if (string.IsNullOrEmpty(origin))
                      return true;
                  
                  var host = new Uri(origin).Host;
                  return host == "localhost" || host == "127.0.0.1" || 
                         host.StartsWith("192.168.", StringComparison.Ordinal) || 
                         host.StartsWith("10.", StringComparison.Ordinal) || 
                         host.StartsWith("172.", StringComparison.Ordinal) ||
                         host == "gixat.com" || host == "www.gixat.com" || host.EndsWith(".gixat.com", StringComparison.Ordinal);
              })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services
    .AddGraphQLServer()
    .AddQueryType()
        .AddType<AuthQueries>()
        .AddType<UserExtensions>()
        .AddTypeExtension<UserProfileQueries>()
        .AddType<OrganizationQueries>()
        .AddType<LookupQueries>()
        .AddTypeExtension<SessionQueries>()
        .AddType<JobCardQueries>()
        .AddTypeExtension<JobCardCommentQueries>()
        .AddTypeExtension<GixatBackend.Modules.JobCards.GraphQL.InventoryQueries>()
        .AddTypeExtension<GixatBackend.Modules.JobCards.GraphQL.LaborQueries>()
        .AddType<CustomerQueries>()
        .AddType<CustomerExtensions>()
        .AddTypeExtension<HealthCheckQueries>()
        .AddType<InviteQueries>()
        .AddType<GixatBackend.Modules.Appointments.GraphQL.AppointmentQueries>()
    .AddMutationType()
        .AddType<AuthMutations>()
        .AddTypeExtension<UserProfileMutations>()
        .AddType<OrganizationMutations>()
        .AddType<MediaMutations>()
        .AddTypeExtension<PresignedUploadMutations>()
        .AddType<LookupMutations>()
        .AddType<SessionMutations>()
        .AddType<JobCardMutations>()
        .AddTypeExtension<JobCardCommentMutations>()
        .AddTypeExtension<GixatBackend.Modules.JobCards.GraphQL.InventoryMutations>()
        .AddTypeExtension<GixatBackend.Modules.JobCards.GraphQL.JobItemPartsAndLaborMutations>()
        .AddType<CustomerMutations>()
        .AddType<InviteMutations>()
        .AddType<GixatBackend.Modules.Appointments.GraphQL.AppointmentMutations>()
    // DataLoader-based extensions
    .AddType<GixatBackend.Modules.JobCards.GraphQL.JobCardExtensions>()
    .AddType<GixatBackend.Modules.JobCards.GraphQL.JobItemExtensions>()
    .AddType<GixatBackend.Modules.JobCards.GraphQL.JobCardCommentExtensions>()
    .AddType<GixatBackend.Modules.JobCards.GraphQL.JobCardCommentMentionExtensions>()
    .AddType<GixatBackend.Modules.Customers.GraphQL.CarExtensions>()
    .AddUploadType()
    // DataLoaders for N+1 prevention
    .AddDataLoader<GixatBackend.Modules.Customers.Services.CarsByCustomerDataLoader>()
    .AddDataLoader<GixatBackend.Modules.Sessions.Services.SessionsByCustomerDataLoader>()
    .AddDataLoader<GixatBackend.Modules.Sessions.Services.SessionsByCarDataLoader>()
    .AddDataLoader<GixatBackend.Modules.JobCards.Services.JobCardsByCustomerDataLoader>()
    .AddDataLoader<GixatBackend.Modules.JobCards.Services.JobItemsByJobCardDataLoader>()
    .AddDataLoader<GixatBackend.Modules.JobCards.Services.JobCardByIdDataLoader>()
    .AddDataLoader<GixatBackend.Modules.JobCards.Services.JobItemByIdDataLoader>()
    .AddDataLoader<GixatBackend.Modules.JobCards.Services.JobItemPartsDataLoader>()
    .AddDataLoader<GixatBackend.Modules.JobCards.Services.JobItemLaborEntriesDataLoader>()
    .AddDataLoader<GixatBackend.Modules.JobCards.Services.JobCardCommentByIdDataLoader>()
    .AddDataLoader<GixatBackend.Modules.JobCards.Services.CommentRepliesDataLoader>()
    .AddDataLoader<GixatBackend.Modules.JobCards.Services.CommentMentionsDataLoader>()
    .AddDataLoader<GixatBackend.Modules.Users.Services.UserByIdDataLoader>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .ModifyPagingOptions(options =>
    {
        options.DefaultPageSize = PaginationConstants.DefaultPageSize;
        options.MaxPageSize = PaginationConstants.MaxPageSize;
        options.IncludeTotalCount = true;
    })
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true)
    .AddErrorFilter<GixatBackend.Modules.Common.GraphQL.GraphQLErrorFilter>()
    .AddMaxExecutionDepthRule(QueryLimits.MaxExecutionDepth)
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = QueryLimits.MaxFieldCost;
        options.EnforceCostLimits = true;
    })
    .AddAuthorization();

builder.Services.AddOpenApi();

// Add Controllers for REST API endpoints
builder.Services.AddControllers();

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
        ProgramLogMessages.LogSeedingError(logger, ex);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Phase 4: Global exception handler
app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGraphQL();

await app.RunAsync().ConfigureAwait(false);

// LoggerMessage delegates for performance
internal static partial class ProgramLogMessages
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while seeding the database.")]
    public static partial void LogSeedingError(ILogger logger, Exception ex);
}

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Redis connection failed. Cache service will not be available.")]
    public static partial void LogRedisWarning(ILogger logger, Exception ex);
}
