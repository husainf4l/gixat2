using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Common.Services.AWS;

[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "Used by GraphQL mutations via dependency injection")]
public interface IVirusScanService
{
    /// <summary>
    /// Scans a file for viruses and malware
    /// </summary>
    /// <param name="stream">The file stream to scan</param>
    /// <param name="fileName">The name of the file being scanned</param>
    /// <returns>True if file is clean, false if malware detected</returns>
    Task<ScanResult> ScanFileAsync(Stream stream, string fileName);
}

[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "Used by public interface IVirusScanService")]
public sealed class ScanResult
{
    public bool IsClean { get; init; }
    public string? ThreatName { get; init; }
    public string Message { get; init; } = string.Empty;
}
