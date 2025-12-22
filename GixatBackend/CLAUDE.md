# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Development Commands

### Building the Project
```bash
# Build the entire solution
dotnet build GixatBackend.sln

# Build with warnings as errors (for production builds)
dotnet build -warnaserrors

# Build in release mode
dotnet build -c Release
```

### Running the Application
```bash
# Run the backend API
dotnet run --project GixatBackend.csproj

# Run with watch for auto-reload during development
dotnet watch run --project GixatBackend.csproj
```

### Testing
```bash
# Run all tests
dotnet test GixatBackend.Tests/GixatBackend.Tests.csproj

# Run tests with coverage
dotnet test GixatBackend.Tests/GixatBackend.Tests.csproj --collect:"XPlat Code Coverage"

# Run a specific test
dotnet test GixatBackend.Tests/GixatBackend.Tests.csproj --filter "FullyQualifiedName~AuthServiceTests.LoginAsync_ValidCredentials_ReturnsToken"
```

### Database Migrations
```bash
# Add a new migration
dotnet ef migrations add MigrationName --project GixatBackend.csproj

# Update database to latest migration
dotnet ef database update --project GixatBackend.csproj

# Rollback to a specific migration
dotnet ef database update PreviousMigrationName --project GixatBackend.csproj

# Remove last migration (if not applied)
dotnet ef migrations remove --project GixatBackend.csproj

# Generate SQL script for migration (for production deployments)
dotnet ef migrations script --project GixatBackend.csproj --output migrations.sql
```

### Code Analysis
```bash
# Run code analysis
dotnet build /p:EnforceCodeStyleInBuild=true /p:TreatWarningsAsErrors=true

# Format code
dotnet format GixatBackend.sln
```

## Architecture Overview

### Technology Stack
- **.NET 10.0** - Latest LTS with modern C# features
- **HotChocolate 15.x** - GraphQL server
- **PostgreSQL** with **Entity Framework Core 10.0**
- **AWS S3** - File storage with presigned URLs
- **Redis** (optional) - Caching layer
- **ASP.NET Core Identity** - Authentication with JWT (RSA-256)

### Module Structure
The codebase follows a modular architecture with clear separation of concerns:

```
Modules/
├── Common/          # Shared services, models, GraphQL
│   ├── Services/
│   │   ├── AWS/     # S3, file validation, compression, virus scan
│   │   ├── Redis/   # Caching
│   │   └── Tenant/  # Multi-tenancy isolation
│   ├── Lookup/      # Reference data (LookupItem system)
│   ├── GraphQL/     # Shared mutations/queries
│   └── Models/      # Shared entities (AppMedia, Address, etc.)
├── Users/           # Authentication, user management
├── Organizations/   # Multi-tenant organizations
├── Customers/       # Customer & car management with export/statistics
├── Sessions/        # Garage session workflow
├── JobCards/        # Job tracking
└── Invites/         # User invitation system
```

### Multi-Tenancy Architecture
**Critical:** This application uses organization-level multi-tenancy with automatic data isolation:

1. **Global Query Filters** in [ApplicationDbContext.cs](Data/ApplicationDbContext.cs):
   - Automatically filters all queries by `OrganizationId`
   - Applied to: Customers, Cars, Sessions, JobCards, Users, Invites

2. **Automatic OrganizationId Assignment**:
   - When creating entities implementing `IMustHaveOrganization`, the `OrganizationId` is automatically set from the authenticated user's context via `ITenantService`
   - Throws exception if user has no organization and tries to create tenant-scoped entities

3. **Tenant Service**:
   - `ITenantService.OrganizationId` extracts organization from JWT claims
   - Injected via `IHttpContextAccessor`
   - Used throughout the application for tenant isolation

**Important:** When querying or creating tenant-scoped entities, do NOT manually filter by `OrganizationId` - EF Core handles this automatically. Only use `.IgnoreQueryFilters()` when you need cross-tenant access (very rare).

### GraphQL Architecture
All API endpoints are GraphQL (no REST controllers):

- **Queries**: Read operations (e.g., `AuthQueries`, `CustomerQueries`, `SessionQueries`)
- **Mutations**: Write operations (e.g., `AuthMutations`, `CustomerMutations`)
- **Extensions**: Data loaders and computed fields (e.g., `CustomerExtensions` for activity loading)
- **Filtering/Sorting/Pagination**: Built-in via HotChocolate `.AddProjections()`, `.AddFiltering()`, `.AddSorting()`

**GraphQL Configuration** in [Program.cs](Program.cs:177-214):
- Max page size: 100 (default: 50)
- Max execution depth: 10 (prevents DoS)
- Max field cost: 10,000
- Includes total count in paginated results

### File Upload Architecture - Presigned URLs
**Critical:** This application uses a two-step presigned URL upload flow:

