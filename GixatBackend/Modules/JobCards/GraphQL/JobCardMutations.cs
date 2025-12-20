using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Modules.Sessions.Enums;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.JobCards.GraphQL;

[ExtendObjectType(OperationTypeNames.Mutation)]
[Authorize]
internal static class JobCardMutations
{
    public static async Task<JobCard> CreateJobCardFromSessionAsync(
        Guid sessionId,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var session = await context.GarageSessions
            .Include(s => s.Car)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == sessionId).ConfigureAwait(false);

        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        var jobCard = new JobCard
        {
            SessionId = session.Id,
            CarId = session.CarId,
            CustomerId = session.CustomerId,
            OrganizationId = session.OrganizationId,
            Status = JobCardStatus.Pending,
            InternalNotes = $"Created from Session {session.Id}"
        };

        // Automatically add items from customer requests if possible
        if (!string.IsNullOrEmpty(session.CustomerRequests))
        {
            jobCard.Items.Add(new JobItem
            {
                Description = $"Customer Request: {session.CustomerRequests}",
                Status = JobItemStatus.Pending
            });
        }

        session.Status = SessionStatus.JobCardCreated;
        
        context.JobCards.Add(jobCard);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return jobCard;
    }

    public static async Task<JobCard> AddJobItemAsync(
        Guid jobCardId,
        string description,
        decimal estimatedCost,
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
            EstimatedCost = estimatedCost,
            Status = JobItemStatus.Pending
        };

        jobCard.Items.Add(item);
        jobCard.TotalEstimatedCost += estimatedCost;
        jobCard.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync().ConfigureAwait(false);
        return jobCard;
    }

    public static async Task<JobItem> UpdateJobItemStatusAsync(
        Guid itemId,
        JobItemStatus status,
        decimal actualCost,
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

        item.Status = status;
        item.ActualCost = actualCost;
        item.TechnicianNotes = technicianNotes;
        item.UpdatedAt = DateTime.UtcNow;

        if (item.JobCard != null)
        {
            item.JobCard.TotalActualCost = await context.JobItems
                .Where(i => i.JobCardId == item.JobCardId)
                .SumAsync(i => i.ActualCost).ConfigureAwait(false);
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

        var jobCard = await context.JobCards.FindAsync(jobCardId).ConfigureAwait(false);
        if (jobCard == null)
        {
            throw new InvalidOperationException("Job Card not found");
        }

        jobCard.Status = status;
        jobCard.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync().ConfigureAwait(false);
        return jobCard;
    }
}
