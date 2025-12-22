using GixatBackend.Data;
using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.Customers.Services;
using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Modules.Sessions.Models;
using GixatBackend.Modules.Sessions.Services;
using GixatBackend.Modules.JobCards.Services;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Customers.GraphQL;

[ExtendObjectType<Customer>]
[Authorize]
internal static class CustomerExtensions
{
    // Load cars using DataLoader - prevents N+1 queries
    [GraphQLName("cars")]
    public static async Task<IEnumerable<Car>> GetCarsAsync(
        [Parent] Customer customer,
        CarsByCustomerDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(dataLoader);

        return await dataLoader.LoadAsync(customer.Id, cancellationToken).ConfigureAwait(false);
    }

    // Load sessions using DataLoader
    [GraphQLName("sessions")]
    public static async Task<IEnumerable<GarageSession>> GetSessionsAsync(
        [Parent] Customer customer,
        SessionsByCustomerDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(dataLoader);

        return await dataLoader.LoadAsync(customer.Id, cancellationToken).ConfigureAwait(false);
    }

    // Load job cards using DataLoader
    [GraphQLName("jobCards")]
    public static async Task<IEnumerable<JobCard>> GetJobCardsAsync(
        [Parent] Customer customer,
        JobCardsByCustomerDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(dataLoader);

        return await dataLoader.LoadAsync(customer.Id, cancellationToken).ConfigureAwait(false);
    }
}
