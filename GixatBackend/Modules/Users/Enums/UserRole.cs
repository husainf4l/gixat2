using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Users.Enums;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public enum UserRole
{
    // System Roles
    SuperAdmin = 0,
    SystemAdmin = 1,
    
    // Organizational Roles
    OrgAdmin = 10,
    OrgManager = 11,
    OrgUser = 12,
    
    // Customer Roles (usually just 'Customer')
    Customer = 20
}
