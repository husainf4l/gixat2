using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Organizations.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.JobCards.Models;

/// <summary>
/// Represents an item in the organization's inventory (parts, materials, consumables)
/// </summary>
[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class InventoryItem : IMustHaveOrganization
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    /// <summary>
    /// Part number or SKU
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string PartNumber { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the part
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Category (e.g., "Engine Parts", "Electrical", "Fluids", "Tires")
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Unit of measure (e.g., "piece", "liter", "set")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string UnitOfMeasure { get; set; } = "piece";

    /// <summary>
    /// Current quantity in stock
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal QuantityInStock { get; set; }

    /// <summary>
    /// Minimum stock level for reorder alerts
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal MinimumStockLevel { get; set; }

    /// <summary>
    /// Cost price per unit (what you pay to supplier)
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }

    /// <summary>
    /// Selling price per unit (what customer pays)
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal SellingPrice { get; set; }

    /// <summary>
    /// Supplier/manufacturer name
    /// </summary>
    [MaxLength(200)]
    public string? Supplier { get; set; }

    /// <summary>
    /// Whether this item is currently active/available
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Additional notes
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Job items where this inventory item was used
    /// </summary>
    public ICollection<JobItemPart> JobItemParts { get; } = new List<JobItemPart>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
