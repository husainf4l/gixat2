using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.JobCards.GraphQL;

/// <summary>
/// Input for adding a part to a job item
/// </summary>
public sealed record AddPartToJobItemInput(
    Guid JobItemId,
    Guid InventoryItemId,
    decimal Quantity,
    decimal? UnitPrice,
    decimal Discount,
    bool IsActual,
    string? Notes
);

/// <summary>
/// Input for updating a job item part
/// </summary>
public sealed record UpdateJobItemPartInput(
    Guid Id,
    decimal? Quantity,
    decimal? UnitPrice,
    decimal? Discount,
    bool? IsActual,
    string? Notes
);

/// <summary>
/// Input for adding a labor entry
/// </summary>
public sealed record AddLaborEntryInput(
    Guid JobItemId,
    string TechnicianId,
    DateTime StartTime,
    DateTime? EndTime,
    decimal? HoursWorked,
    decimal HourlyRate,
    string? LaborType,
    string? Description,
    bool IsActual,
    bool IsBillable,
    string? Notes
);

/// <summary>
/// Input for updating a labor entry
/// </summary>
public sealed record UpdateLaborEntryInput(
    Guid Id,
    DateTime? StartTime,
    DateTime? EndTime,
    decimal? HoursWorked,
    decimal? HourlyRate,
    string? LaborType,
    string? Description,
    bool? IsActual,
    bool? IsBillable,
    string? Notes
);