1. **Frontend requests presigned URL** → Backend validates and returns S3 presigned URL (15min expiry)
2. **Frontend uploads directly to S3** → No backend bottleneck
3. **Frontend calls `ProcessUploadedFile`** → Backend validates, scans, compresses, creates DB record

**Key Services:**
- [IS3Service](Modules/Common/Services/AWS/IS3Service.cs) - Presigned URL generation, file operations
- [FileValidationService](Modules/Common/Services/AWS/FileValidationService.cs) - Extension whitelist, MIME type validation, size limits
- [IVirusScanService](Modules/Common/Services/AWS/IVirusScanService.cs) - ClamAV integration (currently stubbed)
- [IImageCompressionService](Modules/Common/Services/AWS/IImageCompressionService.cs) - Image compression with ImageSharp

**Bulk Upload Support:**
- `GetBulkPresignedUploadUrlsAsync` - Up to 50 files at once
- `ProcessBulkUploadedFilesAsync` - Parallel processing of multiple files
- `ProcessBulkSessionUploadsAsync` - Session-specific bulk uploads

**S3 Folder Structure:**
```
{bucket}/
├── {organizationId}/           # Tenant isolation at S3 level
│   ├── customers/{customerId}/{fileKey}
│   ├── sessions/{sessionId}/{fileKey}
│   ├── jobcards/{jobCardId}/{fileKey}
│   └── organizations/{organizationId}/{fileKey}
```

### Authentication & Authorization
- **JWT with RSA-256** asymmetric signing (public/private key pair)
- **Token location**: Both Authorization header AND `access_token` cookie (flexible client support)
- **Google OAuth** integration via ASP.NET Identity
- **Authorization**: GraphQL `[Authorize]` attributes + role-based claims

**Important:** JWT keys should be in environment variables/Key Vault (currently in appsettings.json - needs migration).

### Database Design Patterns
- **EF Core with PostgreSQL** using Npgsql provider
- **Query splitting enabled** (`QuerySplittingBehavior.SplitQuery`) to prevent cartesian explosion on complex joins
- **Composite indexes** on frequently filtered columns (e.g., `LookupItem`: category + isActive + parentId)
- **Cascade delete** for related entities (e.g., Account → User)
- **Unique constraints** on Provider + ProviderAccountId

### Lookup System (Reference Data)
The `LookupItem` system provides hierarchical reference data:
- **Categories**: "CarMake", "CarModel", "ServiceType", etc.
- **Parent-Child relationships**: e.g., "Toyota" → "Camry", "Corolla"
- **Tenant-agnostic**: Shared across all organizations
- **Performance**: Indexed on (Category, IsActive, ParentId)

**Usage:** Use `LookupQueries` to fetch dropdown options, validation lists, etc.

### Customer Statistics & Activity
The `Customer` module includes:
- **CustomerStatistics**: Computed fields (total spent, sessions count, last visit)
- **CustomerActivityDataLoader**: GraphQL DataLoader to prevent N+1 queries when loading customer activity
- **CustomerExportService**: Export customers to CSV/Excel

**Performance:** Use `CustomerActivityDataLoader` when fetching customer activity to batch database queries.

### Redis Caching (Optional)
- **Optional for local development** - application works without Redis
- **Graceful degradation**: Startup catches `RedisConnectionException` and continues without cache
- **IRedisCacheService**: Generic caching interface

## Development Best Practices

### Code Style
- **AnalysisLevel**: `latest-all` (strictest code analysis)
- **EnforceCodeStyleInBuild**: Enabled
- **TreatWarningsAsErrors**: Disabled in csproj (but can be enabled for CI/CD)
- **Nullable reference types**: Enabled
- **ConfigureAwait(false)**: Use everywhere in library code

### Async/Await
All I/O operations must be async:
```csharp
// Always use ConfigureAwait(false) in non-UI code
await context.SaveChangesAsync().ConfigureAwait(false);
await s3Client.PutObjectAsync(request).ConfigureAwait(false);
```

### Input Validation
```csharp
// Use modern C# null checking
ArgumentNullException.ThrowIfNull(parameter);
ArgumentException.ThrowIfNullOrWhiteSpace(stringParameter);
```

### Logging
Use structured logging with `LoggerMessage` source generators:
```csharp
internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Operation failed for {EntityId}")]
    public static partial void LogOperationFailed(ILogger logger, string entityId, Exception ex);
}

// Usage
Log.LogOperationFailed(_logger, customerId, exception);
```

### Dependency Injection
- Services are `internal sealed` with interface-based registration
- Use constructor injection for all dependencies
- Scoped services: `DbContext`, `ITenantService`, `IAuthService`
- Singleton services: `IConnectionMultiplexer` (Redis), `IAmazonS3`

### GraphQL Resolvers
Use static extension methods for computed fields:
```csharp
[ExtendObjectType<Customer>]
public static class CustomerExtensions
{
    public static async Task<List<GarageSession>> GetRecentSessions(
        [Parent] Customer customer,
        ApplicationDbContext context)
    {
        // Implementation
    }
}
```

