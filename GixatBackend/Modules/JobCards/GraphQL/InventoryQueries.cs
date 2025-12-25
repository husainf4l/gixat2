using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.JobCards.GraphQL;

/// <summary>
/// GraphQL queries for inventory management
/// </summary>
[ExtendObjectType(OperationTypeNames.Query)]
public sealed class InventoryQueries
{
    /// <summary>
    /// Get all inventory items for the organization
    /// </summary>
    [Authorize]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<InventoryItem> GetInventoryItems(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.InventoryItems;
    }

    /// <summary>
    /// Get inventory item by ID
    /// </summary>
    [Authorize]
    public async Task<InventoryItem?> GetInventoryItemByIdAsync(
        Guid id,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await context.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get inventory items that are low in stock (below minimum level)
    /// </summary>
    [Authorize]
    public async Task<List<InventoryItem>> GetLowStockItemsAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await context.InventoryItems
            .Where(i => i.IsActive && i.QuantityInStock <= i.MinimumStockLevel)
            .OrderBy(i => i.QuantityInStock)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Search inventory items by name, part number, or category
    /// </summary>
    [Authorize]
    public async Task<List<InventoryItem>> SearchInventoryAsync(
        string searchTerm,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);

        var search = searchTerm.Trim().ToLowerInvariant();
        return await context.InventoryItems
            .Where(i => i.IsActive && (
                i.Name.ToLower().Contains(search) ||
                i.PartNumber.ToLower().Contains(search) ||
                (i.Category != null && i.Category.ToLower().Contains(search))
            ))
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get inventory items by category
    /// </summary>
    [Authorize]
    public async Task<List<InventoryItem>> GetInventoryByCategoryAsync(
        string category,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        return await context.InventoryItems
            .Where(i => i.IsActive && i.Category == category)
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
