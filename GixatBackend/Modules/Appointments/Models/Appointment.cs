using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using GixatBackend.Modules.Appointments.Enums;
using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Sessions.Models;
using GixatBackend.Modules.Users.Models;

namespace GixatBackend.Modules.Appointments.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class Appointment : IMustHaveOrganization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    [Required]
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    [Required]
    public Guid CarId { get; set; }
    public Car? Car { get; set; }
    
    [Required]
    public DateTime ScheduledStartTime { get; set; }
    
    [Required]
    public DateTime ScheduledEndTime { get; set; }
    
    public string? AssignedTechnicianId { get; set; }
    public ApplicationUser? AssignedTechnician { get; set; }
    
    [Required]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    
    [Required]
    public AppointmentType Type { get; set; }
    
    [MaxLength(1000)]
    public string? ServiceRequested { get; set; }
    
    [MaxLength(2000)]
    public string? CustomerNotes { get; set; }
    
    [MaxLength(2000)]
    public string? InternalNotes { get; set; }
    
    // Conversion tracking
    public Guid? SessionId { get; set; }
    public GarageSession? Session { get; set; }
    
    // Reminder tracking
    public bool ReminderSent { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    
    // Contact information (in case customer changes phone/email)
    [MaxLength(100)]
    public string? ContactPhone { get; set; }
    
    [MaxLength(200)]
    public string? ContactEmail { get; set; }
    
    // Estimated duration in minutes
    [Range(15, 480)]
    public int EstimatedDurationMinutes { get; set; } = 60;
    
    // Cancellation tracking
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(450)]
    public string? CreatedById { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
}
