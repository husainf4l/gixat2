using GixatBackend.Modules.Sessions.Enums;
using GixatBackend.Modules.Sessions.Models;
using GixatBackend.Modules.Common.Constants;
using GixatBackend.Modules.Common.Exceptions;

namespace GixatBackend.Modules.Sessions.Services;

internal sealed class SessionService : ISessionService
{
    public void ValidateNoActiveSession(GarageSession? existingSession)
    {
        if (existingSession != null)
        {
            throw new BusinessRuleViolationException(
                "ActiveSessionExists",
                string.Format(System.Globalization.CultureInfo.InvariantCulture, 
                    ErrorMessages.ActiveSessionExists, existingSession.Id, existingSession.Status));
        }
    }

    public SessionLog CreateStatusLog(SessionStatus fromStatus, SessionStatus toStatus, string? notes)
    {
        return new SessionLog
        {
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Notes = notes
        };
    }
}
