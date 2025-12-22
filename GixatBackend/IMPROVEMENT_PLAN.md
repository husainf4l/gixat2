# GixatBackend Improvement Plan

## Overview
Comprehensive plan to address code quality, performance, and maintainability issues identified in the architecture review.

**Target Completion:** 4-6 weeks  
**Priority:** High → Medium → Low  
**Status:** 🔴 Not Started | 🟡 In Progress | ✅ Completed

---

## Phase 1: Critical Data Integrity (Week 1-2)

### 1.1 Add Input Validation Attributes ⭐ Priority: HIGH
**Status:** 🔴 Not Started  
**Effort:** 2-3 days  
**Impact:** Prevents invalid data at model level

**Files to Update:**
- `Modules/Customers/Models/Customer.cs`
- `Modules/Customers/Models/Car.cs`
- `Modules/Sessions/Models/GarageSession.cs`
- `Modules/JobCards/Models/JobCard.cs`
- `Modules/JobCards/Models/JobItem.cs`
- `Modules/Users/Models/ApplicationUser.cs`
- `Modules/Organizations/Models/Organization.cs`

**Example Changes:**
```csharp
public class Customer
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; set; }
    
    [Required]
    [Phone]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class Car
{
    [Required]
    [MaxLength(50)]
    public string LicensePlate { get; set; } = string.Empty;
    
    [MaxLength(17)]
    [RegularExpression(@"^[A-HJ-NPR-Z0-9]{17}$", ErrorMessage = "Invalid VIN format")]
    public string? VIN { get; set; }
    
    [Range(1900, 2100)]
    public int Year { get; set; }
}

public class JobItem
{
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Range(0, double.MaxValue)]
    public decimal EstimatedLaborCost { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal EstimatedPartsCost { get; set; }
}
```

**Testing:**
- Unit tests for validation attributes
- Integration tests for model binding

---

### 1.2 Add Unique Constraints on Business Keys ⭐ Priority: HIGH
**Status:** 🔴 Not Started  
**Effort:** 1 day  
**Impact:** Prevents duplicate data per organization

**Migration Required:** Yes

**Changes:**
```csharp
// In ApplicationDbContext.OnModelCreating
builder.Entity<Car>()
    .HasIndex(c => new { c.LicensePlate, c.OrganizationId })
    .IsUnique()
    .HasDatabaseName("IX_Cars_LicensePlate_OrgId");

builder.Entity<Car>()
    .HasIndex(c => new { c.VIN, c.OrganizationId })
    .IsUnique()
    .HasFilter("VIN IS NOT NULL")
    .HasDatabaseName("IX_Cars_VIN_OrgId");

builder.Entity<Customer>()
    .HasIndex(c => new { c.Email, c.OrganizationId })
    .IsUnique()
    .HasFilter("Email IS NOT NULL")
    .HasDatabaseName("IX_Customers_Email_OrgId");

builder.Entity<Customer>()
    .HasIndex(c => new { c.PhoneNumber, c.OrganizationId })
    .IsUnique()
    .HasDatabaseName("IX_Customers_Phone_OrgId");
```

**Commands:**
```bash
dotnet ef migrations add AddUniqueConstraintsForBusinessKeys
dotnet ef database update
```

**Error Handling:**
Add duplicate key exception handling in mutations.

---

### 1.3 Add Null Safety Checks in Mutations ⭐ Priority: HIGH
**Status:** 🔴 Not Started  
**Effort:** 2 days  
**Impact:** Prevents runtime null reference exceptions

**Pattern to Apply:**
```csharp
// Before
var session = await context.GarageSessions.FindAsync(sessionId);
session.Status = newStatus; // 💥 Could be null!

// After
var session = await context.GarageSessions.FindAsync(sessionId);
if (session == null)
{
    throw new EntityNotFoundException("Session", sessionId);
}
session.Status = newStatus;
```

**Files to Update:**
- All mutation files in `Modules/*/GraphQL/*Mutations.cs`
- Service files in `Modules/*/Services/*.cs`

---

