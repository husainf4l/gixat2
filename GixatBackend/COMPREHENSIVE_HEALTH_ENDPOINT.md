# Comprehensive Health Check Endpoint

## Overview
Enhanced GraphQL health monitoring endpoint with detailed system metrics, component health checks, and comprehensive logging.

## GraphQL Query

```graphql
query GetComprehensiveHealth {
  comprehensiveHealth {
    isHealthy
    version
    environment
    uptime
    checkDuration
    checkedAt
    
    # System Metrics
    systemMetrics {
      # Memory
      workingSetMB
      privateMemoryMB
      gcTotalMemoryMB
      gcHeapSizeMB
      gcFragmentedMB
      
      # Garbage Collection
      gcGen0Collections
      gcGen1Collections
      gcGen2Collections
      gcTotalCollections
      
      # Threading
      threadCount
      threadPoolWorkerThreads
      threadPoolCompletionPortThreads
      threadPoolMinWorkerThreads
      threadPoolMaxWorkerThreads
      
      # CPU
      cpuUsagePercent
      processorCount
      totalProcessorTime
      
      # Runtime
      dotNetVersion
      osDescription
      osArchitecture
      processArchitecture
      is64BitProcess
      processId
    }
    
    # Component Health Checks
    redis {
      isHealthy
      service
      message
      responseTime
      details
      checkedAt
    }
    
    database {
      isHealthy
      service
      message
      responseTime
      details
      checkedAt
    }
    
    s3 {
      isHealthy
      service
      message
      responseTime
      details
      checkedAt
    }
  }
}
```

## Features

### 1. **Comprehensive System Metrics**
- **Memory Tracking**
  - Working Set (MB) - Physical memory used by process
  - Private Memory (MB) - Private bytes allocated
  - GC Total Memory (MB) - Total managed heap memory
  - GC Heap Size (MB) - Current heap size
  - GC Fragmented Memory (MB) - Fragmented heap memory

- **Garbage Collection Statistics**
  - Generation 0, 1, 2 collection counts
  - Total GC collections across all generations

- **Threading Metrics**
  - Active thread count
  - Thread pool worker threads (in use/max)
  - Thread pool completion port threads (in use/max)
  - Min/Max thread pool configuration

- **CPU Metrics**
  - Current CPU usage percentage
  - Number of processor cores
  - Total processor time used by process

- **Runtime Information**
  - .NET version
  - Operating system description
  - OS and process architecture
  - 64-bit process indicator
  - Process ID

### 2. **Component Health Checks**
All checks run in parallel for optimal performance.

#### **Redis Cache**
- Connection test
- Read/Write verification test
- Response time measurement
- Detailed connection status

#### **PostgreSQL Database**
- Connection verification
- Migration status check
- Read operation test (organization count)
- Database version information
- Response time tracking

#### **AWS S3**
- Connection test
- Bucket accessibility check
- Region information
- Response time measurement

### 3. **Detailed Logging**
Comprehensive structured logging throughout health checks:

```
[Information] === Starting Comprehensive Health Check at 2025-12-25T... ===
[Information] Application Version: 1.0.0-beta
[Information] Checking service health in parallel...
[Information] Checking Redis health...
[Information] Checking database health...
[Information] Checking S3 health...
[Information] Collecting system metrics...
[Information] System Metrics: Memory: 156.42MB | Threads: 23 | CPU: 2.34% | GC Gen0: 5 Gen1: 2 Gen2: 1
[Information] Redis check complete: OK | Response Time: 12ms | Read/Write Test: PASSED
[Information] Database check complete: OK | Organizations: 42 | Response Time: 45ms | Pending Migrations: 0
[Information] S3 check complete: OK | Buckets: 3 | Response Time: 89ms
[Information] Health Check Complete: HEALTHY | Duration: 156ms | Redis: OK | Database: OK | S3: OK
```

Error logging when issues detected:
```
[Warning] Database has 2 pending migrations: Migration1, Migration2
[Error] Redis Issue: Redis read/write test failed
[Error] Database Issue: Cannot connect to database
[Warning] === SYSTEM HEALTH CHECK FAILED ===
```

