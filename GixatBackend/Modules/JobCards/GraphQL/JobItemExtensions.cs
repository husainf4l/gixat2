using GixatBackend.Modules.JobCards.Models;
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
}
