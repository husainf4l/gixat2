using GixatBackend.Data;
using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.Customers.Services;
using GixatBackend.Modules.JobCards.Models;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Customers.GraphQL;

[ExtendObjectType<Customer>]
[Authorize]
internal static class CustomerExtensions
{
    // Load cars only when explicitly requested in GraphQL query - batched
    [GraphQLName("cars")]
    public static async Task<ICollection<Car>> GetCarsAsync(
        [Parent] Customer customer,
        [Service] CustomerActivityDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(dataLoader);

        var results = await dataLoader.GetCarsAsync([customer.Id], cancellationToken)
            .ConfigureAwait(false);
        
        return results.TryGetValue(customer.Id, out var cars) ? cars : [];
    }
}
