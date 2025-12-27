using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.Sessions.Models;
using GixatBackend.Modules.Sessions.Services;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Customers.GraphQL;

[ExtendObjectType<Car>]
[Authorize]
internal sealed class CarExtensions
{
    /// <summary>
    /// Get all sessions for this car using DataLoader to prevent N+1 queries
    /// </summary>
    public static async Task<IEnumerable<GarageSession>> GetSessionsAsync(
        [Parent] Car car,
        SessionsByCarDataLoader sessionLoader)
    {
        ArgumentNullException.ThrowIfNull(car);
        ArgumentNullException.ThrowIfNull(sessionLoader);
        
        return await sessionLoader.LoadAsync(car.Id).ConfigureAwait(false) ?? Enumerable.Empty<GarageSession>();
    }
}
