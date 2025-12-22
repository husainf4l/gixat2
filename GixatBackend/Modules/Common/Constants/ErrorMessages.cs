namespace GixatBackend.Modules.Common.Constants;

/// <summary>
/// Common error messages used throughout the application
/// </summary>
internal static class ErrorMessages
{
    public const string EntityNotFound = "{0} not found";
    public const string EntityNotFoundInOrganization = "{0} not found in your organization";
    public const string UnauthorizedAccess = "Not authorized";
    public const string InvalidOperation = "Invalid operation: {0}";
    
    // Session errors
    public const string ActiveSessionExists = "Cannot create a new session. There is already an active session (ID: {0}) for this car with status: {1}";
    public const string SessionNotFound = "Session not found";
    public const string InvalidSessionStatus = "Can only {0} for sessions in {1} status";
    
    // Job Card errors
    public const string JobCardNotFound = "Job Card not found";
    public const string JobCardFromReportGeneratedOnly = "Can only create job card from sessions with ReportGenerated status";
    public const string JobItemNotFound = "Job Item not found";
    
    // Customer errors
    public const string CustomerNotFound = "Customer not found in your organization";
    public const string CarNotFound = "Car not found in your organization";
    
    // Organization errors
    public const string UserAlreadyInOrganization = "User already belongs to an organization";
    public const string UserNotFound = "User not found";
    public const string OrganizationNotFound = "Organization not found";
}
