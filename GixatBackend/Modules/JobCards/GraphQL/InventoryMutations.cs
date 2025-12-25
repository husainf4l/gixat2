using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.JobCards.GraphQL;

/// <summary>
/// Input for creating a new inventory item
/// </summary>
public sealed record CreateInventoryItemInput(
    string PartNumber,
    string Name,
    string? Description,
    string? Category,
    string UnitOfMeasure,
    decimal QuantityInStock,
    decimal MinimumStockLevel,
    decimal CostPrice,
    decimal SellingPrice,
    string? Supplier,
    string? Notes
);

/// <summary>
/// Input for updating an inventory item
/// </summary>
public sealed record UpdateInventoryItemInput(
    Guid Id,
    string? PartNumber,
    string? Name,
    string? Description,
    string? Category,
    string? UnitOfMeasure,
    decimal? QuantityInStock,
    decimal? MinimumStockLevel,
    decimal? CostPrice,
    decimal? SellingPrice,
    string? Supplier,
    bool? IsActive,
    string? Notes
);

/// <summary>
/// Input for adjusting inventory quantity
/// </summary>
public sealed record AdjustInventoryInput(
    Guid InventoryItemId,
    decimal QuantityChange,
    string? Reason
);

/// <summary>
/// GraphQL mutations for inventory management
/// </summary>
[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class InventoryMutations
{
    /// <summary>
    /// Create a new inventory item
    /// </summary>
    [Authorize]
    public async Task<InventoryItem> CreateInventoryItemAsync(
        CreateInventoryItemInput input,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        // Check if part number already exists for this organization
        var exists = await context.InventoryItems
            .AnyAsync(i => i.PartNumber == input.PartNumber, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            throw new GraphQLException($"Inventory item with part number '{input.PartNumber}' already exists.");
        }

        var item = new InventoryItem
        {
            PartNumber = input.PartNumber,
            Name = input.Name,
            Description = input.Description,
            Category = input.Category,
            UnitOfMeasure = input.UnitOfMeasure,
            QuantityInStock = input.QuantityInStock,
            MinimumStockLevel = input.MinimumStockLevel,
            CostPrice = input.CostPrice,
            SellingPrice = input.SellingPrice,
            Supplier = input.Supplier,
            Notes = input.Notes,
            IsActive = true
        };

        context.InventoryItems.Add(item);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return item;
    }

    /// <summary>
    /// Update an existing inventory item
    /// </summary>
    [Authorize]
    public async Task<InventoryItem> UpdateInventoryItemAsync(
        UpdateInventoryItemInput input,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        var item = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == input.Id, cancellationToken)
            .ConfigureAwait(false);

        if (item == null)
        {
            throw new GraphQLException($"Inventory item with ID '{input.Id}' not found.");
        }

        // Check if part number is being changed and if it conflicts
        if (input.PartNumber != null && input.PartNumber != item.PartNumber)
        {
            var exists = await context.InventoryItems
                .AnyAsync(i => i.PartNumber == input.PartNumber && i.Id != input.Id, cancellationToken)
                .ConfigureAwait(false);

            if (exists)
            {
                throw new GraphQLException($"Inventory item with part number '{input.PartNumber}' already exists.");
            }
            item.PartNumber = input.PartNumber;
        }

        if (input.Name != null) item.Name = input.Name;
        if (input.Description != null) item.Description = input.Description;
        if (input.Category != null) item.Category = input.Category;
        if (input.UnitOfMeasure != null) item.UnitOfMeasure = input.UnitOfMeasure;
        if (input.QuantityInStock.HasValue) item.QuantityInStock = input.QuantityInStock.Value;
        if (input.MinimumStockLevel.HasValue) item.MinimumStockLevel = input.MinimumStockLevel.Value;
        if (input.CostPrice.HasValue) item.CostPrice = input.CostPrice.Value;
        if (input.SellingPrice.HasValue) item.SellingPrice = input.SellingPrice.Value;
        if (input.Supplier != null) item.Supplier = input.Supplier;
        if (input.IsActive.HasValue) item.IsActive = input.IsActive.Value;
        if (input.Notes != null) item.Notes = input.Notes;

        item.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return item;
    }

    /// <summary>
    /// Adjust inventory quantity (add or subtract stock)
    /// </summary>
    [Authorize]
    public async Task<InventoryItem> AdjustInventoryQuantityAsync(
        AdjustInventoryInput input,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        var item = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == input.InventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        if (item == null)
        {
            throw new GraphQLException($"Inventory item with ID '{input.InventoryItemId}' not found.");
        }

        var newQuantity = item.QuantityInStock + input.QuantityChange;
        if (newQuantity < 0)
        {
            throw new GraphQLException($"Cannot adjust inventory. Resulting quantity would be negative ({newQuantity}).");
        }

        item.QuantityInStock = newQuantity;
        item.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return item;
    }

    /// <summary>
    /// Delete (deactivate) an inventory item
    /// </summary>
    [Authorize]
    public async Task<bool> DeleteInventoryItemAsync(
        Guid id,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var item = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (item == null)
        {
            throw new GraphQLException($"Inventory item with ID '{id}' not found.");
        }

        // Soft delete - just mark as inactive
        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }
}
