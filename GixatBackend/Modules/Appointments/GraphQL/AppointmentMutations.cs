using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using GixatBackend.Data;
using GixatBackend.Modules.Appointments.Enums;
using GixatBackend.Modules.Appointments.Models;
using GixatBackend.Modules.Appointments.Services;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.Appointments.GraphQL;

[ExtendObjectType(OperationTypeNames.Mutation)]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by HotChocolate")]
internal static class AppointmentMutations
{
    /// <summary>
    /// Create a new appointment
    /// </summary>
    [Authorize]
    public static async Task<AppointmentPayload> CreateAppointmentAsync(
        CreateAppointmentInput input,
        ClaimsPrincipal claimsPrincipal,
        [Service] ApplicationDbContext context,
        [Service] IAppointmentService appointmentService)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(appointmentService);

        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        // Validate customer and car exist
        var customer = await context.Customers.FindAsync(input.CustomerId).ConfigureAwait(false);
        if (customer == null)
        {
            return new AppointmentPayload(null, "Customer not found");
        }

        var car = await context.Cars.FindAsync(input.CarId).ConfigureAwait(false);
        if (car == null || car.CustomerId != input.CustomerId)
        {
            return new AppointmentPayload(null, "Car not found or does not belong to customer");
        }

        // Validate times
        if (input.ScheduledStartTime >= input.ScheduledEndTime)
        {
            return new AppointmentPayload(null, "Start time must be before end time");
        }

        if (input.ScheduledStartTime < DateTime.UtcNow)
        {
            return new AppointmentPayload(null, "Cannot schedule appointment in the past");
        }

        // Check slot availability
        var isAvailable = await appointmentService.IsSlotAvailableAsync(
            input.ScheduledStartTime,
            input.ScheduledEndTime,
            input.AssignedTechnicianId,
            customer.OrganizationId)
            .ConfigureAwait(false);

        if (!isAvailable)
        {
            return new AppointmentPayload(null, "Time slot is not available");
        }

        // Check customer doesn't have conflicting appointment
        var hasConflict = await appointmentService.HasConflictingAppointmentAsync(
            input.CustomerId,
            input.ScheduledStartTime,
            input.ScheduledEndTime)
            .ConfigureAwait(false);

        if (hasConflict)
        {
            return new AppointmentPayload(null, "Customer already has an appointment during this time");
        }

        var appointment = new Appointment
        {
            CustomerId = input.CustomerId,
            CarId = input.CarId,
            ScheduledStartTime = input.ScheduledStartTime,
            ScheduledEndTime = input.ScheduledEndTime,
            AssignedTechnicianId = input.AssignedTechnicianId,
            Type = input.Type,
            ServiceRequested = input.ServiceRequested,
            CustomerNotes = input.CustomerNotes,
            InternalNotes = input.InternalNotes,
            EstimatedDurationMinutes = input.EstimatedDurationMinutes,
            ContactPhone = input.ContactPhone ?? customer.PhoneNumber,
            ContactEmail = input.ContactEmail ?? customer.Email,
            Status = AppointmentStatus.Scheduled,
            OrganizationId = customer.OrganizationId,
            CreatedById = userId
        };

        context.Appointments.Add(appointment);
        await context.SaveChangesAsync().ConfigureAwait(false);

        // Reload with navigation properties
        await context.Entry(appointment)
            .Reference(a => a.Customer)
            .LoadAsync()
            .ConfigureAwait(false);
        await context.Entry(appointment)
            .Reference(a => a.Car)
            .LoadAsync()
            .ConfigureAwait(false);
        if (appointment.AssignedTechnicianId != null)
        {
            await context.Entry(appointment)
                .Reference(a => a.AssignedTechnician)
                .LoadAsync()
                .ConfigureAwait(false);
        }

