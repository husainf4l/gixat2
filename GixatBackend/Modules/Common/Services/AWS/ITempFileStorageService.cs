using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Common.Services.AWS;

/// <summary>
/// Interface for temporary file storage operations
/// </summary>
[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "Used by S3Service and dependency injection")]
public interface ITempFileStorageService
{
    /// <summary>
    /// Saves uploaded file to temporary storage
    /// </summary>
    /// <param name="fileStream">The file stream to save</param>
    /// <param name="fileName">The original file name</param>
    /// <returns>Path to the temporary file</returns>
    Task<string> SaveTempFileAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Opens a temporary file for reading
    /// </summary>
    /// <param name="tempFilePath">Path to the temporary file</param>
    /// <returns>FileStream for reading</returns>
    FileStream OpenTempFile(string tempFilePath);

    /// <summary>
    /// Deletes a specific temporary file
    /// </summary>
    /// <param name="tempFilePath">Path to the temporary file to delete</param>
    void DeleteTempFile(string tempFilePath);
}