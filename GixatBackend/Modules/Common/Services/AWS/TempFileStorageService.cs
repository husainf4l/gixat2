using System.Security.Cryptography;
using System.Text;
using IOPath = System.IO.Path;
using Microsoft.Extensions.Logging;

using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Common.Services.AWS;

/// <summary>
/// Manages temporary file storage for scanning before S3 upload
/// </summary>
[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "Used by GraphQL mutations and public API")]
public sealed partial class TempFileStorageService : ITempFileStorageService, IDisposable
{
    private readonly string _tempDirectory;
    private readonly ILogger<TempFileStorageService> _logger;
    private bool _disposed;

    // High-performance logging using LoggerMessage source generator
    [LoggerMessage(Level = LogLevel.Information, Message = "Created temporary upload directory: {Directory}")]
    private partial void LogDirectoryCreated(string directory);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Saved temporary file: {FilePath}")]
    private partial void LogFileSaved(string filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Deleted temporary file: {FilePath}")]
    private partial void LogFileDeleted(string filePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to delete temporary file: {FilePath}")]
    private partial void LogDeleteFailed(Exception ex, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cleaned up temporary directory: {Directory}")]
    private partial void LogDirectoryCleanedUp(string directory);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error cleaning up temporary directory: {Directory}")]
    private partial void LogCleanupError(Exception ex, string directory);

    public TempFileStorageService(ILogger<TempFileStorageService> logger)
    {
        _logger = logger;
        _tempDirectory = IOPath.Combine(IOPath.GetTempPath(), "gixat-uploads", Guid.NewGuid().ToString());
        
        // Create temp directory
        Directory.CreateDirectory(_tempDirectory);
        LogDirectoryCreated(_tempDirectory);
    }

    /// <summary>
    /// Saves uploaded file to temporary storage
    /// </summary>
    public async Task<string> SaveTempFileAsync(Stream fileStream, string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        var safeFileName = GetSafeFileName(fileName);
        var tempFilePath = IOPath.Combine(_tempDirectory, safeFileName);

#pragma warning disable CA2007 // Do not use ConfigureAwait with await using
        await using var fileStreamWriter = File.Create(tempFilePath);
#pragma warning restore CA2007
        
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }
        
        await fileStream.CopyToAsync(fileStreamWriter).ConfigureAwait(false);
        await fileStreamWriter.FlushAsync().ConfigureAwait(false);

        LogFileSaved(tempFilePath);
        return tempFilePath;
    }

    /// <summary>
    /// Opens a temporary file for reading
    /// </summary>
    public FileStream OpenTempFile(string tempFilePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (!File.Exists(tempFilePath))
        {
            throw new FileNotFoundException("Temporary file not found", tempFilePath);
        }

        return File.OpenRead(tempFilePath);
    }

    /// <summary>
    /// Deletes a specific temporary file
    /// </summary>
    public void DeleteTempFile(string tempFilePath)
    {
        try
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
                LogFileDeleted(tempFilePath);
            }
        }
        catch (IOException ex)
        {
            LogDeleteFailed(ex, tempFilePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogDeleteFailed(ex, tempFilePath);
        }
    }

    private static string GetSafeFileName(string fileName)
    {
        // Generate hash of filename to avoid collisions
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fileName)))[..16];
        var extension = IOPath.GetExtension(fileName);
        return $"{hash}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            // Clean up all temporary files
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
                LogDirectoryCleanedUp(_tempDirectory);
            }
        }
        catch (IOException ex)
        {
            LogCleanupError(ex, _tempDirectory);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogCleanupError(ex, _tempDirectory);
        }

        _disposed = true;
    }
}
