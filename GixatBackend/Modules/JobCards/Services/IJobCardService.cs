using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Modules.Sessions.Models;

namespace GixatBackend.Modules.JobCards.Services;

internal interface IJobCardService
{
    /// <summary>
    /// Builds internal notes from session data
    /// </summary>
    string BuildInternalNotesFromSession(GarageSession session);

    /// <summary>
    /// Extracts job items from session request fields
    /// </summary>
    List<JobItem> ExtractJobItemsFromSession(GarageSession session);

    /// <summary>
    /// Validates if job card can be created from session
    /// </summary>
    void ValidateSessionForJobCard(GarageSession session);
}
