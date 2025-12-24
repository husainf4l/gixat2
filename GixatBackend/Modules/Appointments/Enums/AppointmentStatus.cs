using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Appointments.Enums;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public enum AppointmentStatus
{
    Scheduled = 0,
    Confirmed = 1,
    CheckedIn = 2,
    InProgress = 3,
    Completed = 4,
    NoShow = 5,
    Cancelled = 6
}
