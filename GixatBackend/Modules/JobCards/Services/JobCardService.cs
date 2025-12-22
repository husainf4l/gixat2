using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Modules.Sessions.Enums;
using GixatBackend.Modules.Sessions.Models;
using GixatBackend.Modules.Common.Constants;
using GixatBackend.Modules.Common.Exceptions;
using System.Text;

namespace GixatBackend.Modules.JobCards.Services;

internal sealed class JobCardService : IJobCardService
{
    private static readonly char[] RequestPrefixes = ['-', '*', '•', '·'];

    public void ValidateSessionForJobCard(GarageSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.Status != SessionStatus.ReportGenerated)
        {
            throw new BusinessRuleViolationException(
                "InvalidSessionStatus", 
                ErrorMessages.JobCardFromReportGeneratedOnly);
        }
    }

    public string BuildInternalNotesFromSession(GarageSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var notes = new StringBuilder();
        notes.AppendLine($"Created from Session {session.Id}");

        if (session.Mileage.HasValue)
        {
            notes.AppendLine($"Mileage: {session.Mileage.Value:N0} km");
        }

        AppendSectionIfNotEmpty(notes, "Customer Requests", session.CustomerRequests);
        AppendSectionIfNotEmpty(notes, "Inspection Notes", session.InspectionNotes);
        AppendSectionIfNotEmpty(notes, "Inspection Requests", session.InspectionRequests);
        AppendSectionIfNotEmpty(notes, "Test Drive Notes", session.TestDriveNotes);
        AppendSectionIfNotEmpty(notes, "Test Drive Requests", session.TestDriveRequests);
        AppendSectionIfNotEmpty(notes, "Initial Report", session.InitialReport);

        return notes.ToString();
    }

    public List<JobItem> ExtractJobItemsFromSession(GarageSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var allRequests = new List<string>();

        AddRequestsIfNotEmpty(allRequests, session.CustomerRequests);
        AddRequestsIfNotEmpty(allRequests, session.InspectionRequests);
        AddRequestsIfNotEmpty(allRequests, session.TestDriveRequests);

        var jobItems = new List<JobItem>();
        foreach (var request in allRequests)
        {
            var trimmedRequest = request.Trim().TrimStart(RequestPrefixes).Trim();
            if (!string.IsNullOrWhiteSpace(trimmedRequest))
            {
                jobItems.Add(new JobItem
                {
                    Description = trimmedRequest,
                    Status = JobItemStatus.Pending,
                    EstimatedLaborCost = 0,
                    EstimatedPartsCost = 0
                });
            }
        }

        return jobItems;
    }

    private static void AppendSectionIfNotEmpty(StringBuilder builder, string title, string? content)
    {
        if (!string.IsNullOrEmpty(content))
        {
            builder.AppendLine($"\n{title}:\n{content}");
        }
    }

    private static void AddRequestsIfNotEmpty(List<string> target, string? source)
    {
        if (!string.IsNullOrEmpty(source))
        {
            target.AddRange(source.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
