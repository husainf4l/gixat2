using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Modules.JobCards.Services;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Services;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.JobCards.GraphQL;

[ExtendObjectType<JobCard>]
[Authorize]
internal sealed class JobCardExtensions
{
    // Load job items using DataLoader - prevents N+1 queries
    [GraphQLName("items")]
    public static async Task<IEnumerable<JobItem>> GetItemsAsync(
        [Parent] JobCard jobCard,
        JobItemsByJobCardDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(jobCard);
        ArgumentNullException.ThrowIfNull(dataLoader);

        return await dataLoader.LoadAsync(jobCard.Id, cancellationToken).ConfigureAwait(false) ?? Enumerable.Empty<JobItem>();
    }

    // Load assigned technician using DataLoader
    [GraphQLName("assignedTechnician")]
    public static async Task<ApplicationUser?> GetAssignedTechnicianAsync(
        [Parent] JobCard jobCard,
        UserByIdDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(jobCard);
        ArgumentNullException.ThrowIfNull(dataLoader);

        if (string.IsNullOrEmpty(jobCard.AssignedTechnicianId))
        {
            return null;
        }

        return await dataLoader.LoadAsync(jobCard.AssignedTechnicianId, cancellationToken).ConfigureAwait(false);
    }
}