## Phase 2: Performance Optimization (Week 3)

### 2.1 Implement DataLoader Pattern ⭐ Priority: MEDIUM
**Status:** 🔴 Not Started  
**Effort:** 3-4 days  
**Impact:** Eliminates N+1 queries

**New DataLoaders to Create:**
```
Modules/Customers/Services/CarsByCustomerDataLoader.cs
Modules/Sessions/Services/SessionsByCustomerDataLoader.cs
Modules/JobCards/Services/JobCardsByCustomerDataLoader.cs
Modules/JobCards/Services/JobItemsByJobCardDataLoader.cs
Modules/Users/Services/UserByIdDataLoader.cs (for technician lookup)
```

**Example Implementation:**
```csharp
internal sealed class CarsByCustomerDataLoader : GroupedDataLoader<Guid, Car>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public CarsByCustomerDataLoader(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<ILookup<Guid, Car>> LoadGroupedBatchAsync(
        IReadOnlyList<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var cars = await context.Cars
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.CustomerId))
            .ToListAsync(cancellationToken);
        
        return cars.ToLookup(c => c.CustomerId);
    }
}
```

**Register DataLoaders in Program.cs:**
```csharp
builder.Services
    .AddDbContextFactory<ApplicationDbContext>()
    .AddGraphQLServer()
    .AddDataLoader<CarsByCustomerDataLoader>()
    .AddDataLoader<SessionsByCustomerDataLoader>()
    .AddDataLoader<JobCardsByCustomerDataLoader>()
    .AddDataLoader<JobItemsByJobCardDataLoader>()
    .AddDataLoader<UserByIdDataLoader>()
    // ... existing config
```

**Update Resolvers:**
```csharp
// In CustomerExtensions.cs
public static async Task<IEnumerable<Car>> GetCarsAsync(
    [Parent] Customer customer,
    CarsByCustomerDataLoader dataLoader,
    CancellationToken cancellationToken)
{
    return await dataLoader.LoadAsync(customer.Id, cancellationToken);
}
```

---

### 2.2 Add Query Timeouts and Connection Resilience ⭐ Priority: MEDIUM
**Status:** 🔴 Not Started  
**Effort:** 1 day  
**Impact:** Prevents long-running query issues

**Changes in Program.cs:**
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(30); // 30 seconds
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    })
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment()));
```

**Add Query Timeout Middleware:**
```csharp
// Modules/Common/Middleware/QueryTimeoutMiddleware.cs
public class QueryTimeoutMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

    public QueryTimeoutMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var cts = new CancellationTokenSource(_timeout);
        context.RequestAborted = cts.Token;
        
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            context.Response.StatusCode = 504; // Gateway Timeout
            await context.Response.WriteAsJsonAsync(new { error = "Request timeout" });
        }
    }
}
```

---

### 2.3 Configure Query Complexity Limits ⭐ Priority: MEDIUM
**Status:** 🔴 Not Started  
**Effort:** 2 hours  
**Impact:** Prevents expensive queries

**Update Program.cs:**
```csharp
builder.Services
    .AddGraphQLServer()
    // ... existing config
    .AddMaxExecutionDepthRule(10) // ✅ Already done
    .AddMaxComplexityRule(1000) // Add this
    .ModifyCostOptions(options => 
    {
        options.MaxFieldCost = 10000; // ✅ Already done
        options.DefaultFieldCost = 1;
        options.DefaultResolverCost = 5;
    })
    .UseAutomaticPersistedQueryPipeline() // Add persisted queries
    .AddReadOnlyFileSystemQueryStorage("./persisted-queries");
```

**Add Cost Annotations:**
```csharp
// Expensive queries
[UsePaging]
[Cost(100)] // High cost for paginated queries
public static IQueryable<Customer> GetCustomers(ApplicationDbContext context)
{
    // ...
}

