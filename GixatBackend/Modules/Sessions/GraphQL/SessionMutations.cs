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

        var log = new SessionLog
        {
            Session = session,
            FromStatus = SessionStatus.Intake,
            ToStatus = SessionStatus.Intake,
            Notes = "Session started"
        };
        
        session.Logs.Add(log);

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

        var session = await context.GarageSessions.FindAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        session.IntakeNotes = intakeNotes;
        session.CustomerRequests = customerRequests;
        session.IntakeRequests = intakeRequests;
        session.UpdatedAt = DateTime.UtcNow;

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

        var session = await context.GarageSessions.FindAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        session.InspectionNotes = inspectionNotes;
        session.InspectionRequests = inspectionRequests;
        session.UpdatedAt = DateTime.UtcNow;

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

        await context.SaveChangesAsync().ConfigureAwait(false);
        return session;
    }

    public static async Task<GarageSession> GenerateInitialReportAsync(
        Guid sessionId,
        string report,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var session = await context.GarageSessions.FindAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        session.InitialReport = report;
        session.Status = SessionStatus.ReportGenerated;
        session.UpdatedAt = DateTime.UtcNow;

        session.Logs.Add(new SessionLog
        {
            FromStatus = SessionStatus.TestDrive, // Assuming coming from test drive
            ToStatus = SessionStatus.ReportGenerated,
            Notes = "Initial report generated"
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
}
