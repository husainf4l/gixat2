using GixatBackend.Modules.Appointments.Models;

namespace GixatBackend.Modules.Appointments.Services;

internal interface IAppointmentService
{
    Task<bool> IsSlotAvailableAsync(DateTime startTime, DateTime endTime, string? technicianId, Guid organizationId, Guid? excludeAppointmentId = null);
    Task<List<DateTime>> GetAvailableSlotsAsync(DateTime date, int durationMinutes, string? technicianId, Guid organizationId);
    Task<bool> HasConflictingAppointmentAsync(Guid customerId, DateTime startTime, DateTime endTime, Guid? excludeAppointmentId = null);
}