[Cost(50)]
public static async Task<CustomerStatistics> GetCustomerStatisticsAsync(...)
{
    // ...
}
```

---

## Phase 3: Code Quality & Maintainability (Week 4-5)

### 3.1 Extract Business Logic to Service Layer ⭐ Priority: MEDIUM
**Status:** 🔴 Not Started  
**Effort:** 5-6 days  
**Impact:** Testability, reusability, maintainability

**New Service Structure:**
```
Modules/Sessions/Services/
  ├── ISessionService.cs
  ├── SessionService.cs
  └── SessionValidationService.cs

Modules/JobCards/Services/
  ├── IJobCardService.cs
  ├── JobCardService.cs
  ├── JobItemService.cs
  └── JobCardValidationService.cs

Modules/Customers/Services/
  ├── ICustomerService.cs
  ├── CustomerService.cs (already has export service)
  └── CarService.cs
```

**Example Refactor:**
```csharp
// BEFORE (in SessionMutations.cs)
public static async Task<GarageSession> CreateSessionAsync(
    Guid carId,
    Guid customerId,
    ApplicationDbContext context)
{
    // 50+ lines of business logic
}

// AFTER (in SessionMutations.cs)
public static async Task<GarageSession> CreateSessionAsync(
    Guid carId,
    Guid customerId,
    [Service] ISessionService sessionService)
{
    return await sessionService.CreateSessionAsync(carId, customerId);
}

// Business logic moves to SessionService.cs
internal sealed class SessionService : ISessionService
{
    private readonly ApplicationDbContext _context;
    private readonly SessionValidationService _validator;

    public async Task<GarageSession> CreateSessionAsync(Guid carId, Guid customerId)
    {
        // Validation
        await _validator.ValidateNoActiveSessionForCarAsync(carId);
        await _validator.ValidateCarAndCustomerExistAsync(carId, customerId);
        
        // Business logic
        var session = new GarageSession
        {
            CarId = carId,
            CustomerId = customerId,
            Status = SessionStatus.CustomerRequest
        };
        
        _context.GarageSessions.Add(session);
        await _context.SaveChangesAsync();
        
        return session;
    }
}
```

**Register Services:**
```csharp
// In Program.cs
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<SessionValidationService>();
builder.Services.AddScoped<IJobCardService, JobCardService>();
builder.Services.AddScoped<JobItemService>();
builder.Services.AddScoped<JobCardValidationService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<CarService>();
```

---

### 3.2 Refactor Large Mutation Methods ⭐ Priority: MEDIUM
**Status:** 🔴 Not Started  
**Effort:** 3 days  
**Impact:** Testability, readability

**Target Methods (100+ lines):**
- `SessionMutations.GenerateInitialReportAsync()` (230+ lines)
- `JobCardMutations.CreateJobCardFromSessionAsync()` (120+ lines)
- `JobCardMutations.UpdateJobItemStatusAsync()` (150+ lines)

**Refactoring Strategy:**
```csharp
// BEFORE: One giant method
public static async Task<JobCard> CreateJobCardFromSessionAsync(Guid sessionId, ApplicationDbContext context)
{
    // 120 lines of logic
}

// AFTER: Composed of smaller methods
public static async Task<JobCard> CreateJobCardFromSessionAsync(
    Guid sessionId,
    [Service] IJobCardService jobCardService)
{
    return await jobCardService.CreateFromSessionAsync(sessionId);
}

// In JobCardService.cs
public async Task<JobCard> CreateFromSessionAsync(Guid sessionId)
{
    var session = await ValidateAndGetSessionAsync(sessionId);
    var internalNotes = BuildInternalNotes(session);
    var jobCard = CreateJobCard(session, internalNotes);
    var jobItems = ParseJobItems(session);
    
    jobCard.Items.AddRange(jobItems);
    session.Status = SessionStatus.JobCardCreated;
    
    _context.JobCards.Add(jobCard);
    await _context.SaveChangesAsync();
    
    return jobCard;
}

