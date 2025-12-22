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
    // Last session date - batched
    [GraphQLName("lastSessionDate")]
    public static async Task<DateTime?> GetLastSessionDateAsync(
        [Parent] Customer customer,
        [Service] CustomerActivityDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(dataLoader);

        var results = await dataLoader.GetLastSessionDatesAsync([customer.Id], cancellationToken)
            .ConfigureAwait(false);
        
        return results.TryGetValue(customer.Id, out var date) ? date : null;
    }

    // Total number of visits/sessions - batched
    [GraphQLName("totalVisits")]
    public static async Task<int> GetTotalVisitsAsync(
        [Parent] Customer customer,
        [Service] CustomerActivityDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(dataLoader);

        var results = await dataLoader.GetVisitCountsAsync([customer.Id], cancellationToken)
            .ConfigureAwait(false);
        
        return results.TryGetValue(customer.Id, out var count) ? count : 0;
    }

    // Total spent from completed job cards - batched
    [GraphQLName("totalSpent")]
    public static async Task<decimal> GetTotalSpentAsync(
        [Parent] Customer customer,
        [Service] CustomerActivityDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(dataLoader);

        var results = await dataLoader.GetTotalSpentAsync([customer.Id], cancellationToken)
            .ConfigureAwait(false);
        
        return results.TryGetValue(customer.Id, out var total) ? total : 0;
    }

    // Number of active job cards (pending or in progress) - batched
    [GraphQLName("activeJobCards")]
    public static async Task<int> GetActiveJobCardsAsync(
        [Parent] Customer customer,
        [Service] CustomerActivityDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(dataLoader);

        var results = await dataLoader.GetActiveJobCountsAsync([customer.Id], cancellationToken)
            .ConfigureAwait(false);
        
        return results.TryGetValue(customer.Id, out var count) ? count : 0;
    }

    // Total number of cars - batched
    [GraphQLName("totalCars")]
    public static async Task<int> GetTotalCarsAsync(
        [Parent] Customer customer,
        [Service] CustomerActivityDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(dataLoader);

        var results = await dataLoader.GetCarCountsAsync([customer.Id], cancellationToken)
            .ConfigureAwait(false);
        
        return results.TryGetValue(customer.Id, out var count) ? count : 0;
    }
}
