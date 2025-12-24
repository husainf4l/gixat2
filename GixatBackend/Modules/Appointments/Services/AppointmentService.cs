using System.Diagnostics.CodeAnalysis;
using GixatBackend.Data;
using GixatBackend.Modules.Appointments.Enums;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.Appointments.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by DI")]
internal sealed class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;

    public AppointmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsSlotAvailableAsync(
        DateTime startTime, 
        DateTime endTime, 
        string? technicianId, 
        Guid organizationId, 
        Guid? excludeAppointmentId = null)
    {
        var query = _context.Appointments
            .Where(a => a.OrganizationId == organizationId)
            .Where(a => a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow && a.Status != AppointmentStatus.Completed)
            .Where(a => 
                (a.ScheduledStartTime < endTime && a.ScheduledEndTime > startTime));

        if (technicianId != null)
        {
            query = query.Where(a => a.AssignedTechnicianId == technicianId);
        }

        if (excludeAppointmentId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAppointmentId.Value);
        }

        var conflictingAppointments = await query.CountAsync().ConfigureAwait(false);
        
        return conflictingAppointments == 0;
    }

    public async Task<List<DateTime>> GetAvailableSlotsAsync(
        DateTime date, 
        int durationMinutes, 
        string? technicianId, 
        Guid organizationId)
    {
        var availableSlots = new List<DateTime>();
        
        // Business hours: 8 AM to 6 PM
        var workStart = new DateTime(date.Year, date.Month, date.Day, 8, 0, 0);
        var workEnd = new DateTime(date.Year, date.Month, date.Day, 18, 0, 0);
        
        // Get all appointments for this day
        var dayStart = date.Date;
        var dayEnd = date.Date.AddDays(1);
        
        var appointments = await _context.Appointments
            .Where(a => a.OrganizationId == organizationId)
            .Where(a => a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow && a.Status != AppointmentStatus.Completed)
            .Where(a => a.ScheduledStartTime >= dayStart && a.ScheduledStartTime < dayEnd)
            .Where(a => technicianId == null || a.AssignedTechnicianId == technicianId)
            .OrderBy(a => a.ScheduledStartTime)
            .Select(a => new { a.ScheduledStartTime, a.ScheduledEndTime })
            .ToListAsync()
            .ConfigureAwait(false);
        
        // Check each 30-minute slot
        var currentTime = workStart;
        while (currentTime.AddMinutes(durationMinutes) <= workEnd)
        {
            var slotEnd = currentTime.AddMinutes(durationMinutes);
            
            // Check if this slot conflicts with any appointment
            var hasConflict = appointments.Any(a => 
                (a.ScheduledStartTime < slotEnd && a.ScheduledEndTime > currentTime));
            
            if (!hasConflict)
            {
                availableSlots.Add(currentTime);
            }
            
            currentTime = currentTime.AddMinutes(30); // 30-minute increments
        }
        
        return availableSlots;
    }

    public async Task<bool> HasConflictingAppointmentAsync(
        Guid customerId, 
        DateTime startTime, 
        DateTime endTime, 
        Guid? excludeAppointmentId = null)
    {
        var query = _context.Appointments
            .Where(a => a.CustomerId == customerId)
            .Where(a => a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow && a.Status != AppointmentStatus.Completed)
            .Where(a => 
                (a.ScheduledStartTime < endTime && a.ScheduledEndTime > startTime));

        if (excludeAppointmentId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAppointmentId.Value);
        }

        return await query.AnyAsync().ConfigureAwait(false);
    }
}