        return new AppointmentPayload(appointment);
    }

    /// <summary>
    /// Update an existing appointment
    /// </summary>
    [Authorize]
    public static async Task<AppointmentPayload> UpdateAppointmentAsync(
        Guid id,
        UpdateAppointmentInput input,
        [Service] ApplicationDbContext context,
        [Service] IAppointmentService appointmentService)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        var appointment = await context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Car)
            .Include(a => a.AssignedTechnician)
            .FirstOrDefaultAsync(a => a.Id == id)
            .ConfigureAwait(false);

        if (appointment == null)
        {
            return new AppointmentPayload(null, "Appointment not found");
        }

        // If rescheduling, validate new time slot
        if (input.ScheduledStartTime.HasValue || input.ScheduledEndTime.HasValue)
        {
            var newStartTime = input.ScheduledStartTime ?? appointment.ScheduledStartTime;
            var newEndTime = input.ScheduledEndTime ?? appointment.ScheduledEndTime;

            if (newStartTime >= newEndTime)
            {
                return new AppointmentPayload(null, "Start time must be before end time");
            }

            var isAvailable = await appointmentService.IsSlotAvailableAsync(
                newStartTime,
                newEndTime,
                input.AssignedTechnicianId ?? appointment.AssignedTechnicianId,
                appointment.OrganizationId,
                appointment.Id)
                .ConfigureAwait(false);

            if (!isAvailable)
            {
                return new AppointmentPayload(null, "Time slot is not available");
            }

            appointment.ScheduledStartTime = newStartTime;
            appointment.ScheduledEndTime = newEndTime;
        }

        if (input.AssignedTechnicianId != null)
        {
            appointment.AssignedTechnicianId = input.AssignedTechnicianId;
        }

        if (input.ServiceRequested != null)
        {
            appointment.ServiceRequested = input.ServiceRequested;
        }

        if (input.CustomerNotes != null)
        {
            appointment.CustomerNotes = input.CustomerNotes;
        }

        if (input.InternalNotes != null)
        {
            appointment.InternalNotes = input.InternalNotes;
        }

        if (input.ContactPhone != null)
        {
            appointment.ContactPhone = input.ContactPhone;
        }

        if (input.ContactEmail != null)
        {
            appointment.ContactEmail = input.ContactEmail;
        }

        appointment.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync().ConfigureAwait(false);

        return new AppointmentPayload(appointment);
    }

    /// <summary>
    /// Update appointment status
    /// </summary>
    [Authorize]
    public static async Task<AppointmentPayload> UpdateAppointmentStatusAsync(
        Guid id,
        AppointmentStatus status,
        string? cancellationReason,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var appointment = await context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Car)
            .Include(a => a.AssignedTechnician)
            .FirstOrDefaultAsync(a => a.Id == id)
            .ConfigureAwait(false);

        if (appointment == null)
        {
            return new AppointmentPayload(null, "Appointment not found");
        }

        appointment.Status = status;
        appointment.UpdatedAt = DateTime.UtcNow;

        if (status == AppointmentStatus.Cancelled)
        {
            appointment.CancelledAt = DateTime.UtcNow;
            appointment.CancellationReason = cancellationReason;
        }

        await context.SaveChangesAsync().ConfigureAwait(false);

        return new AppointmentPayload(appointment);
    }

    /// <summary>
    /// Convert appointment to session
    /// </summary>
    [Authorize]
    public static async Task<AppointmentPayload> ConvertToSessionAsync(
        Guid id,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var appointment = await context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Car)
            .FirstOrDefaultAsync(a => a.Id == id)
            .ConfigureAwait(false);

        if (appointment == null)
        {
            return new AppointmentPayload(null, "Appointment not found");
        }

        if (appointment.SessionId.HasValue)
        {
            return new AppointmentPayload(null, "Appointment already converted to session");
        }

        // Create session from appointment
        var session = new GixatBackend.Modules.Sessions.Models.GarageSession
        {
            CustomerId = appointment.CustomerId,
            CarId = appointment.CarId,
            Status = GixatBackend.Modules.Sessions.Enums.SessionStatus.CustomerRequest,
            CustomerRequests = appointment.ServiceRequested,
            OrganizationId = appointment.OrganizationId
        };

        context.GarageSessions.Add(session);
        
        appointment.SessionId = session.Id;
        appointment.Status = AppointmentStatus.InProgress;
        appointment.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync().ConfigureAwait(false);

        // Reload appointment with session
        await context.Entry(appointment)
            .Reference(a => a.Session)
            .LoadAsync()
            .ConfigureAwait(false);

        return new AppointmentPayload(appointment);
    }

    /// <summary>
    /// Delete an appointment
    /// </summary>
    [Authorize(Roles = new[] { "OrgAdmin", "OrgManager" })]
    public static async Task<bool> DeleteAppointmentAsync(
        Guid id,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var appointment = await context.Appointments.FindAsync(id).ConfigureAwait(false);
        if (appointment == null)
        {
            return false;
        }

        context.Appointments.Remove(appointment);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return true;
    }
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public record CreateAppointmentInput(
    Guid CustomerId,
    Guid CarId,
    DateTime ScheduledStartTime,
    DateTime ScheduledEndTime,
    AppointmentType Type,
    string? ServiceRequested = null,
    string? CustomerNotes = null,
    string? InternalNotes = null,
    string? AssignedTechnicianId = null,
    string? ContactPhone = null,
    string? ContactEmail = null,
    int EstimatedDurationMinutes = 60
);

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public record UpdateAppointmentInput(
    DateTime? ScheduledStartTime = null,
    DateTime? ScheduledEndTime = null,
    string? ServiceRequested = null,
    string? CustomerNotes = null,
    string? InternalNotes = null,
    string? AssignedTechnicianId = null,
    string? ContactPhone = null,
    string? ContactEmail = null
);

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public record AppointmentPayload(Appointment? Appointment, string? Error = null);
