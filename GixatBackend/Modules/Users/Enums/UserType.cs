using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Users.Enums;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public enum UserType
{
    System = 0,
    Organizational = 1
}