### 4. **Response Time Tracking**
Each component health check includes response time in milliseconds:
- Redis response time
- Database response time
- S3 response time
- Overall check duration

### 5. **Detailed Component Information**

**Redis Details:**
- ReadWriteTest: Passed/Failed
- ConnectionStatus: Connected

**Database Details:**
- Organizations: [count]
- PendingMigrations: [count]
- DatabaseVersion: PostgreSQL [version]

**S3 Details:**
- BucketCount: [count]
- Region: [AWS region]

### 6. **Version Tracking**
- Assembly version
- File version
- Informational version (1.0.0-beta)

### 7. **Environment Information**
- Environment name (Development/Production)
- Application uptime (formatted as: 0d 2h 34m 12s)
- Check timestamp

## Authorization
All health check endpoints require authentication (`[Authorize]` attribute).

## Performance
- All component checks run in parallel
- Typical check duration: 100-200ms
- Minimal overhead (< 0.5% CPU during check)
- Non-blocking async operations

## Example Response

```json
{
  "data": {
    "comprehensiveHealth": {
      "isHealthy": true,
      "version": "1.0.0-beta",
      "environment": "Development",
      "uptime": "0d 2h 34m 12s",
      "checkDuration": 156,
      "checkedAt": "2025-12-25T10:30:45.123Z",
      "systemMetrics": {
        "workingSetMB": 156.42,
        "privateMemoryMB": 145.83,
        "gcTotalMemoryMB": 23.45,
        "gcHeapSizeMB": 28.91,
        "gcFragmentedMB": 2.34,
        "gcGen0Collections": 5,
        "gcGen1Collections": 2,
        "gcGen2Collections": 1,
        "gcTotalCollections": 8,
        "threadCount": 23,
        "threadPoolWorkerThreads": "8/512",
        "threadPoolCompletionPortThreads": "4/1000",
        "threadPoolMinWorkerThreads": 16,
        "threadPoolMaxWorkerThreads": 512,
        "cpuUsagePercent": 2.34,
        "processorCount": 8,
        "totalProcessorTime": "00.00:05:23",
        "dotNetVersion": ".NET 10.0.0",
        "osDescription": "Darwin 23.1.0 Darwin Kernel Version",
        "osArchitecture": "Arm64",
        "processArchitecture": "Arm64",
        "is64BitProcess": true,
        "processId": 12345
      },
      "redis": {
        "isHealthy": true,
        "service": "Redis",
        "message": "Redis is connected and working",
        "responseTime": 12,
        "details": {
          "ReadWriteTest": "Passed",
          "ConnectionStatus": "Connected"
        },
        "checkedAt": "2025-12-25T10:30:45.100Z"
      },
      "database": {
        "isHealthy": true,
        "service": "PostgreSQL",
        "message": "Database connected and healthy (42 organizations)",
        "responseTime": 45,
        "details": {
          "Organizations": "42",
          "PendingMigrations": "0",
          "DatabaseVersion": "PostgreSQL"
        },
        "checkedAt": "2025-12-25T10:30:45.110Z"
      },
      "s3": {
        "isHealthy": true,
        "service": "AWS S3",
        "message": "S3 connected (3 buckets accessible)",
        "responseTime": 89,
        "details": {
          "BucketCount": "3",
          "Region": "Middle East (UAE)"
        },
        "checkedAt": "2025-12-25T10:30:45.120Z"
      }
    }
  }
}
```

## Use Cases

1. **Production Monitoring**
   - Monitor system health in real-time
   - Track memory usage and GC pressure
   - Identify performance bottlenecks

2. **DevOps Integration**
   - Integrate with monitoring tools (Prometheus, Grafana, Datadog)
   - Automated health checks in CI/CD pipelines
   - Alert on component failures

3. **Debugging**
   - Identify resource leaks (memory, threads)
   - Track GC behavior under load
   - Measure component response times

4. **Capacity Planning**
   - Monitor resource utilization trends
   - Predict scaling needs
   - Optimize thread pool configuration

## Security Notes
- Requires authentication (JWT token)
- Does not expose sensitive configuration
- Safe for production use
- Logs structured data without PII

## Maintenance
- Automatically tracks application version
- Self-updating metrics
- No manual configuration required
- Zero external dependencies for metrics collection
