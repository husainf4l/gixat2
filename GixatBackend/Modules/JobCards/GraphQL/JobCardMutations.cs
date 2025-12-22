using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Modules.JobCards.Services;
using GixatBackend.Modules.Sessions.Enums;
using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Common.Services;
using GixatBackend.Modules.Common.Services.AWS;
using GixatBackend.Modules.Common.Constants;
using GixatBackend.Modules.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;
using Microsoft.Extensions.Logging;

namespace GixatBackend.Modules.JobCards.GraphQL;

[ExtendObjectType(OperationTypeNames.Mutation)]
[Authorize]
internal static class JobCardMutations
{
    public static async Task<JobCard> CreateJobCardFromSessionAsync(
        Guid sessionId,
        ApplicationDbContext context,
        [Service] IJobCardService jobCardService)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(jobCardService);

        var session = await context.GarageSessions
            .Include(s => s.Car)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == sessionId).ConfigureAwait(false);

        if (session == null)
        {
            throw new EntityNotFoundException("Session", sessionId);
        }

        jobCardService.ValidateSessionForJobCard(session);

        var jobCard = new JobCard
        {
            SessionId = session.Id,
            CarId = session.CarId,
            CustomerId = session.CustomerId,
            OrganizationId = session.OrganizationId,
            Status = JobCardStatus.Pending,
            InternalNotes = jobCardService.BuildInternalNotesFromSession(session)
        };

        // Auto-create job items from requests
        var jobItems = jobCardService.ExtractJobItemsFromSession(session);
        foreach (var item in jobItems)
        {
            jobCard.Items.Add(item);
        }

        session.Status = SessionStatus.JobCardCreated;
        
        context.JobCards.Add(jobCard);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return jobCard;
    }

    public static async Task<JobCard> AddJobItemAsync(
        Guid jobCardId,
        string description,
        decimal estimatedLaborCost,
        decimal estimatedPartsCost,
        string? assignedTechnicianId,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var jobCard = await context.JobCards
            .Include(j => j.Items)
            .FirstOrDefaultAsync(j => j.Id == jobCardId).ConfigureAwait(false);

        if (jobCard == null)
        {
            throw new InvalidOperationException("Job Card not found");
        }

        var item = new JobItem
        {
            JobCardId = jobCardId,
            Description = description,
            EstimatedLaborCost = estimatedLaborCost,
            EstimatedPartsCost = estimatedPartsCost,
            AssignedTechnicianId = assignedTechnicianId,
            Status = JobItemStatus.Pending
        };

        jobCard.Items.Add(item);
        jobCard.TotalEstimatedLabor += estimatedLaborCost;
        jobCard.TotalEstimatedParts += estimatedPartsCost;
        jobCard.TotalEstimatedCost += item.EstimatedCost;
        jobCard.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync().ConfigureAwait(false);
        return jobCard;
    }

    public static async Task<JobItem> UpdateJobItemStatusAsync(
        Guid itemId,
        JobItemStatus status,
        decimal actualLaborCost,
        decimal actualPartsCost,
        string? technicianNotes,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var item = await context.JobItems
            .Include(i => i.JobCard)
            .FirstOrDefaultAsync(i => i.Id == itemId).ConfigureAwait(false);

        if (item == null)
        {
            throw new InvalidOperationException("Job Item not found");
        }

        // Validation: Can't complete without costs
        if (status == JobItemStatus.Completed && actualLaborCost == 0 && actualPartsCost == 0)
        {
            throw new InvalidOperationException("Cannot mark item as completed without actual costs");
        }

        // Validation: Must be approved before starting work
        if (status == JobItemStatus.InProgress && !item.IsApprovedByCustomer)
        {
            throw new InvalidOperationException("Cannot start work on unapproved item");
        }

        item.Status = status;
        item.ActualLaborCost = actualLaborCost;
        item.ActualPartsCost = actualPartsCost;
        item.TechnicianNotes = technicianNotes;
        item.UpdatedAt = DateTime.UtcNow;

        if (item.JobCard != null)
        {
            // Recalculate totals
            var items = await context.JobItems
                .Where(i => i.JobCardId == item.JobCardId)
                .ToListAsync().ConfigureAwait(false);
            
            item.JobCard.TotalActualLabor = items.Sum(i => i.ActualLaborCost);
            item.JobCard.TotalActualParts = items.Sum(i => i.ActualPartsCost);
            item.JobCard.TotalActualCost = item.JobCard.TotalActualLabor + item.JobCard.TotalActualParts;
            item.JobCard.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        return item;
    }

    public static async Task<JobCard> UpdateJobCardStatusAsync(
        Guid jobCardId,
        JobCardStatus status,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var jobCard = await context.JobCards
            .Include(j => j.Items)
            .FirstOrDefaultAsync(j => j.Id == jobCardId).ConfigureAwait(false);
            
        if (jobCard == null)
        {
            throw new InvalidOperationException("Job Card not found");
        }

        // Validation: Can't complete if there are pending/in-progress items
        if (status == JobCardStatus.Completed)
        {
            var incompleteItems = jobCard.Items.Where(i => 
                i.Status == JobItemStatus.Pending || 
                i.Status == JobItemStatus.InProgress).ToList();
            
            if (incompleteItems.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Cannot complete job card. {incompleteItems.Count} item(s) are still pending or in progress.");
            }
        }

        jobCard.Status = status;
        jobCard.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync().ConfigureAwait(false);
        return jobCard;
    }

    // Assign technician to job card
    public static async Task<JobCard> AssignTechnicianToJobCardAsync(
        Guid jobCardId,
        string technicianId,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var jobCard = await context.JobCards.FindAsync(jobCardId).ConfigureAwait(false);
        if (jobCard == null)
        {
            throw new InvalidOperationException("Job Card not found");
        }

        jobCard.AssignedTechnicianId = technicianId;
        jobCard.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync().ConfigureAwait(false);
        return jobCard;
    }

    // Assign technician to specific job item
    public static async Task<JobItem> AssignTechnicianToJobItemAsync(
        Guid itemId,
        string technicianId,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var item = await context.JobItems.FindAsync(itemId).ConfigureAwait(false);
        if (item == null)
        {
            throw new InvalidOperationException("Job Item not found");
        }

        item.AssignedTechnicianId = technicianId;
        item.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync().ConfigureAwait(false);
        return item;
    }

    // Customer approval for job card
    public static async Task<JobCard> ApproveJobCardAsync(
        Guid jobCardId,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var jobCard = await context.JobCards
            .Include(j => j.Items)
            .FirstOrDefaultAsync(j => j.Id == jobCardId).ConfigureAwait(false);
            
        if (jobCard == null)
        {
            throw new InvalidOperationException("Job Card not found");
        }

        jobCard.IsApprovedByCustomer = true;
        jobCard.ApprovedAt = DateTime.UtcNow;
        jobCard.UpdatedAt = DateTime.UtcNow;

        // Auto-approve all items
        foreach (var item in jobCard.Items)
        {
            item.IsApprovedByCustomer = true;
            item.ApprovedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        return jobCard;
    }

    // Customer approval for specific job item
    public static async Task<JobItem> ApproveJobItemAsync(
        Guid itemId,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var item = await context.JobItems.FindAsync(itemId).ConfigureAwait(false);
        if (item == null)
        {
            throw new InvalidOperationException("Job Item not found");
        }

        item.IsApprovedByCustomer = true;
        item.ApprovedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync().ConfigureAwait(false);
        return item;
    }

    // Upload media to job card
    public static async Task<JobCardMedia> UploadMediaToJobCardAsync(
        Guid jobCardId,
        IFile file,
        JobCardMediaType type,
        string? alt,
        ApplicationDbContext context,
        [Service] IS3Service s3Service,
        [Service] IVirusScanService virusScanService)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(s3Service);
        ArgumentNullException.ThrowIfNull(virusScanService);

        var jobCard = await context.JobCards.FindAsync(jobCardId).ConfigureAwait(false);
        if (jobCard == null)
        {
            throw new InvalidOperationException("Job Card not found");
        }

        FileValidationService.ValidateFile(file);
        var sanitizedFileName = FileValidationService.SanitizeFileName(file.Name);

#pragma warning disable CA2000
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
#pragma warning restore CA2000
        using var tempStorage = new TempFileStorageService(loggerFactory.CreateLogger<TempFileStorageService>());
        
        string? tempFilePath = null;
        try
        {
            using var uploadStream = file.OpenReadStream();
            tempFilePath = await tempStorage.SaveTempFileAsync(uploadStream, sanitizedFileName).ConfigureAwait(false);

            using var scanStream = tempStorage.OpenTempFile(tempFilePath);
            var scanResult = await virusScanService.ScanFileAsync(scanStream, sanitizedFileName).ConfigureAwait(false);

            if (!scanResult.IsClean)
            {
                throw new InvalidOperationException(
                    $"File failed security scan: {scanResult.Message}" + 
                    (scanResult.ThreatName != null ? $" (Threat: {scanResult.ThreatName})" : ""));
            }

            using var s3Stream = tempStorage.OpenTempFile(tempFilePath);
            var fileKey = await s3Service.UploadFileAsync(s3Stream, sanitizedFileName, file.ContentType ?? "application/octet-stream").ConfigureAwait(false);
            var url = s3Service.GetFileUrl(fileKey);

            var media = new AppMedia
            {
                Url = url,
                Alt = alt,
                Type = (file.ContentType != null && file.ContentType.StartsWith("video", StringComparison.OrdinalIgnoreCase)) ? MediaType.Video : MediaType.Image
            };

            context.Medias.Add(media);

            var jobCardMedia = new JobCardMedia
            {
                JobCardId = jobCardId,
                Media = media,
                Type = type
            };

            context.JobCardMedias.Add(jobCardMedia);
            await context.SaveChangesAsync().ConfigureAwait(false);

            return jobCardMedia;
        }
        finally
        {
            if (tempFilePath != null)
            {
                tempStorage.DeleteTempFile(tempFilePath);
            }
        }
    }

    // Upload media to job item
    public static async Task<JobItemMedia> UploadMediaToJobItemAsync(
        Guid itemId,
        IFile file,
        JobCardMediaType type,
        string? alt,
        ApplicationDbContext context,
        [Service] IS3Service s3Service,
        [Service] IVirusScanService virusScanService)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(s3Service);
        ArgumentNullException.ThrowIfNull(virusScanService);

        var jobItem = await context.JobItems.FindAsync(itemId).ConfigureAwait(false);
        if (jobItem == null)
        {
            throw new InvalidOperationException("Job Item not found");
        }

        FileValidationService.ValidateFile(file);
        var sanitizedFileName = FileValidationService.SanitizeFileName(file.Name);

#pragma warning disable CA2000
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
#pragma warning restore CA2000
        using var tempStorage = new TempFileStorageService(loggerFactory.CreateLogger<TempFileStorageService>());
        
        string? tempFilePath = null;
        try
        {
            using var uploadStream = file.OpenReadStream();
            tempFilePath = await tempStorage.SaveTempFileAsync(uploadStream, sanitizedFileName).ConfigureAwait(false);

            using var scanStream = tempStorage.OpenTempFile(tempFilePath);
            var scanResult = await virusScanService.ScanFileAsync(scanStream, sanitizedFileName).ConfigureAwait(false);

            if (!scanResult.IsClean)
            {
                throw new InvalidOperationException(
                    $"File failed security scan: {scanResult.Message}" + 
                    (scanResult.ThreatName != null ? $" (Threat: {scanResult.ThreatName})" : ""));
            }

            using var s3Stream = tempStorage.OpenTempFile(tempFilePath);
            var fileKey = await s3Service.UploadFileAsync(s3Stream, sanitizedFileName, file.ContentType ?? "application/octet-stream").ConfigureAwait(false);
            var url = s3Service.GetFileUrl(fileKey);

            var media = new AppMedia
            {
                Url = url,
                Alt = alt,
                Type = (file.ContentType != null && file.ContentType.StartsWith("video", StringComparison.OrdinalIgnoreCase)) ? MediaType.Video : MediaType.Image
            };

            context.Medias.Add(media);

            var jobItemMedia = new JobItemMedia
            {
                JobItemId = itemId,
                Media = media,
                Type = type
            };

            context.JobItemMedias.Add(jobItemMedia);
            await context.SaveChangesAsync().ConfigureAwait(false);

            return jobItemMedia;
        }
        finally
        {
            if (tempFilePath != null)
            {
                tempStorage.DeleteTempFile(tempFilePath);
            }
        }
    }
}
