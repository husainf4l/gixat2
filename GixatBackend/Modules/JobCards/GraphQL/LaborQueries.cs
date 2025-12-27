using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.JobCards.GraphQL;

/// <summary>
/// GraphQL queries for labor tracking
/// </summary>
[ExtendObjectType(OperationTypeNames.Query)]
internal sealed class LaborQueries
{
    /// <summary>
    /// Get all labor entries for a job item
    /// </summary>
    [Authorize]
    public static async Task<List<LaborEntry>> GetLaborEntriesByJobItemAsync(
        Guid jobItemId,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await context.LaborEntries
            .Where(le => le.JobItemId == jobItemId)
            .OrderBy(le => le.StartTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get labor entries for a technician
    /// </summary>
    [Authorize]
    public static async Task<List<LaborEntry>> GetLaborEntriesByTechnicianAsync(
        string technicianId,
        DateTime? startDate,
        DateTime? endDate,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(technicianId);

        var query = context.LaborEntries
            .Where(le => le.TechnicianId == technicianId);

        if (startDate.HasValue)
        {
            query = query.Where(le => le.StartTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(le => le.StartTime <= endDate.Value);
        }

        return await query
            .OrderByDescending(le => le.StartTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get labor entry by ID
    /// </summary>
    [Authorize]
    public static async Task<LaborEntry?> GetLaborEntryByIdAsync(
        Guid id,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await context.LaborEntries
            .Include(le => le.Technician)
            .Include(le => le.JobItem)
            .FirstOrDefaultAsync(le => le.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get all active (not clocked out) labor entries for the organization
    /// </summary>
    [Authorize]
    public static async Task<List<LaborEntry>> GetActiveLaborEntriesAsync(
        string? technicianId,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var query = context.LaborEntries
            .Where(le => le.EndTime == null);

        if (!string.IsNullOrWhiteSpace(technicianId))
        {
            query = query.Where(le => le.TechnicianId == technicianId);
        }

        return await query
            .OrderBy(le => le.StartTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get labor summary for a job card
    /// </summary>
    [Authorize]
    public static async Task<LaborSummary> GetLaborSummaryByJobCardAsync(
        Guid jobCardId,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var laborEntries = await context.LaborEntries
            .Where(le => le.JobItem!.JobCardId == jobCardId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var estimatedEntries = laborEntries.Where(le => !le.IsActual).ToList();
        var actualEntries = laborEntries.Where(le => le.IsActual).ToList();

        return new LaborSummary
        {
            TotalEstimatedHours = estimatedEntries.Sum(le => le.HoursWorked),
            TotalActualHours = actualEntries.Sum(le => le.HoursWorked),
            TotalEstimatedCost = estimatedEntries.Sum(le => le.TotalCost),
            TotalActualCost = actualEntries.Sum(le => le.TotalCost),
            EntryCount = laborEntries.Count
        };
    }
}

/// <summary>
/// Summary of labor data
/// </summary>
internal sealed class LaborSummary
{
    public decimal TotalEstimatedHours { get; set; }
    public decimal TotalActualHours { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public decimal TotalActualCost { get; set; }
    public int EntryCount { get; set; }
}
