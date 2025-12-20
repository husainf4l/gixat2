using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Invites.Enums;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public enum InviteStatus
{
    Pending = 0,
    Accepted = 1,
    Expired = 2,
    Canceled = 3
}