Register in [Program.cs](Program.cs) with `.AddType(typeof(CustomerExtensions))`

## Environment Variables Required

Create a `.env` file in the project root:
```bash
# Database
DB_SERVER=localhost
DB_DATABASE=gixat
DB_USER=postgres
DB_PASSWORD=your_password

# JWT Keys (should be in Key Vault for production)
# Generated via: openssl genrsa -out private.pem 2048 && openssl rsa -in private.pem -pubout -out public.pem

# AWS
AWS_ACCESS_KEY=your_access_key
AWS_SECRET_KEY=your_secret_key
AWS_REGION=me-central-1
AWS_S3_BUCKET=gixat-files

# Redis (optional)
REDIS_CONNECTION_STRING=localhost:6379

# Google OAuth
GOOGLE_CLIENT_ID=your_client_id
GOOGLE_CLIENT_SECRET=your_client_secret
```

**Critical:** Never commit `.env` or `appsettings.json` with secrets to version control.

## GraphQL Schema Exploration

Access GraphQL Playground (development only):
```
http://localhost:5000/graphql/
```

**Sample Queries:**
```graphql
# Get customers with filtering
query {
  customers(
    where: { name: { contains: "John" } }
    order: { createdAt: DESC }
    first: 10
  ) {
    nodes {
      id
      name
      email
      cars {
        make
        model
      }
    }
    pageInfo {
      hasNextPage
      hasPreviousPage
    }
    totalCount
  }
}

# Get presigned upload URL
mutation {
  getPresignedUploadUrl(
    fileName: "car-photo.jpg"
    contentType: "image/jpeg"
    entityType: CUSTOMER
    entityId: "customer-id"
  ) {
    uploadUrl
    fileKey
    expiresAt
  }
}
```

## Common Development Tasks

### Adding a New Module
1. Create folder structure: `Modules/NewModule/{Models, Services, GraphQL}`
2. Create entities implementing `IMustHaveOrganization` (if tenant-scoped)
3. Add `DbSet<NewEntity>` to [ApplicationDbContext.cs](Data/ApplicationDbContext.cs)
4. Add global query filter in `OnModelCreating` (if tenant-scoped)
5. Create GraphQL queries/mutations extending `Query`/`Mutation`
6. Register in [Program.cs](Program.cs) with `.AddType(typeof(NewQueries))`
7. Generate migration: `dotnet ef migrations add AddNewModule`

### Adding File Upload Support to Entity
1. Add `AppMedia` navigation property to entity
2. Use `GetPresignedUploadUrl` mutation with appropriate `EntityType`
3. Call `ProcessUploadedFile` after upload completes
4. File automatically associated with entity via `EntityType` + `EntityId`

### Working with Tests
Test project uses:
- **xUnit** - Test framework
- **Moq** - Mocking
- **FluentAssertions** - Assertion library
- **EF Core InMemory** - Database mocking

Example test structure:
```csharp
public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Setup mocks
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        // Act
        // Assert
    }
}
```

## Known Issues & TODOs

### Critical (Production Blockers)
- **Virus Scanning**: [ClamAvScanService.cs](Modules/Common/Services/AWS/ClamAvScanService.cs) is stubbed - always returns clean
- **JWT Keys**: Private key in appsettings.json should be moved to environment variables/Key Vault
- **CORS Policy**: Too permissive for local networks - tighten for production

### High Priority
- **Exception Details**: `IncludeExceptionDetails = true` in [Program.cs](Program.cs:211) exposes stack traces in production
- **Password Requirements**: Min 6 chars is too weak - increase to 12+ for production
- **Video Compression**: [ImageCompressionService.cs](Modules/Common/Services/AWS/ImageCompressionService.cs) `CompressVideoAsync` not implemented

### Medium Priority
- **Rate Limiting**: Not implemented - add AspNetCoreRateLimit middleware
- **Health Checks**: No `/health` endpoint for container orchestration
- **Database Connection Pooling**: No max pool size configured
- **Query Timeouts**: No command timeout configured

## Documentation References
- [ARCHITECTURE_REVIEW.md](ARCHITECTURE_REVIEW.md) - Comprehensive security and architecture analysis
- [PRESIGNED_UPLOAD_ARCHITECTURE.md](PRESIGNED_UPLOAD_ARCHITECTURE.md) - File upload flow details
- [SECURITY_S3_BEST_PRACTICES.md](SECURITY_S3_BEST_PRACTICES.md) - S3 security configuration
- [SECURITY_FILE_UPLOAD.md](SECURITY_FILE_UPLOAD.md) - File upload validation requirements
- [AWS_S3_SETUP.md](AWS_S3_SETUP.md) - AWS setup instructions
- [S3_FOLDER_STRUCTURE.md](S3_FOLDER_STRUCTURE.md) - S3 folder organization
