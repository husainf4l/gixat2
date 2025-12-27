using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Common.Services;
using GixatBackend.Modules.Common.Services.AWS;
using GixatBackend.Modules.Common.Services.Tenant;
using GixatBackend.Data;
using Microsoft.EntityFrameworkCore;
using GixatBackend.Modules.Sessions.Models;
using GixatBackend.Modules.Sessions.Enums;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Common.GraphQL;

[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "GraphQL mutation type")]
[HotChocolate.Types.ExtendObjectType("Mutation")]
[Authorize]
internal sealed partial class PresignedUploadMutations
{
    // High-performance logging using LoggerMessage source generator
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to process session file {FileKey}")]
    private static partial void LogProcessingError(ILogger logger, Exception ex, string fileKey);
    /// <summary>
    /// Step 1: Get presigned URLs for session media upload (supports bulk uploads)
    /// Frontend uploads files directly to S3 (super fast, no backend involved)
    /// Organizes files by session: organizations/{orgId}/sessions/{sessionId}/{stage}/{date}/{file}
    /// </summary>
    public static async Task<List<PresignedUploadUrl>> GetPresignedUploadUrlAsync(
        Guid sessionId,
        SessionStage stage,
        IReadOnlyList<SessionFileUploadRequest> files,
        [Service] IS3Service s3Service,
        [Service] ITenantService tenantService,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(s3Service);
        ArgumentNullException.ThrowIfNull(tenantService);
        ArgumentNullException.ThrowIfNull(context);

        if (files.Count == 0)
        {
            throw new ArgumentException("At least one file must be provided.", nameof(files));
        }

        if (files.Count > 50)
        {
            throw new ArgumentException("Cannot upload more than 50 files at once.", nameof(files));
        }

        // Verify session exists and belongs to the organization
        var session = await context.GarageSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            .ConfigureAwait(false);

        if (session == null)
        {
            throw new InvalidOperationException($"Session with ID {sessionId} not found.");
        }

        var orgId = tenantService.OrganizationId ?? throw new InvalidOperationException("Organization context required");
        var now = DateTime.UtcNow;
        var stageName = stage.ToString().ToUpperInvariant();

        // Generate presigned URLs in parallel for all files
        var tasks = files.Select(async file =>
        {
            // Validate file metadata
            FileValidationService.ValidateFileMetadata(file.FileName, file.ContentType);

            // Sanitize filename
            var sanitizedFileName = FileValidationService.SanitizeFileName(file.FileName);

            // Organized folder structure: organizations/{orgId}/sessions/{sessionId}/{stage}/{year}/{month}/{guid}_{filename}
            var fileKey = $"organizations/{orgId}/sessions/{sessionId}/{stageName}/{now:yyyy}/{now:MM}/{Guid.NewGuid()}_{sanitizedFileName}";
            var presignedUrl = await s3Service.GeneratePresignedUploadUrlAsync(fileKey, file.ContentType, expiresInMinutes: 15).ConfigureAwait(false);

            return new PresignedUploadUrl
            {
                UploadUrl = new Uri(presignedUrl),
                FileKey = fileKey,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };
        });

        return (await Task.WhenAll(tasks).ConfigureAwait(false)).ToList();
    }

    /// <summary>
    /// Step 2: After frontend uploads to S3, call this to process the file
    /// Backend downloads, scans, compresses, re-uploads, and creates DB record
    /// </summary>
    public static async Task<AppMedia> ProcessUploadedFileAsync(
        string fileKey,
        string? alt,
        [Service] IS3Service s3Service,
        [Service] IVirusScanService virusScanService,
        [Service] IImageCompressionService compressionService,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(fileKey);
        ArgumentNullException.ThrowIfNull(s3Service);
        ArgumentNullException.ThrowIfNull(virusScanService);
        ArgumentNullException.ThrowIfNull(compressionService);
        ArgumentNullException.ThrowIfNull(context);

#pragma warning disable CA2000 // Dispose objects before losing scope
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
#pragma warning restore CA2000
        using var tempStorage = new TempFileStorageService(loggerFactory.CreateLogger<TempFileStorageService>());

        string? downloadedPath = null;
        string? compressedPath = null;

        try
        {
            // Step 1: Download from S3 to temp storage
            downloadedPath = await s3Service.DownloadToTempAsync(fileKey, tempStorage).ConfigureAwait(false);

            // Step 2: Scan for viruses
            using var scanStream = tempStorage.OpenTempFile(downloadedPath);
            var scanResult = await virusScanService.ScanFileAsync(scanStream, fileKey).ConfigureAwait(false);

            if (!scanResult.IsClean)
            {
                // Delete infected file from S3
                await s3Service.DeleteFileAsync(fileKey).ConfigureAwait(false);
                
                throw new InvalidOperationException(
                    $"File failed security scan and has been deleted: {scanResult.Message}" +
                    (scanResult.ThreatName != null ? $" (Threat: {scanResult.ThreatName})" : ""));
            }

            // Step 3: Determine media type
            var extension = System.IO.Path.GetExtension(fileKey).ToUpperInvariant();
            var isVideo = new[] { ".MP4", ".WEBM", ".MOV", ".AVI", ".MKV", ".M4V" }.Contains(extension);
            var mediaType = isVideo ? MediaType.Video : MediaType.Image;

            // Step 4: Compress the file
            if (isVideo)
            {
                // Video compression (FFmpeg)
                compressedPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(downloadedPath)!,
                    $"compressed_{System.IO.Path.GetFileName(downloadedPath)}");
                
                await compressionService.CompressVideoAsync(downloadedPath, compressedPath, crf: 28).ConfigureAwait(false);
            }
            else
            {
                // Image compression (ImageSharp)
                compressedPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(downloadedPath)!,
                    $"compressed_{System.IO.Path.GetFileName(downloadedPath)}");
                
                using var imageStream = tempStorage.OpenTempFile(downloadedPath);
                await compressionService.CompressImageAsync(
                    imageStream, 
                    compressedPath, 
                    quality: 85, 
                    maxWidth: 2048, 
                    maxHeight: 2048).ConfigureAwait(false);
            }

