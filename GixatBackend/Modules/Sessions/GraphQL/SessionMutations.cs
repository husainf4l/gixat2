using GixatBackend.Data;
using GixatBackend.Modules.Sessions.Enums;
using GixatBackend.Modules.Sessions.Models;
using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Common.Services;
using GixatBackend.Modules.Common.Services.AWS;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace GixatBackend.Modules.Sessions.GraphQL;

[ExtendObjectType(OperationTypeNames.Mutation)]
[Authorize]
internal static class SessionMutations
{
    public static async Task<GarageSession> CreateSessionAsync(
        Guid carId,
        Guid customerId,
        string? intakeNotes,
        string? customerRequests,
        string? intakeRequests,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Check if there's an active session for this car
        var activeSession = await context.GarageSessions
            .Where(s => s.CarId == carId && 
                   s.Status != SessionStatus.Completed && 
                   s.Status != SessionStatus.Cancelled)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (activeSession != null)
        {
            throw new InvalidOperationException(
                $"Cannot create a new session. There is already an active session (ID: {activeSession.Id}) for this car with status: {activeSession.Status}");
        }

        // The global query filter will ensure we only find cars/customers in our organization
        var car = await context.Cars.FindAsync(carId).ConfigureAwait(false);
        var customer = await context.Customers.FindAsync(customerId).ConfigureAwait(false);

        if (car == null || customer == null)
        {
            throw new InvalidOperationException("Car or Customer not found in your organization");
        }

        var session = new GarageSession
        {
            CarId = carId,
            CustomerId = customerId,
            IntakeNotes = intakeNotes,
            CustomerRequests = customerRequests,
            IntakeRequests = intakeRequests,
            Status = SessionStatus.Intake
        };

        // No initial log needed - session starts at Intake status

        try
        {
            context.GarageSessions.Add(session);
            await context.SaveChangesAsync().ConfigureAwait(false);
            return session;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create session: {ex.Message}", ex);
        }
    }

    public static async Task<GarageSession> UpdateSessionStatusAsync(
        Guid sessionId,
        SessionStatus newStatus,
        string? notes,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var session = await context.GarageSessions
            .Include(s => s.Logs)
            .FirstOrDefaultAsync(s => s.Id == sessionId).ConfigureAwait(false);

        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        var oldStatus = session.Status;
        session.Status = newStatus;
        session.UpdatedAt = DateTime.UtcNow;

        session.Logs.Add(new SessionLog
        {
            FromStatus = oldStatus,
            ToStatus = newStatus,
            Notes = notes
        });

        await context.SaveChangesAsync().ConfigureAwait(false);
        return session;
    }

