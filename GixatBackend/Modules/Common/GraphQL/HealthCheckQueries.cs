using GixatBackend.Modules.Common.Services.Redis;
using GixatBackend.Data;
using Microsoft.EntityFrameworkCore;
using Amazon.S3;
using HotChocolate.Authorization;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace GixatBackend.Modules.Common.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
internal sealed class HealthCheckQueries
{
    /// <summary>
    /// Get comprehensive system health status with version information
    /// </summary>
    [Authorize]
    public static async Task<ComprehensiveHealthResult> GetComprehensiveHealthAsync(
        [Service] IRedisCacheService? redisCacheService,
        [Service] ApplicationDbContext dbContext,
        [Service] IAmazonS3? s3Client,
        [Service] ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("HealthCheck");
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("=== Starting Comprehensive Health Check at {CheckTime} ===", startTime);

        // Get version information
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0.0";
        var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? version;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? fileVersion;

        logger.LogInformation("Application Version: {Version}", informationalVersion);

        // Collect system metrics
        var systemMetrics = CollectSystemMetrics(logger);

        // Check all services in parallel
        logger.LogInformation("Checking service health in parallel...");
        var redisTask = CheckRedisHealthAsync(redisCacheService);
        var databaseTask = CheckDatabaseHealthAsync(dbContext, logger);
        var s3Task = CheckS3HealthAsync(s3Client, logger);

        await Task.WhenAll(redisTask, databaseTask, s3Task).ConfigureAwait(false);

        var redisHealth = await redisTask;
        var databaseHealth = await databaseTask;
        var s3Health = await s3Task;

        stopwatch.Stop();

        var isHealthy = redisHealth.IsHealthy && databaseHealth.IsHealthy && s3Health.IsHealthy;

        logger.LogInformation(
            "Health Check Complete: {Status} | Duration: {Duration}ms | Redis: {Redis} | Database: {Database} | S3: {S3}",
            isHealthy ? "HEALTHY" : "UNHEALTHY",
            stopwatch.ElapsedMilliseconds,
            redisHealth.IsHealthy ? "OK" : "FAIL",
            databaseHealth.IsHealthy ? "OK" : "FAIL",
            s3Health.IsHealthy ? "OK" : "FAIL"
        );

        if (!isHealthy)
        {
            logger.LogWarning("=== SYSTEM HEALTH CHECK FAILED ===");
            if (!redisHealth.IsHealthy) logger.LogError("Redis Issue: {Message}", redisHealth.Message);
            if (!databaseHealth.IsHealthy) logger.LogError("Database Issue: {Message}", databaseHealth.Message);
            if (!s3Health.IsHealthy) logger.LogError("S3 Issue: {Message}", s3Health.Message);
        }

        return new ComprehensiveHealthResult
        {
            IsHealthy = isHealthy,
            Version = informationalVersion,
            Environment = GetEnvironmentName(),
            Uptime = GetUptime(),
            CheckDuration = stopwatch.ElapsedMilliseconds,
            SystemMetrics = systemMetrics,
            Redis = redisHealth,
            Database = databaseHealth,
            S3 = s3Health,
            CheckedAt = startTime
        };
    }

    private static async Task<HealthCheckResult> CheckDatabaseHealthAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Checking database health...");

        try
        {
            // Test database connection with a simple query
            var canConnect = await dbContext.Database.CanConnectAsync().ConfigureAwait(false);
            
            if (!canConnect)
            {
                logger.LogError("Database connection failed - cannot connect to PostgreSQL");
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Service = "PostgreSQL",
                    Message = "Cannot connect to database",
                    ResponseTime = stopwatch.ElapsedMilliseconds,
                    CheckedAt = DateTime.UtcNow
                };
            }

            // Check if migrations are applied
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync().ConfigureAwait(false);
            var hasPendingMigrations = pendingMigrations.Any();

            if (hasPendingMigrations)
            {
                logger.LogWarning("Database has {Count} pending migrations: {Migrations}", 
                    pendingMigrations.Count(), 
                    string.Join(", ", pendingMigrations));
            }

