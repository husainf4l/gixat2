using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.JobCards.Models;

/// <summary>
/// Represents a labor time entry by a technician on a specific job item
/// Tracks start/end time, hours worked, and labor cost
/// </summary>
[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class LaborEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid JobItemId { get; set; }
    public JobItem? JobItem { get; set; }

    [Required]
    [MaxLength(450)]
    public string TechnicianId { get; set; } = string.Empty;
    public GixatBackend.Modules.Users.Models.ApplicationUser? Technician { get; set; }

    /// <summary>
    /// When the technician started working on this task
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// When the technician finished working on this task (null if still in progress)
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total hours worked (calculated from StartTime and EndTime, or manually entered)
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal HoursWorked { get; set; }

    /// <summary>
    /// Hourly rate for this labor entry
    /// Can vary per technician skill level or job type
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal HourlyRate { get; set; }

    /// <summary>
    /// Total labor cost (HoursWorked * HourlyRate)
    /// Automatically calculated
    /// </summary>
    public decimal TotalCost => HoursWorked * HourlyRate;

    /// <summary>
    /// Type of labor (e.g., "Diagnostic", "Repair", "Installation", "Testing")
    /// </summary>
    [MaxLength(100)]
    public string? LaborType { get; set; }

    /// <summary>
    /// Description of work performed during this time
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is an estimated labor entry (false) or actual time worked (true)
    /// </summary>
    public bool IsActual { get; set; }

    /// <summary>
    /// Whether this labor entry is billable to the customer
    /// </summary>
    public bool IsBillable { get; set; } = true;

    /// <summary>
    /// Additional notes about this labor entry
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