private async Task<GarageSession> ValidateAndGetSessionAsync(Guid sessionId) { }
private string BuildInternalNotes(GarageSession session) { }
private JobCard CreateJobCard(GarageSession session, string notes) { }
private List<JobItem> ParseJobItems(GarageSession session) { }
```

---

### 3.3 Replace Magic Strings with Constants/Enums ⭐ Priority: LOW
**Status:** 🔴 Not Started  
**Effort:** 1 day  
**Impact:** Maintainability, type safety

**Create Constants File:**
```csharp
// Modules/Common/Constants/ValidationConstants.cs
public static class ValidationConstants
{
    public const int MaxNameLength = 100;
    public const int MaxEmailLength = 256;
    public const int MaxPhoneLength = 20;
    public const int MaxLicensePlateLength = 50;
    public const int MaxVINLength = 17;
    public const int MaxDescriptionLength = 500;
    public const int MaxNotesLength = 2000;
    
    public const int MinYear = 1900;
    public const int MaxYear = 2100;
}

// Modules/Common/Constants/CacheKeys.cs
public static class CacheKeys
{
    public const string LookupItems = "lookup_items:{0}"; // {0} = category
    public const string CustomerStats = "customer_stats:{0}"; // {0} = orgId
    public const string UserProfile = "user_profile:{0}"; // {0} = userId
}

// Modules/Common/Constants/ErrorMessages.cs
public static class ErrorMessages
{
    public const string SessionNotFound = "Session not found";
    public const string JobCardNotFound = "Job Card not found";
    public const string CustomerNotFound = "Customer not found";
    public const string CarNotFound = "Car not found";
    public const string InvalidSessionStatus = "Invalid session status for this operation";
    public const string ActiveSessionExists = "An active session already exists for this car";
}
```

**Update Usage:**
```csharp
// BEFORE
[MaxLength(100)]
public string FirstName { get; set; }

throw new InvalidOperationException("Session not found");

// AFTER
[MaxLength(ValidationConstants.MaxNameLength)]
public string FirstName { get; set; }

throw new EntityNotFoundException(ErrorMessages.SessionNotFound);
```

---

## Phase 4: Error Handling & Resilience (Week 6)

### 4.1 Implement Global Exception Handler ⭐ Priority: HIGH
**Status:** 🔴 Not Started  
**Effort:** 1 day  
**Impact:** Security, consistent error responses

**Create Middleware:**
```csharp
// Modules/Common/Middleware/GlobalExceptionHandlerMiddleware.cs
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var errorId = Guid.NewGuid().ToString();
        _logger.LogError(exception, "Unhandled exception {ErrorId}", errorId);

        var response = exception switch
        {
            EntityNotFoundException e => new ErrorResponse
            {
                ErrorId = errorId,
                StatusCode = 404,
                Message = e.Message,
                Details = _env.IsDevelopment() ? e.StackTrace : null
            },
            ValidationException e => new ErrorResponse
            {
                ErrorId = errorId,
                StatusCode = 400,
                Message = e.Message,
                ValidationErrors = e.Errors,
                Details = _env.IsDevelopment() ? e.StackTrace : null
            },
            UnauthorizedAccessException => new ErrorResponse
            {
                ErrorId = errorId,
                StatusCode = 403,
                Message = "Access denied"
            },
            _ => new ErrorResponse
            {
                ErrorId = errorId,
                StatusCode = 500,
                Message = "An unexpected error occurred. Please contact support with error ID.",
                Details = _env.IsDevelopment() ? exception.ToString() : null
            }
        };

        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(response);
    }
}

public record ErrorResponse
{
    public required string ErrorId { get; init; }
    public required int StatusCode { get; init; }
    public required string Message { get; init; }
    public Dictionary<string, string[]>? ValidationErrors { get; init; }
    public string? Details { get; init; }
}
```

**Register in Program.cs:**
```csharp
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
```

**Update GraphQL Config:**
```csharp
.ModifyRequestOptions(opt => 
{
    // Only show exception details in development
    opt.IncludeExceptionDetails = builder.Environment.IsDevelopment();
})
.AddErrorFilter<GraphQLErrorFilter>()
```

---

### 4.2 Create Custom Exception Types ⭐ Priority: MEDIUM
**Status:** 🔴 Not Started  
**Effort:** 2 hours  
**Impact:** Better error handling, clearer intent

**Create Exception Hierarchy:**
```csharp
// Modules/Common/Exceptions/GixatException.cs (Base)
public abstract class GixatException : Exception
{
    public string ErrorCode { get; }
    
