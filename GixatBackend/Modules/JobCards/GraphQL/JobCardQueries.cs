using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.JobCards.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
[Authorize]
internal sealed class JobCardQueries
{
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<JobCard> GetJobCards(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.JobCards
            .AsNoTracking()
            .Include(j => j.Car)
            .Include(j => j.Customer)
            .OrderByDescending(j => j.CreatedAt);
    }

    [UsePaging]
    [UseProjection]
    public static IQueryable<JobCard> SearchJobCards(
        string? query,
        JobCardStatus? status,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var jobCards = context.JobCards
            .AsNoTracking()
            .AsQueryable();

        // Filter by status if provided
        if (status.HasValue)
        {
            jobCards = jobCards.Where(j => j.Status == status.Value);
        }

        // Search by customer name, car details, or job card ID
        if (!string.IsNullOrWhiteSpace(query))
        {
            var searchQuery = $"%{query.Trim()}%";
            jobCards = jobCards.Where(j =>
                EF.Functions.ILike(j.Customer!.FirstName, searchQuery) ||
                EF.Functions.ILike(j.Customer!.LastName, searchQuery) ||
                EF.Functions.ILike(j.Car!.Make, searchQuery) ||
                EF.Functions.ILike(j.Car!.Model, searchQuery) ||
                EF.Functions.ILike(j.Car!.LicensePlate, searchQuery) ||
                EF.Functions.ILike(j.Id.ToString(), searchQuery)
            );
        }

        return jobCards.OrderByDescending(j => j.CreatedAt);
    }

    public static async Task<JobCard?> GetJobCardByIdAsync(Guid id, ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await context.JobCards
            .AsNoTracking()
            .Include(j => j.Car)
            .Include(j => j.Customer)
            .Include(j => j.Items)
            .Include(j => j.Session)
            .FirstOrDefaultAsync(j => j.Id == id).ConfigureAwait(false);
    }

    // Get job cards by customer
    [UsePaging]
    [UseProjection]
    public static IQueryable<JobCard> GetJobCardsByCustomer(
        Guid customerId,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.JobCards
            .AsNoTracking()
            .Where(j => j.CustomerId == customerId)
            .OrderByDescending(j => j.CreatedAt);
    }

    // Get job cards by status
    [UsePaging]
    [UseProjection]
    public static IQueryable<JobCard> GetJobCardsByStatus(
        JobCardStatus status,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.JobCards
            .AsNoTracking()
            .Where(j => j.Status == status)
            .OrderByDescending(j => j.CreatedAt);
    }
}
