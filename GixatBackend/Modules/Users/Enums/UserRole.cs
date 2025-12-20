namespace GixatBackend.Modules.Users.Enums;

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