            // Step 5: Upload compressed file back to S3 (replace original)
            var contentType = GetContentType(extension);
            using var compressedStream = File.OpenRead(compressedPath);
            var finalKey = await s3Service.UploadFileAsync(compressedStream, System.IO.Path.GetFileName(fileKey), contentType).ConfigureAwait(false);

            // Step 6: Delete original uncompressed file from S3
            if (finalKey != fileKey)
            {
                await s3Service.DeleteFileAsync(fileKey).ConfigureAwait(false);
            }

            // Step 7: Create database record
            var media = new AppMedia
            {
                Url = s3Service.GetFileUrl(finalKey),
                Alt = alt,
                Type = mediaType
            };

            context.Medias.Add(media);
            await context.SaveChangesAsync().ConfigureAwait(false);

            return media;
        }
        finally
        {
            // Cleanup temp files
            if (downloadedPath != null)
            {
                tempStorage.DeleteTempFile(downloadedPath);
            }
            if (compressedPath != null && File.Exists(compressedPath))
            {
                tempStorage.DeleteTempFile(compressedPath);
            }
        }
    }

    /// <summary>
    /// Step 2 (Session-specific): Process uploaded file and link to session
    /// </summary>
    public static async Task<SessionMedia> ProcessSessionUploadAsync(
        Guid sessionId,
        string fileKey,
        SessionStage stage,
        string? alt,
        [Service] IS3Service s3Service,
        [Service] IVirusScanService virusScanService,
        [Service] IImageCompressionService compressionService,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(fileKey);
        ArgumentNullException.ThrowIfNull(s3Service);
        ArgumentNullException.ThrowIfNull(virusScanService);
        ArgumentNullException.ThrowIfNull(compressionService);
        ArgumentNullException.ThrowIfNull(context);

        // Verify session exists
        var session = await context.GarageSessions.FindAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Session with ID {sessionId} not found.");
        }

        // Process the file (scan + compress)
        var media = await ProcessUploadedFileAsync(fileKey, alt, s3Service, virusScanService, compressionService, context).ConfigureAwait(false);

        // Link to session
        var sessionMedia = new SessionMedia
        {
            SessionId = sessionId,
            Media = media,
            Stage = stage
        };

        context.SessionMedias.Add(sessionMedia);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return sessionMedia;
    }

    /// <summary>
    /// Step 3 (Bulk): Process multiple uploaded session files at once
    /// </summary>
    public static async Task<List<BulkSessionUploadResult>> ProcessBulkSessionUploadsAsync(
        Guid sessionId,
        IReadOnlyList<BulkSessionFileUploadRequest> files,
        [Service] IS3Service s3Service,
        [Service] IVirusScanService virusScanService,
        [Service] IImageCompressionService compressionService,
        [Service] ApplicationDbContext context,
        [Service] ILogger<PresignedUploadUrl> logger)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(s3Service);
        ArgumentNullException.ThrowIfNull(virusScanService);
        ArgumentNullException.ThrowIfNull(compressionService);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        if (files.Count == 0)
        {
            throw new ArgumentException("At least one file must be provided.", nameof(files));
        }

        if (files.Count > 50)
        {
            throw new ArgumentException("Cannot process more than 50 files at once.", nameof(files));
        }

        // Verify session exists once
        var session = await context.GarageSessions.FindAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Session with ID {sessionId} not found.");
        }

        var results = new List<BulkSessionUploadResult>();

        // Process files in parallel
        var tasks = files.Select(async file =>
        {
            try
            {
                var sessionMedia = await ProcessSessionUploadAsync(
                    sessionId,
                    file.FileKey,
                    file.Stage,
                    file.Alt,
                    s3Service,
                    virusScanService,
                    compressionService,
                    context).ConfigureAwait(false);

                return new BulkSessionUploadResult
                {
                    FileKey = file.FileKey,
                    Success = true,
                    SessionMedia = sessionMedia
                };
            }
            // Intentionally catching all exceptions for graceful bulk operation handling
            // Individual file failures should not stop the entire batch
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogProcessingError(logger, ex, file.FileKey);
                
                return new BulkSessionUploadResult
                {
                    FileKey = file.FileKey,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        });

        results = (await Task.WhenAll(tasks).ConfigureAwait(false)).ToList();
        return results;
    }

    private static string GetContentType(string extension)
    {
        return extension.ToUpperInvariant() switch
        {
            ".JPG" or ".JPEG" => "image/jpeg",
            ".PNG" => "image/png",
            ".GIF" => "image/gif",
            ".WEBP" => "image/webp",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".m4v" => "video/x-m4v",
            _ => "application/octet-stream"
        };
    }
}

[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "GraphQL type")]
public class PresignedUploadUrl
{
    public Uri UploadUrl { get; set; } = new Uri("about:blank");
    public string FileKey { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "GraphQL input type")]
public class SessionFileUploadRequest
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "GraphQL input type")]
public class BulkSessionFileUploadRequest
{
    public string FileKey { get; set; } = string.Empty;
    public SessionStage Stage { get; set; }
    public string? Alt { get; set; }
}

[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "GraphQL type")]
public class BulkSessionUploadResult
{
    public string FileKey { get; set; } = string.Empty;
    public bool Success { get; set; }
    public SessionMedia? SessionMedia { get; set; }
    public string? ErrorMessage { get; set; }
}