            // Test read operation
            var organizationCount = await dbContext.Organizations.CountAsync().ConfigureAwait(false);

            // Get database version
            var dbVersion = await dbContext.Database.SqlQueryRaw<string>("SELECT version()").FirstOrDefaultAsync().ConfigureAwait(false);

            stopwatch.Stop();

            var message = hasPendingMigrations 
                ? $"Database connected ({organizationCount} organizations) - WARNING: {pendingMigrations.Count()} pending migrations"
                : $"Database connected and healthy ({organizationCount} organizations)";

            logger.LogInformation(
                "Database check complete: {Status} | Organizations: {Count} | Response Time: {Time}ms | Pending Migrations: {Pending}",
                hasPendingMigrations ? "WARNING" : "OK",
                organizationCount,
                stopwatch.ElapsedMilliseconds,
                pendingMigrations.Count()
            );

            return new HealthCheckResult
            {
                IsHealthy = !hasPendingMigrations,
                Service = "PostgreSQL",
                Message = message,
                ResponseTime = stopwatch.ElapsedMilliseconds,
                Details = new Dictionary<string, string>
                {
                    ["Organizations"] = organizationCount.ToString(),
                    ["PendingMigrations"] = pendingMigrations.Count().ToString(),
                    ["DatabaseVersion"] = dbVersion?.Split(' ')[0] ?? "Unknown"
                },
                CheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Database health check failed: {Message}", ex.Message);
            
            return new HealthCheckResult
            {
                IsHealthy = false,
                Service = "PostgreSQL",
                Message = $"Database health check failed: {ex.Message}",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    private static async Task<HealthCheckResult> CheckS3HealthAsync(IAmazonS3? s3Client, ILogger logger)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Checking S3 health...");

        if (s3Client == null)
        {
            logger.LogWarning("S3 client is not configured");
            return new HealthCheckResult
            {
                IsHealthy = false,
                Service = "AWS S3",
                Message = "S3 client is not configured",
                ResponseTime = 0,
                CheckedAt = DateTime.UtcNow
            };
        }

        try
        {
            // Test S3 connection by listing buckets (minimal cost operation)
            var buckets = await s3Client.ListBucketsAsync().ConfigureAwait(false);
            var bucketCount = buckets.Buckets.Count;

            stopwatch.Stop();

            logger.LogInformation(
                "S3 check complete: OK | Buckets: {Count} | Response Time: {Time}ms",
                bucketCount,
                stopwatch.ElapsedMilliseconds
            );

            return new HealthCheckResult
            {
                IsHealthy = true,
                Service = "AWS S3",
                Message = $"S3 connected ({bucketCount} buckets accessible)",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                Details = new Dictionary<string, string>
                {
                    ["BucketCount"] = bucketCount.ToString(),
                    ["Region"] = s3Client.Config.RegionEndpoint?.DisplayName ?? "Unknown"
                },
                CheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "S3 health check failed: {Message}", ex.Message);
            
            return new HealthCheckResult
            {
                IsHealthy = false,
                Service = "AWS S3",
                Message = $"S3 health check failed: {ex.Message}",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    private static string GetEnvironmentName()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    }

    private static string GetUptime()
    {
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
    }

    private static SystemMetrics CollectSystemMetrics(ILogger logger)
    {
        logger.LogInformation("Collecting system metrics...");
        
        var process = Process.GetCurrentProcess();
        var gcInfo = GC.GetGCMemoryInfo();

        // Memory metrics
        var workingSetMB = process.WorkingSet64 / 1024.0 / 1024.0;
        var privateMemoryMB = process.PrivateMemorySize64 / 1024.0 / 1024.0;
        var gcTotalMemoryMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
        var gcHeapSizeMB = gcInfo.HeapSizeBytes / 1024.0 / 1024.0;
        var gcFragmentedMB = gcInfo.FragmentedBytes / 1024.0 / 1024.0;

        // GC metrics
        var gen0Collections = GC.CollectionCount(0);
        var gen1Collections = GC.CollectionCount(1);
        var gen2Collections = GC.CollectionCount(2);

        // Thread metrics
        var threadCount = process.Threads.Count;
        ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionPortThreads);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
        ThreadPool.GetMinThreads(out var minWorkerThreads, out var minCompletionPortThreads);

        // CPU metrics
        var totalProcessorTime = process.TotalProcessorTime;
        var cpuUsagePercent = 0.0;
        try
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            Thread.Sleep(100); // Sample for 100ms
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            cpuUsagePercent = (cpuUsedMs / (Environment.ProcessorCount * totalMsPassed)) * 100.0;
        }
        catch
        {
            // CPU sampling failed, leave at 0
        }

        // Runtime info
        var dotnetVersion = RuntimeInformation.FrameworkDescription;
        var osDescription = RuntimeInformation.OSDescription;
        var osArchitecture = RuntimeInformation.OSArchitecture.ToString();
        var processArchitecture = RuntimeInformation.ProcessArchitecture.ToString();

        logger.LogInformation(
            "System Metrics: Memory: {Memory:F2}MB | Threads: {Threads} | CPU: {CPU:F2}% | GC Gen0: {Gen0} Gen1: {Gen1} Gen2: {Gen2}",
            workingSetMB,
            threadCount,
            cpuUsagePercent,
            gen0Collections,
            gen1Collections,
            gen2Collections
        );

        return new SystemMetrics
        {
            // Memory
            WorkingSetMB = Math.Round(workingSetMB, 2),
            PrivateMemoryMB = Math.Round(privateMemoryMB, 2),
            GCTotalMemoryMB = Math.Round(gcTotalMemoryMB, 2),
            GCHeapSizeMB = Math.Round(gcHeapSizeMB, 2),
            GCFragmentedMB = Math.Round(gcFragmentedMB, 2),
            
            // GC
            GCGen0Collections = gen0Collections,
            GCGen1Collections = gen1Collections,
            GCGen2Collections = gen2Collections,
            GCTotalCollections = gen0Collections + gen1Collections + gen2Collections,
            
            // Threading
            ThreadCount = threadCount,
            ThreadPoolWorkerThreads = $"{maxWorkerThreads - availableWorkerThreads}/{maxWorkerThreads}",
            ThreadPoolCompletionPortThreads = $"{maxCompletionPortThreads - availableCompletionPortThreads}/{maxCompletionPortThreads}",
            ThreadPoolMinWorkerThreads = minWorkerThreads,
            ThreadPoolMaxWorkerThreads = maxWorkerThreads,
            
            // CPU
            CPUUsagePercent = Math.Round(cpuUsagePercent, 2),
            ProcessorCount = Environment.ProcessorCount,
            TotalProcessorTime = totalProcessorTime.ToString(@"dd\.hh\:mm\:ss"),
            
            // Runtime
            DotNetVersion = dotnetVersion,
            OSDescription = osDescription,
            OSArchitecture = osArchitecture,
            ProcessArchitecture = processArchitecture,
            Is64BitProcess = Environment.Is64BitProcess,
            ProcessId = process.Id
        };
    }

    /// <summary>
    /// Check if Redis cache is connected and working
    /// </summary>
    [Authorize]
    public static async Task<HealthCheckResult> CheckRedisHealthAsync(
        [Service] IRedisCacheService? redisCacheService)
    {
        var stopwatch = Stopwatch.StartNew();

        if (redisCacheService == null)
        {
            return new HealthCheckResult
            {
                IsHealthy = false,
                Service = "Redis",
                Message = "Redis cache service is not configured",
                ResponseTime = 0,
                CheckedAt = DateTime.UtcNow
            };
        }

        try
        {
            var isConnected = await redisCacheService.IsConnectedAsync().ConfigureAwait(false);
            
            if (!isConnected)
            {
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Service = "Redis",
                    Message = "Redis is not connected",
                    ResponseTime = stopwatch.ElapsedMilliseconds,
                    CheckedAt = DateTime.UtcNow
                };
            }

            // Test write and read
            var testKey = $"health-check-{Guid.NewGuid()}";
            var testValue = DateTime.UtcNow.ToString("O");
            
            await redisCacheService.SetStringAsync(testKey, testValue, TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            var retrievedValue = await redisCacheService.GetStringAsync(testKey).ConfigureAwait(false);
            await redisCacheService.RemoveAsync(testKey).ConfigureAwait(false);

            stopwatch.Stop();

            var isWorking = retrievedValue == testValue;

            return new HealthCheckResult
            {
                IsHealthy = isWorking,
                Service = "Redis",
                Message = isWorking ? "Redis is connected and working" : "Redis read/write test failed",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                Details = new Dictionary<string, string>
                {
                    ["ReadWriteTest"] = isWorking ? "Passed" : "Failed",
                    ["ConnectionStatus"] = "Connected"
                },
                CheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            return new HealthCheckResult
            {
                IsHealthy = false,
                Service = "Redis",
                Message = $"Redis health check failed: {ex.Message}",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Get overall system health status
    /// </summary>
    [Authorize]
    public static async Task<SystemHealthResult> GetSystemHealthAsync(
        [Service] IRedisCacheService? redisCacheService)
    {
        var redisHealth = await CheckRedisHealthAsync(redisCacheService).ConfigureAwait(false);

        return new SystemHealthResult
        {
            IsHealthy = redisHealth.IsHealthy,
            Redis = redisHealth,
            CheckedAt = DateTime.UtcNow
        };
    }
}

[ObjectType]
internal sealed record HealthCheckResult
{
    public required bool IsHealthy { get; init; }
    public required string Service { get; init; }
    public required string Message { get; init; }
    public required long ResponseTime { get; init; }
    public Dictionary<string, string>? Details { get; init; }
    public required DateTime CheckedAt { get; init; }
}

[ObjectType]
internal sealed record SystemHealthResult
{
    public required bool IsHealthy { get; init; }
    public required HealthCheckResult Redis { get; init; }
    public required DateTime CheckedAt { get; init; }
}

[ObjectType]
internal sealed record ComprehensiveHealthResult
{
    public required bool IsHealthy { get; init; }
    public required string Version { get; init; }
    public required string Environment { get; init; }
    public required string Uptime { get; init; }
    public required long CheckDuration { get; init; }
    public required SystemMetrics SystemMetrics { get; init; }
    public required HealthCheckResult Redis { get; init; }
    public required HealthCheckResult Database { get; init; }
    public required HealthCheckResult S3 { get; init; }
    public required DateTime CheckedAt { get; init; }
}

[ObjectType]
internal sealed record SystemMetrics
{
    // Memory Metrics
    public required double WorkingSetMB { get; init; }
    public required double PrivateMemoryMB { get; init; }
    public required double GCTotalMemoryMB { get; init; }
    public required double GCHeapSizeMB { get; init; }
    public required double GCFragmentedMB { get; init; }
    
    // Garbage Collection Metrics
    public required int GCGen0Collections { get; init; }
    public required int GCGen1Collections { get; init; }
    public required int GCGen2Collections { get; init; }
    public required int GCTotalCollections { get; init; }
    
    // Threading Metrics
    public required int ThreadCount { get; init; }
    public required string ThreadPoolWorkerThreads { get; init; }
    public required string ThreadPoolCompletionPortThreads { get; init; }
    public required int ThreadPoolMinWorkerThreads { get; init; }
    public required int ThreadPoolMaxWorkerThreads { get; init; }
    
    // CPU Metrics
    public required double CPUUsagePercent { get; init; }
    public required int ProcessorCount { get; init; }
    public required string TotalProcessorTime { get; init; }
    
    // Runtime Information
    public required string DotNetVersion { get; init; }
    public required string OSDescription { get; init; }
    public required string OSArchitecture { get; init; }
    public required string ProcessArchitecture { get; init; }
    public required bool Is64BitProcess { get; init; }
    public required int ProcessId { get; init; }
}
