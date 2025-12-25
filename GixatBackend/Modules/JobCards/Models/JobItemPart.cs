using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.JobCards.Models;

/// <summary>
/// Represents a part/inventory item used in a specific job item
/// Links JobItem to InventoryItem with quantity and pricing details
/// </summary>
[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class JobItemPart
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid JobItemId { get; set; }
    public JobItem? JobItem { get; set; }

    [Required]
    public Guid InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }

    /// <summary>
    /// Quantity of this part used/needed
    /// </summary>
    [Required]
    [Range(0.001, double.MaxValue)]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price at the time of use (may differ from current inventory selling price)
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total cost for this part (Quantity * UnitPrice)
    /// Automatically calculated
    /// </summary>
    public decimal TotalCost => Quantity * UnitPrice;

    /// <summary>
    /// Discount applied (percentage or fixed amount)
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal Discount { get; set; }

    /// <summary>
    /// Final cost after discount
    /// </summary>
    public decimal FinalCost => TotalCost - Discount;

    /// <summary>
    /// Additional notes about this part usage
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this part was actually used (true) or just estimated (false)
    /// </summary>
    public bool IsActual { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
