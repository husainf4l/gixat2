using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Modules.JobCards.Services;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Services;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.JobCards.GraphQL;

[ExtendObjectType<JobItem>]
[Authorize]
internal static class JobItemExtensions
{
    // Load assigned technician using DataLoader
    [GraphQLName("assignedTechnician")]
    public static async Task<ApplicationUser?> GetAssignedTechnicianAsync(
        [Parent] JobItem jobItem,
        UserByIdDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(jobItem);
        ArgumentNullException.ThrowIfNull(dataLoader);

        if (string.IsNullOrEmpty(jobItem.AssignedTechnicianId))
        {
            return null;
        }

        return await dataLoader.LoadAsync(jobItem.AssignedTechnicianId, cancellationToken).ConfigureAwait(false);
    }

    // Load parts used in this job item using DataLoader
    [GraphQLName("parts")]
    public static async Task<IEnumerable<JobItemPart>> GetPartsAsync(
        [Parent] JobItem jobItem,
        JobItemPartsDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(jobItem);
        ArgumentNullException.ThrowIfNull(dataLoader);

        return await dataLoader.LoadAsync(jobItem.Id, cancellationToken).ConfigureAwait(false);
    }

    // Load labor entries for this job item using DataLoader
    [GraphQLName("laborEntries")]
    public static async Task<IEnumerable<LaborEntry>> GetLaborEntriesAsync(
        [Parent] JobItem jobItem,
        JobItemLaborEntriesDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(jobItem);
        ArgumentNullException.ThrowIfNull(dataLoader);

        return await dataLoader.LoadAsync(jobItem.Id, cancellationToken).ConfigureAwait(false);
    }
}
