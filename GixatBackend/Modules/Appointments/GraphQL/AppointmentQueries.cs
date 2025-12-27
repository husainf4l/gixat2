using System.Diagnostics.CodeAnalysis;
using GixatBackend.Data;
using GixatBackend.Modules.Appointments.Models;
using GixatBackend.Modules.Appointments.Services;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.Appointments.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by HotChocolate")]
internal sealed class AppointmentQueries
{
    /// <summary>
    /// Get all appointments with filtering and sorting
    /// </summary>
    [Authorize]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Appointment> GetAppointments([Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Car)
            .Include(a => a.AssignedTechnician);
    }

    /// <summary>
    /// Get a specific appointment by ID
    /// </summary>
    [Authorize]
    public static async Task<Appointment?> GetAppointmentByIdAsync(
        Guid id,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        return await context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Car)
            .Include(a => a.AssignedTechnician)
            .Include(a => a.Session)
            .FirstOrDefaultAsync(a => a.Id == id)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get appointments for a specific date range
    /// </summary>
    [Authorize]
    public static async Task<List<Appointment>> GetAppointmentsByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        return await context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Car)
            .Include(a => a.AssignedTechnician)
            .Where(a => a.ScheduledStartTime >= startDate && a.ScheduledStartTime < endDate)
            .OrderBy(a => a.ScheduledStartTime)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get available time slots for a specific date
    /// </summary>
    [Authorize]
    public static async Task<List<DateTime>> GetAvailableSlotsAsync(
        DateTime date,
        int durationMinutes,
        string? technicianId,
        Guid organizationId,
        [Service] IAppointmentService appointmentService)
    {
        ArgumentNullException.ThrowIfNull(appointmentService);
        
        return await appointmentService.GetAvailableSlotsAsync(
            date, 
            durationMinutes, 
            technicianId, 
            organizationId)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get upcoming appointments for a customer
    /// </summary>
    [Authorize]
    public static async Task<List<Appointment>> GetCustomerUpcomingAppointmentsAsync(
        Guid customerId,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var now = DateTime.UtcNow;
        
        return await context.Appointments
            .Include(a => a.Car)
            .Include(a => a.AssignedTechnician)
            .Where(a => a.CustomerId == customerId)
            .Where(a => a.ScheduledStartTime >= now)
            .OrderBy(a => a.ScheduledStartTime)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get technician's appointments for a specific date
    /// </summary>
    [Authorize]
    public static async Task<List<Appointment>> GetTechnicianAppointmentsAsync(
        string technicianId,
        DateTime date,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var dayStart = date.Date;
        var dayEnd = date.Date.AddDays(1);
        
        return await context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Car)
            .Where(a => a.AssignedTechnicianId == technicianId)
            .Where(a => a.ScheduledStartTime >= dayStart && a.ScheduledStartTime < dayEnd)
            .OrderBy(a => a.ScheduledStartTime)
            .ToListAsync()
            .ConfigureAwait(false);
    }
}