    protected GixatException(string message, string errorCode) 
        : base(message)
    {
        ErrorCode = errorCode;
    }
    
    protected GixatException(string message, string errorCode, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

// Business Logic Exceptions
public class EntityNotFoundException : GixatException
{
    public EntityNotFoundException(string entityName, Guid id)
        : base($"{entityName} with ID '{id}' was not found", "ENTITY_NOT_FOUND")
    {
        Data["EntityName"] = entityName;
        Data["EntityId"] = id;
    }
}

public class InvalidSessionStatusException : GixatException
{
    public InvalidSessionStatusException(SessionStatus currentStatus, SessionStatus requiredStatus)
        : base($"Cannot perform this operation. Session is in '{currentStatus}' status but requires '{requiredStatus}'", 
               "INVALID_SESSION_STATUS")
    {
        Data["CurrentStatus"] = currentStatus;
        Data["RequiredStatus"] = requiredStatus;
    }
}

public class DuplicateEntityException : GixatException
{
    public DuplicateEntityException(string entityName, string field, string value)
        : base($"{entityName} with {field} '{value}' already exists", "DUPLICATE_ENTITY")
    {
        Data["EntityName"] = entityName;
        Data["Field"] = field;
        Data["Value"] = value;
    }
}

public class ValidationException : GixatException
{
    public Dictionary<string, string[]> Errors { get; }
    
    public ValidationException(Dictionary<string, string[]> errors)
        : base("One or more validation errors occurred", "VALIDATION_FAILED")
    {
        Errors = errors;
    }
}

public class JobCardCompletionException : GixatException
{
    public JobCardCompletionException(int incompleteItems)
        : base($"Cannot complete job card. {incompleteItems} item(s) are still pending or in progress", 
               "JOB_CARD_INCOMPLETE")
    {
        Data["IncompleteItems"] = incompleteItems;
    }
}

public class ApprovalRequiredException : GixatException
{
    public ApprovalRequiredException(string entityType)
        : base($"{entityType} must be approved by customer before work can begin", 
               "APPROVAL_REQUIRED")
    {
        Data["EntityType"] = entityType;
    }
}
```

**Update Usage:**
```csharp
// BEFORE
if (session == null)
{
    throw new InvalidOperationException("Session not found");
}

// AFTER
if (session == null)
{
    throw new EntityNotFoundException("Session", sessionId);
}

// BEFORE
if (session.Status != SessionStatus.Inspection)
{
    throw new InvalidOperationException("Can only update inspection for sessions in Inspection status");
}

// AFTER
if (session.Status != SessionStatus.Inspection)
{
    throw new InvalidSessionStatusException(session.Status, SessionStatus.Inspection);
}
```

---

### 4.3 Add Retry/Circuit Breaker with Polly ⭐ Priority: MEDIUM
**Status:** 🔴 Not Started  
**Effort:** 1-2 days  
**Impact:** Resilience against transient failures

**Install Package:**
```bash
dotnet add package Polly
dotnet add package Polly.Extensions.Http
```

**Create Resilience Policies:**
```csharp
// Modules/Common/Resilience/ResiliencePolicies.cs
public static class ResiliencePolicies
{
    // Retry policy for database operations
    public static IAsyncPolicy<T> GetDatabaseRetryPolicy<T>()
    {
        return Policy<T>
            .Handle<NpgsqlException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning(
                        "Database operation failed (attempt {RetryAttempt}/3). Retrying in {Delay}ms",
                        retryAttempt,
                        timespan.TotalMilliseconds);
                });
    }

