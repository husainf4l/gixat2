using HotChocolate.Types;
using System.Globalization;
using IOPath = System.IO.Path;

namespace GixatBackend.Modules.Common.Services.AWS;

internal sealed class FileValidationService
{
    private static readonly char[] InvalidChars = IOPath.GetInvalidFileNameChars().Concat(new[] { '<', '>', ':', '|', '?', '*' }).ToArray();
    
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg"
    };

    private static readonly HashSet<string> AllowedVideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm", ".mov", ".avi", ".mkv", ".m4v"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/bmp", "image/svg+xml",
        // Videos
        "video/mp4", "video/webm", "video/quicktime", "video/x-msvideo", "video/x-matroska", "video/x-m4v"
    };

    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB
    private const long MaxImageSizeBytes = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Validates file metadata for presigned URL generation (without file content)
    /// </summary>
    public static void ValidateFileMetadata(string fileName, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("File name is required");
        }

        // Sanitize and validate file name
        var cleanFileName = IOPath.GetFileName(fileName);
        if (cleanFileName != fileName || 
            fileName.Contains("..", StringComparison.Ordinal) || 
            IOPath.IsPathRooted(fileName) ||
            fileName.Contains('\\', StringComparison.Ordinal) || 
            fileName.Contains('/', StringComparison.Ordinal) ||
            fileName.Contains(':', StringComparison.Ordinal) ||
            fileName.Contains('*', StringComparison.Ordinal) ||
            fileName.Contains('?', StringComparison.Ordinal) ||
            fileName.Contains('"', StringComparison.Ordinal) ||
            fileName.Contains('<', StringComparison.Ordinal) ||
            fileName.Contains('>', StringComparison.Ordinal) ||
            fileName.Contains('|', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid file name");
        }

        // Validate extension
        var extension = IOPath.GetExtension(cleanFileName).ToUpperInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            throw new InvalidOperationException("File must have an extension");
        }

        if (!AllowedImageExtensions.Contains(extension) && !AllowedVideoExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                $"File type '{extension}' is not allowed. Allowed types: images (.jpg, .png, .gif, .webp) and videos (.mp4, .webm, .mov)");
        }

        // Validate content type
        if (!string.IsNullOrEmpty(contentType) && !AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException($"Content type '{contentType}' is not allowed");
        }

        // Validate content type matches extension
        ValidateContentTypeMatchesExtension(extension, contentType);
    }

    public static void ValidateFile(IFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        // Validate file name
        if (string.IsNullOrWhiteSpace(file.Name))
        {
            throw new InvalidOperationException("File name is required");
        }

        // Sanitize and validate file name
        var fileName = IOPath.GetFileName(file.Name);
        if (fileName != file.Name || 
            fileName.Contains("..", StringComparison.Ordinal) || 
            IOPath.IsPathRooted(fileName) ||
            fileName.Contains('\\', StringComparison.Ordinal) || 
            fileName.Contains('/', StringComparison.Ordinal) ||
            fileName.Contains(':', StringComparison.Ordinal) ||
            fileName.Contains('*', StringComparison.Ordinal) ||
            fileName.Contains('?', StringComparison.Ordinal) ||
            fileName.Contains('"', StringComparison.Ordinal) ||
            fileName.Contains('<', StringComparison.Ordinal) ||
            fileName.Contains('>', StringComparison.Ordinal) ||
            fileName.Contains('|', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid file name");
        }

        // Validate extension
        var extension = IOPath.GetExtension(fileName).ToUpperInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            throw new InvalidOperationException("File must have an extension");
        }

        if (!AllowedImageExtensions.Contains(extension) && !AllowedVideoExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                $"File type '{extension}' is not allowed. Allowed types: images (.jpg, .png, .gif, .webp) and videos (.mp4, .webm, .mov)");
        }

        // Validate content type
        if (!string.IsNullOrEmpty(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException($"Content type '{file.ContentType}' is not allowed");
        }

        // Validate content type matches extension
        ValidateContentTypeMatchesExtension(extension, file.ContentType);

        // Validate file size
        var fileLength = file.Length;
        if (fileLength <= 0)
        {
            throw new InvalidOperationException("File is empty");
        }

        var isImage = AllowedImageExtensions.Contains(extension);
        var maxSize = isImage ? MaxImageSizeBytes : MaxFileSizeBytes;
        
        if (fileLength > maxSize)
        {
            var maxSizeMB = maxSize / (1024 * 1024);
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {maxSizeMB}MB");
        }
    }

    private static void ValidateContentTypeMatchesExtension(string extension, string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return;
        }

        var isImageExtension = AllowedImageExtensions.Contains(extension);
        var isVideoExtension = AllowedVideoExtensions.Contains(extension);
        var isImageContentType = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        var isVideoContentType = contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

        if ((isImageExtension && !isImageContentType) || (isVideoExtension && !isVideoContentType))
        {
            throw new InvalidOperationException("File extension does not match content type");
        }
    }

    public static string SanitizeFileName(string fileName)
    {
        // Remove any path characters - handle both Windows (\) and Unix (/) separators
        var lastBackslash = fileName.LastIndexOf('\\');
        var lastForwardSlash = fileName.LastIndexOf('/');
        var lastSeparator = Math.Max(lastBackslash, lastForwardSlash);
        if (lastSeparator >= 0)
        {
            fileName = fileName.Substring(lastSeparator + 1);
        }

        // Replace invalid characters with underscores
        foreach (var invalidChar in InvalidChars)
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        // Add timestamp to prevent collisions
        var extension = IOPath.GetExtension(fileName);
        var nameWithoutExtension = IOPath.GetFileNameWithoutExtension(fileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        return $"{nameWithoutExtension}_{timestamp}{extension}";
    }
}