/// <summary>
/// GraphQL mutations for managing parts and labor on job items
/// </summary>
[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class JobItemPartsAndLaborMutations
{
    #region Part Management

    /// <summary>
    /// Add a part to a job item
    /// </summary>
    [Authorize]
    public async Task<JobItemPart> AddPartToJobItemAsync(
        AddPartToJobItemInput input,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        // Verify job item exists
        var jobItem = await context.JobItems
            .FirstOrDefaultAsync(ji => ji.Id == input.JobItemId, cancellationToken)
            .ConfigureAwait(false);

        if (jobItem == null)
        {
            throw new GraphQLException($"Job item with ID '{input.JobItemId}' not found.");
        }

        // Verify inventory item exists
        var inventoryItem = await context.InventoryItems
            .FirstOrDefaultAsync(ii => ii.Id == input.InventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        if (inventoryItem == null)
        {
            throw new GraphQLException($"Inventory item with ID '{input.InventoryItemId}' not found.");
        }

        // Use provided unit price or default to inventory selling price
        var unitPrice = input.UnitPrice ?? inventoryItem.SellingPrice;

        var jobItemPart = new JobItemPart
        {
            JobItemId = input.JobItemId,
            InventoryItemId = input.InventoryItemId,
            Quantity = input.Quantity,
            UnitPrice = unitPrice,
            Discount = input.Discount,
            IsActual = input.IsActual,
            Notes = input.Notes
        };

        context.JobItemParts.Add(jobItemPart);

        // If this is actual usage, deduct from inventory
        if (input.IsActual)
        {
            if (inventoryItem.QuantityInStock < input.Quantity)
            {
                throw new GraphQLException(
                    $"Insufficient stock for '{inventoryItem.Name}'. " +
                    $"Available: {inventoryItem.QuantityInStock}, Required: {input.Quantity}");
            }

            inventoryItem.QuantityInStock -= input.Quantity;
            inventoryItem.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return jobItemPart;
    }

    /// <summary>
    /// Update a job item part
    /// </summary>
    [Authorize]
    public async Task<JobItemPart> UpdateJobItemPartAsync(
        UpdateJobItemPartInput input,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        var jobItemPart = await context.JobItemParts
            .Include(jip => jip.InventoryItem)
            .FirstOrDefaultAsync(jip => jip.Id == input.Id, cancellationToken)
            .ConfigureAwait(false);

        if (jobItemPart == null)
        {
            throw new GraphQLException($"Job item part with ID '{input.Id}' not found.");
        }

        var oldQuantity = jobItemPart.Quantity;
        var oldIsActual = jobItemPart.IsActual;

        if (input.Quantity.HasValue) jobItemPart.Quantity = input.Quantity.Value;
        if (input.UnitPrice.HasValue) jobItemPart.UnitPrice = input.UnitPrice.Value;
        if (input.Discount.HasValue) jobItemPart.Discount = input.Discount.Value;
        if (input.IsActual.HasValue) jobItemPart.IsActual = input.IsActual.Value;
        if (input.Notes != null) jobItemPart.Notes = input.Notes;

        jobItemPart.UpdatedAt = DateTime.UtcNow;

        // Handle inventory adjustments if quantity or actual status changed
        if (jobItemPart.InventoryItem != null)
        {
            if (oldIsActual && jobItemPart.IsActual && oldQuantity != jobItemPart.Quantity)
            {
                // Both old and new are actual - adjust the difference
                var quantityDiff = jobItemPart.Quantity - oldQuantity;
                jobItemPart.InventoryItem.QuantityInStock -= quantityDiff;
                jobItemPart.InventoryItem.UpdatedAt = DateTime.UtcNow;
            }
            else if (!oldIsActual && jobItemPart.IsActual)
            {
                // Changed from estimate to actual - deduct from inventory
                jobItemPart.InventoryItem.QuantityInStock -= jobItemPart.Quantity;
                jobItemPart.InventoryItem.UpdatedAt = DateTime.UtcNow;
            }
            else if (oldIsActual && !jobItemPart.IsActual)
            {
                // Changed from actual to estimate - add back to inventory
                jobItemPart.InventoryItem.QuantityInStock += oldQuantity;
                jobItemPart.InventoryItem.UpdatedAt = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return jobItemPart;
    }

    /// <summary>
    /// Remove a part from a job item
    /// </summary>
    [Authorize]
    public async Task<bool> RemovePartFromJobItemAsync(
        Guid id,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var jobItemPart = await context.JobItemParts
            .Include(jip => jip.InventoryItem)
            .FirstOrDefaultAsync(jip => jip.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (jobItemPart == null)
        {
            throw new GraphQLException($"Job item part with ID '{id}' not found.");
        }

        // If this was actual usage, add the quantity back to inventory
        if (jobItemPart.IsActual && jobItemPart.InventoryItem != null)
        {
            jobItemPart.InventoryItem.QuantityInStock += jobItemPart.Quantity;
            jobItemPart.InventoryItem.UpdatedAt = DateTime.UtcNow;
        }

        context.JobItemParts.Remove(jobItemPart);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    #endregion

    #region Labor Management

    /// <summary>
    /// Add a labor entry to a job item
    /// </summary>
    [Authorize]
    public async Task<LaborEntry> AddLaborEntryAsync(
        AddLaborEntryInput input,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        // Verify job item exists
        var jobItem = await context.JobItems
            .FirstOrDefaultAsync(ji => ji.Id == input.JobItemId, cancellationToken)
            .ConfigureAwait(false);

        if (jobItem == null)
        {
            throw new GraphQLException($"Job item with ID '{input.JobItemId}' not found.");
        }

        // Verify technician exists
        var technician = await context.Users
            .FirstOrDefaultAsync(u => u.Id == input.TechnicianId, cancellationToken)
            .ConfigureAwait(false);

        if (technician == null)
        {
            throw new GraphQLException($"Technician with ID '{input.TechnicianId}' not found.");
        }

        // Calculate hours worked if not provided
        decimal hoursWorked = input.HoursWorked ?? 0;
        if (!input.HoursWorked.HasValue && input.EndTime.HasValue)
        {
            var timeSpan = input.EndTime.Value - input.StartTime;
            hoursWorked = (decimal)timeSpan.TotalHours;
        }

        var laborEntry = new LaborEntry
        {
            JobItemId = input.JobItemId,
            TechnicianId = input.TechnicianId,
            StartTime = input.StartTime,
            EndTime = input.EndTime,
            HoursWorked = hoursWorked,
            HourlyRate = input.HourlyRate,
            LaborType = input.LaborType,
            Description = input.Description,
            IsActual = input.IsActual,
            IsBillable = input.IsBillable,
            Notes = input.Notes
        };

        context.LaborEntries.Add(laborEntry);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return laborEntry;
    }

    /// <summary>
    /// Update a labor entry
    /// </summary>
    [Authorize]
    public async Task<LaborEntry> UpdateLaborEntryAsync(
        UpdateLaborEntryInput input,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        var laborEntry = await context.LaborEntries
            .FirstOrDefaultAsync(le => le.Id == input.Id, cancellationToken)
            .ConfigureAwait(false);

        if (laborEntry == null)
        {
            throw new GraphQLException($"Labor entry with ID '{input.Id}' not found.");
        }

        if (input.StartTime.HasValue) laborEntry.StartTime = input.StartTime.Value;
        if (input.EndTime.HasValue) laborEntry.EndTime = input.EndTime;
        if (input.HourlyRate.HasValue) laborEntry.HourlyRate = input.HourlyRate.Value;
        if (input.LaborType != null) laborEntry.LaborType = input.LaborType;
        if (input.Description != null) laborEntry.Description = input.Description;
        if (input.IsActual.HasValue) laborEntry.IsActual = input.IsActual.Value;
        if (input.IsBillable.HasValue) laborEntry.IsBillable = input.IsBillable.Value;
        if (input.Notes != null) laborEntry.Notes = input.Notes;

        // Recalculate hours if provided or if times changed
        if (input.HoursWorked.HasValue)
        {
            laborEntry.HoursWorked = input.HoursWorked.Value;
        }
        else if (laborEntry.EndTime.HasValue)
        {
            var timeSpan = laborEntry.EndTime.Value - laborEntry.StartTime;
            laborEntry.HoursWorked = (decimal)timeSpan.TotalHours;
        }

        laborEntry.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return laborEntry;
    }

    /// <summary>
    /// Clock out a labor entry (set end time to now)
    /// </summary>
    [Authorize]
    public async Task<LaborEntry> ClockOutLaborEntryAsync(
        Guid id,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var laborEntry = await context.LaborEntries
            .FirstOrDefaultAsync(le => le.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (laborEntry == null)
        {
            throw new GraphQLException($"Labor entry with ID '{id}' not found.");
        }

        if (laborEntry.EndTime.HasValue)
        {
            throw new GraphQLException("Labor entry is already clocked out.");
        }

        laborEntry.EndTime = DateTime.UtcNow;
        var timeSpan = laborEntry.EndTime.Value - laborEntry.StartTime;
        laborEntry.HoursWorked = (decimal)timeSpan.TotalHours;
        laborEntry.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return laborEntry;
    }

    /// <summary>
    /// Delete a labor entry
    /// </summary>
    [Authorize]
    public async Task<bool> DeleteLaborEntryAsync(
        Guid id,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var laborEntry = await context.LaborEntries
            .FirstOrDefaultAsync(le => le.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (laborEntry == null)
        {
            throw new GraphQLException($"Labor entry with ID '{id}' not found.");
        }

        context.LaborEntries.Remove(laborEntry);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    #endregion
}