    // Circuit breaker for S3 operations
    public static IAsyncPolicy GetS3CircuitBreakerPolicy()
    {
        return Policy
            .Handle<AmazonS3Exception>()
            .Or<HttpRequestException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (exception, duration) =>
                {
                    // Log circuit breaker opened
                },
                onReset: () =>
                {
                    // Log circuit breaker reset
                },
                onHalfOpen: () =>
                {
                    // Log circuit breaker testing
                });
    }

    // Combined policy for S3: Retry + Circuit Breaker
    public static IAsyncPolicy GetS3ResiliencePolicy()
    {
        var retry = Policy
            .Handle<AmazonS3Exception>(ex => ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)));

        var circuitBreaker = GetS3CircuitBreakerPolicy();

        return Policy.WrapAsync(retry, circuitBreaker);
    }
}
```

**Update S3Service:**
```csharp
internal sealed class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly IAsyncPolicy _resiliencePolicy;

    public S3Service(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["AWS:S3BucketName"];
        _resiliencePolicy = ResiliencePolicies.GetS3ResiliencePolicy();
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            var fileKey = $"{Guid.NewGuid()}-{fileName}";
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = fileKey,
                BucketName = _bucketName,
                ContentType = contentType
            };

            using var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.UploadAsync(uploadRequest);

            return fileKey;
        });
    }
}
```

**Add to EF Core:**
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    }));
```

---

## Testing Strategy

### Unit Tests (Per Phase)
- Phase 1: Validation attribute tests, constraint violation tests
- Phase 3: Service layer unit tests (isolated with mocks)
- Phase 4: Exception handling tests, resilience policy tests

### Integration Tests
- DataLoader N+1 query prevention tests
- End-to-end workflow tests with real database
- Circuit breaker behavior tests

### Performance Tests
- Query complexity benchmarks
- DataLoader vs direct query performance
- Timeout behavior validation

---

## Success Metrics

| Metric | Before | Target | How to Measure |
|--------|--------|--------|----------------|
| Test Coverage | ~5% | >70% | `dotnet test --collect:"XPlat Code Coverage"` |
| N+1 Queries | Unknown | 0 | MiniProfiler or EF Core logging |
| Average Query Time | Unknown | <500ms | Application Insights |
| Exception Rate | Unknown | <0.1% | Error logging analysis |
| Duplicate Data | Possible | 0 | Database constraint violations |
| Code Complexity | High | Medium | SonarQube/CodeMetrics |

---

## Rollout Plan

### Week 1-2: Data Integrity
- Day 1-3: Add validation attributes
- Day 4-5: Add unique constraints + migration
- Day 6-10: Add null safety checks across all mutations

### Week 3: Performance
- Day 1-4: Implement all DataLoaders
- Day 5: Add query timeouts and complexity limits

### Week 4-5: Code Quality
- Day 1-5: Create service layer and move business logic
- Day 6-10: Refactor large methods

### Week 6: Error Handling
- Day 1-2: Global exception handler + custom exceptions
- Day 3-5: Add Polly resilience policies

---

## Dependencies

**NuGet Packages to Add:**
```bash
dotnet add package Polly --version 8.5.0
dotnet add package Polly.Extensions.Http --version 3.0.0
dotnet add package System.ComponentModel.Annotations --version 5.0.0
```

**No Breaking Changes Expected** ✅
- All changes are additive or internal refactoring
- Existing GraphQL API remains compatible

---

## Risk Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Migration fails in production | Low | High | Test on staging, backup before migration |
| DataLoader breaks existing queries | Medium | Medium | Comprehensive integration tests |
| Service layer increases complexity | Low | Low | Clear interfaces, good documentation |
| Circuit breaker causes false positives | Medium | Medium | Tune thresholds based on monitoring |

---

## Post-Implementation

### Monitoring
- Set up Application Insights dashboards
- Configure alerts for:
  - Query timeouts
  - Circuit breaker opens
  - Validation failures
  - Exception rates

### Documentation
- Update `CLAUDE.md` with service layer patterns
- Document DataLoader usage for new developers
- Create troubleshooting guide for common errors

---

**Last Updated:** December 22, 2025  
**Owner:** Development Team  
**Review Cadence:** Weekly progress reviews
