using GixatBackend.Modules.Sessions.Enums;
using GixatBackend.Modules.Sessions.Models;

namespace GixatBackend.Modules.Sessions.Services;

internal interface ISessionService
{
    /// <summary>
    /// Validates if a new session can be created for the car
    /// </summary>
    void ValidateNoActiveSession(GarageSession? existingSession);

    /// <summary>
    /// Creates a session log entry for status transition
    /// </summary>
    SessionLog CreateStatusLog(SessionStatus fromStatus, SessionStatus toStatus, string? notes);
}
