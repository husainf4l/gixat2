namespace GixatBackend.Modules.Common.Constants;

/// <summary>
/// Database and query configuration constants
/// </summary>
internal static class DatabaseConstants
{
    /// <summary>
    /// Default command timeout in seconds
    /// </summary>
    public const int CommandTimeoutSeconds = 30;
    
    /// <summary>
    /// Maximum retry attempts for transient failures
    /// </summary>
    public const int MaxRetryCount = 3;
    
    /// <summary>
    /// Maximum delay between retries in seconds
    /// </summary>
    public const int MaxRetryDelaySeconds = 5;
}

/// <summary>
/// Pagination configuration constants
/// </summary>
internal static class PaginationConstants
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;
}

/// <summary>
/// GraphQL query limits and costs
/// </summary>
internal static class QueryLimits
{
    public const int MaxExecutionDepth = 10;
    public const int MaxFieldCost = 10000;
}
