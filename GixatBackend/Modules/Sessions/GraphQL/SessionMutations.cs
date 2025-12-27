using GixatBackend.Data;
using GixatBackend.Modules.Sessions.Enums;
using GixatBackend.Modules.Sessions.Models;
using GixatBackend.Modules.Sessions.Services;
using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Common.Services;
using GixatBackend.Modules.Common.Services.AWS;
using GixatBackend.Modules.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace GixatBackend.Modules.Sessions.GraphQL;

[ExtendObjectType(OperationTypeNames.Mutation)]
[Authorize]
internal sealed class SessionMutations
{
    public static async Task<GarageSession> CreateSessionAsync(
        Guid carId,
        Guid customerId,
        ApplicationDbContext context,
        [Service] ISessionService sessionService)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(sessionService);

        // Check if there's an active session for this car
        var activeSession = await context.GarageSessions
            .Where(s => s.CarId == carId && 
                   s.Status != SessionStatus.Completed && 
                   s.Status != SessionStatus.Cancelled)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        sessionService.ValidateNoActiveSession(activeSession);

        // The global query filter will ensure we only find cars/customers in our organization
        // Note: FindAsync bypasses query filters, so we must use FirstOrDefaultAsync
        var car = await context.Cars
            .FirstOrDefaultAsync(c => c.Id == carId)
            .ConfigureAwait(false);
        var customer = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId)
            .ConfigureAwait(false);

        if (car == null)
        {
            throw new EntityNotFoundException("Car", carId);
        }

        if (customer == null)
        {
            throw new EntityNotFoundException("Customer", customerId);
        }

        var session = new GarageSession
        {
            CarId = carId,
            CustomerId = customerId,
            Status = SessionStatus.CustomerRequest
        };

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
        ApplicationDbContext context,
        [Service] ISessionService sessionService)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(sessionService);

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

        session.Logs.Add(sessionService.CreateStatusLog(oldStatus, newStatus, notes));

        await context.SaveChangesAsync().ConfigureAwait(false);
        return session;
    }

    public static async Task<GarageSession> UpdateCustomerRequestsAsync(
        Guid sessionId,
        string? customerRequests,
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

        if (session.Status != SessionStatus.CustomerRequest)
        {
            throw new InvalidOperationException("Can only update customer requests for sessions in CustomerRequest status");
        }

        var oldStatus = session.Status;
        session.CustomerRequests = customerRequests;
        session.UpdatedAt = DateTime.UtcNow;

        // Auto-transition to Inspection status after customer requests
        session.Status = SessionStatus.Inspection;
        session.Logs.Add(new SessionLog
        {
            FromStatus = oldStatus,
            ToStatus = SessionStatus.Inspection,
            Notes = "Customer requests recorded, moved to Inspection"
        });

        await context.SaveChangesAsync().ConfigureAwait(false);
        return session;
    }

    public static async Task<GarageSession> UpdateInspectionAsync(
        Guid sessionId,
        int mileage,
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

        if (session.Status != SessionStatus.Inspection)
        {
            throw new InvalidOperationException("Can only update inspection for sessions in Inspection status");
        }

        var oldStatus = session.Status;
        session.Mileage = mileage;
        session.InspectionNotes = inspectionNotes;
        session.InspectionRequests = inspectionRequests;
        session.UpdatedAt = DateTime.UtcNow;

        // Auto-transition to TestDrive status after inspection
        session.Status = SessionStatus.TestDrive;
        session.Logs.Add(new SessionLog
        {
            FromStatus = oldStatus,
            ToStatus = SessionStatus.TestDrive,
            Notes = $"Inspection completed with mileage {mileage}, moved to Test Drive"
        });

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
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Load session with all related data
        var session = await context.GarageSessions
            .Include(s => s.Car)
            .Include(s => s.Customer)
            .Include(s => s.Media)
                .ThenInclude(sm => sm.Media)
            .Include(s => s.Logs)
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            .ConfigureAwait(false);

        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        if (session.Status != SessionStatus.TestDrive)
        {
            throw new InvalidOperationException("Can only generate report for sessions in TestDrive status");
        }

        // Build comprehensive report
        var reportBuilder = new System.Text.StringBuilder();
        
        reportBuilder.AppendLine("=== VEHICLE INSPECTION REPORT ===\n");
        
        // Vehicle Information
        reportBuilder.AppendLine("VEHICLE INFORMATION:");
        reportBuilder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Make: {session.Car?.Make}");
        reportBuilder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Model: {session.Car?.Model}");
        reportBuilder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Year: {session.Car?.Year}");
        reportBuilder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"License Plate: {session.Car?.LicensePlate}");
        if (session.Mileage.HasValue)
        {
            reportBuilder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Mileage: {session.Mileage.Value:N0} km");
        }
        reportBuilder.AppendLine();

        // Customer Information
        reportBuilder.AppendLine("CUSTOMER INFORMATION:");
        reportBuilder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Name: {session.Customer?.FirstName} {session.Customer?.LastName}");
        reportBuilder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Phone: {session.Customer?.PhoneNumber}");
        reportBuilder.AppendLine();

        // Customer Requests
        if (!string.IsNullOrEmpty(session.CustomerRequests))
        {
            reportBuilder.AppendLine("CUSTOMER REQUESTS:");
            reportBuilder.AppendLine(session.CustomerRequests);
            reportBuilder.AppendLine();
        }

        // Inspection Findings
        if (!string.IsNullOrEmpty(session.InspectionNotes) || !string.IsNullOrEmpty(session.InspectionRequests))
        {
            reportBuilder.AppendLine("INSPECTION FINDINGS:");
            if (!string.IsNullOrEmpty(session.InspectionNotes))
            {
                reportBuilder.AppendLine("Notes:");
                reportBuilder.AppendLine(session.InspectionNotes);
            }
            if (!string.IsNullOrEmpty(session.InspectionRequests))
            {
                reportBuilder.AppendLine("Recommendations:");
                reportBuilder.AppendLine(session.InspectionRequests);
            }
            reportBuilder.AppendLine();
        }

        // Test Drive Results
        if (!string.IsNullOrEmpty(session.TestDriveNotes) || !string.IsNullOrEmpty(session.TestDriveRequests))
        {
            reportBuilder.AppendLine("TEST DRIVE RESULTS:");
            if (!string.IsNullOrEmpty(session.TestDriveNotes))
            {
                reportBuilder.AppendLine("Notes:");
                reportBuilder.AppendLine(session.TestDriveNotes);
            }
            if (!string.IsNullOrEmpty(session.TestDriveRequests))
            {
                reportBuilder.AppendLine("Recommendations:");
                reportBuilder.AppendLine(session.TestDriveRequests);
            }
            reportBuilder.AppendLine();
        }

        // Media/Images
        var mediaByStage = session.Media
            .GroupBy(m => m.Stage)
            .OrderBy(g => g.Key);

        if (session.Media.Count > 0)
        {
            reportBuilder.AppendLine("DOCUMENTATION (Images/Videos):");
            foreach (var stageGroup in mediaByStage)
            {
                reportBuilder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"\n{stageGroup.Key} Phase:");
                foreach (var media in stageGroup)
                {
                    if (media.Media != null)
                    {
                        reportBuilder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"  - {media.Media.Type}: {media.Media.Url}");
                        if (!string.IsNullOrEmpty(media.Media.Alt))
                        {
                            reportBuilder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    Description: {media.Media.Alt}");
                        }
                    }
                }
            }
            reportBuilder.AppendLine();
        }

        reportBuilder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"\nReport Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

        var oldStatus = session.Status;
        session.InitialReport = reportBuilder.ToString();
        session.Status = SessionStatus.ReportGenerated;
        session.UpdatedAt = DateTime.UtcNow;

        session.Logs.Add(new SessionLog
        {
            FromStatus = oldStatus,
            ToStatus = SessionStatus.ReportGenerated,
            Notes = "Initial report generated with all session data"
        });

        await context.SaveChangesAsync().ConfigureAwait(false);
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