    public static async Task<GarageSession> UpdateIntakeAsync(
        Guid sessionId,
        string? intakeNotes,
        string? customerRequests,
        string? intakeRequests,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var session = await context.GarageSessions
            .Include(s => s.Logs)
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            .ConfigureAwait(false);
            
        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        var oldStatus = session.Status;
        session.IntakeNotes = intakeNotes;
        session.CustomerRequests = customerRequests;
        session.IntakeRequests = intakeRequests;
        session.UpdatedAt = DateTime.UtcNow;

        // Auto-transition to Inspection status after intake
        if (session.Status == SessionStatus.Intake)
        {
            session.Status = SessionStatus.Inspection;
            session.Logs.Add(new SessionLog
            {
                FromStatus = oldStatus,
                ToStatus = SessionStatus.Inspection,
                Notes = "Intake completed, moved to Inspection"
            });
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        return session;
    }

    public static async Task<GarageSession> UpdateCustomerRequestsAsync(
        Guid sessionId,
        string? customerRequests,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var session = await context.GarageSessions.FindAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        session.CustomerRequests = customerRequests;
        session.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync().ConfigureAwait(false);
        return session;
    }

    public static async Task<GarageSession> UpdateInspectionAsync(
        Guid sessionId,
        string? inspectionNotes,
        string? inspectionRequests,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var session = await context.GarageSessions
            .Include(s => s.Logs)
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            .ConfigureAwait(false);
            
        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        var oldStatus = session.Status;
        session.InspectionNotes = inspectionNotes;
        session.InspectionRequests = inspectionRequests;
        session.UpdatedAt = DateTime.UtcNow;

        // Auto-transition to TestDrive status after inspection
        if (session.Status == SessionStatus.Inspection)
        {
            session.Status = SessionStatus.TestDrive;
            session.Logs.Add(new SessionLog
            {
                FromStatus = oldStatus,
                ToStatus = SessionStatus.TestDrive,
                Notes = "Inspection completed, moved to Test Drive"
            });
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        return session;
    }

    public static async Task<GarageSession> UpdateTestDriveAsync(
        Guid sessionId,
        string? testDriveNotes,
        string? testDriveRequests,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var session = await context.GarageSessions.FindAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        session.TestDriveNotes = testDriveNotes;
        session.TestDriveRequests = testDriveRequests;
        session.UpdatedAt = DateTime.UtcNow;
        // Status stays as TestDrive - report generation will move to next status

        await context.SaveChangesAsync().ConfigureAwait(false);
        return session;
    }

    public static async Task<GarageSession> GenerateInitialReportAsync(
        Guid sessionId,
        string report,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Get current status before updating
        var currentStatus = await context.GarageSessions
            .Where(s => s.Id == sessionId)
            .Select(s => s.Status)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        // Use ExecuteUpdateAsync to avoid concurrency issues with triggers
        await context.GarageSessions
            .Where(s => s.Id == sessionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.InitialReport, report)
                .SetProperty(s => s.Status, SessionStatus.ReportGenerated)
                .SetProperty(s => s.UpdatedAt, DateTime.UtcNow))
            .ConfigureAwait(false);

        // Add log entry separately with actual previous status
        var log = new SessionLog
        {
            SessionId = sessionId,
            FromStatus = currentStatus,
            ToStatus = SessionStatus.ReportGenerated,
            Notes = "Initial report generated"
        };
        
        context.SessionLogs.Add(log);
        await context.SaveChangesAsync().ConfigureAwait(false);

        // Load and return the updated session
        var session = await context.GarageSessions
            .Include(s => s.Logs)
            .FirstAsync(s => s.Id == sessionId)
            .ConfigureAwait(false);
            
        return session;
    }

    public static async Task<SessionMedia> UploadMediaToSessionAsync(
        Guid sessionId,
        IFile file,
        SessionStage stage,
        string? alt,
        ApplicationDbContext context,
        [Service] IS3Service s3Service,
        [Service] IVirusScanService virusScanService)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(s3Service);
        ArgumentNullException.ThrowIfNull(virusScanService);

        var session = await context.GarageSessions.FindAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        // Validate file
        FileValidationService.ValidateFile(file);

        // Sanitize filename
        var sanitizedFileName = FileValidationService.SanitizeFileName(file.Name);

#pragma warning disable CA2000 // Dispose objects before losing scope
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
#pragma warning restore CA2000
        using var tempStorage = new TempFileStorageService(loggerFactory.CreateLogger<TempFileStorageService>());
        
        string? tempFilePath = null;
        try
        {
            // Step 1: Save to temporary storage
            using var uploadStream = file.OpenReadStream();
            tempFilePath = await tempStorage.SaveTempFileAsync(uploadStream, sanitizedFileName).ConfigureAwait(false);

            // Step 2: Scan for viruses
            using var scanStream = tempStorage.OpenTempFile(tempFilePath);
            var scanResult = await virusScanService.ScanFileAsync(scanStream, sanitizedFileName).ConfigureAwait(false);

            if (!scanResult.IsClean)
            {
                throw new InvalidOperationException(
                    $"File failed security scan: {scanResult.Message}" + 
                    (scanResult.ThreatName != null ? $" (Threat: {scanResult.ThreatName})" : ""));
            }

            // Step 3: Upload to S3
            using var s3Stream = tempStorage.OpenTempFile(tempFilePath);
            var fileKey = await s3Service.UploadFileAsync(s3Stream, sanitizedFileName, file.ContentType ?? "application/octet-stream").ConfigureAwait(false);
            var url = s3Service.GetFileUrl(fileKey);

            // Step 4: Create media record
            var media = new AppMedia
            {
                Url = url,
                Alt = alt,
                Type = (file.ContentType != null && file.ContentType.StartsWith("video", StringComparison.OrdinalIgnoreCase)) ? MediaType.Video : MediaType.Image
            };

            context.Medias.Add(media);

            // Link media to session
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
        finally
        {
            // Clean up temp file
            if (tempFilePath != null)
            {
                tempStorage.DeleteTempFile(tempFilePath);
            }
        }
    }

    /// <summary>
    /// Deletes a session media entry and its associated file from S3.
    /// </summary>
    /// <param name="mediaId">The AppMedia ID (the media file ID)</param>
    public static async Task<bool> DeleteSessionMediaAsync(
        Guid mediaId,
        ApplicationDbContext context,
        [Service] IS3Service s3Service)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(s3Service);

        // Query through GarageSessions to apply multi-tenancy filter, looking by MediaId
        var sessionMedia = await context.GarageSessions
            .SelectMany(s => s.Media)
            .Include(sm => sm.Media)
            .FirstOrDefaultAsync(sm => sm.MediaId == mediaId)
            .ConfigureAwait(false);

        if (sessionMedia == null)
        {
            throw new InvalidOperationException("Session media not found or access denied");
        }

        // Delete from S3 if URL exists - extract file key from URL
        if (sessionMedia.Media?.Url != null)
        {
            // Extract the file key from the S3 URL (everything after the last /)
            var fileKey = sessionMedia.Media.Url.AbsolutePath.TrimStart('/');
            if (!string.IsNullOrEmpty(fileKey))
            {
                await s3Service.DeleteFileAsync(fileKey).ConfigureAwait(false);
            }
        }

        // Delete from database
        context.SessionMedias.Remove(sessionMedia);
        if (sessionMedia.Media != null)
        {
            context.Medias.Remove(sessionMedia.Media);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }
}
