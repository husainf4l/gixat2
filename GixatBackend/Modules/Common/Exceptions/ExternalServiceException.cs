namespace GixatBackend.Modules.Common.Exceptions;

/// <summary>
/// Exception thrown when external service call fails
/// </summary>
internal sealed class ExternalServiceException : Exception
{
    public string ServiceName { get; }
    public int? StatusCode { get; }

    public ExternalServiceException()
        : base("External service failed")
    {
        ServiceName = string.Empty;
    }

    public ExternalServiceException(string message)
        : base(message)
    {
        ServiceName = string.Empty;
    }

    public ExternalServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
        ServiceName = string.Empty;
    }

    public ExternalServiceException(string serviceName, string message)
        : base($"External service '{serviceName}' failed: {message}")
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, int statusCode)
        : base($"External service '{serviceName}' failed with status {statusCode}: {message}")
    {
        ServiceName = serviceName;
        StatusCode = statusCode;
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException)
        : base($"External service '{serviceName}' failed: {message}", innerException)
    {
        ServiceName = serviceName;
    }
}
