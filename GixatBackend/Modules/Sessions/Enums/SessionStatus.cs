using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Sessions.Enums;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public enum SessionStatus
{
    CustomerRequest = 0,
    Inspection = 1,
    TestDrive = 2,
    ReportGenerated = 3,
    JobCardCreated = 4,
    Completed = 5,
    Cancelled = 6
}
